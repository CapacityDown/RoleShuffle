using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

namespace REPOJP.StageRoles;

internal sealed class StinkerRoleRuntime
{
    private const float TrailEmissionIntervalSeconds = 0.2f;
    private readonly MonoBehaviour _coroutineOwner;
    private readonly StageRolesConfig _config;
    private readonly VanillaRolePrefabResolver _resolver;
    private readonly List<GameObject> _carriers = new();
    private readonly Dictionary<string, List<Vector3>> _pendingTrailPoints =
        new(StringComparer.Ordinal);
    private readonly Dictionary<string, float> _nextTrailEmissionAt =
        new(StringComparer.Ordinal);
    private int _generation;
    private bool _active;

    internal StinkerRoleRuntime(
        MonoBehaviour coroutineOwner,
        StageRolesConfig config,
        VanillaRolePrefabResolver resolver)
    {
        _coroutineOwner = coroutineOwner;
        _config = config;
        _resolver = resolver;
    }

    internal void Begin(IReadOnlyList<RoleAssignment> assignments)
    {
        Stop();
        _active = true;
        _generation++;
        foreach (RoleAssignment assignment in assignments)
        {
            if (!RoleCatalog.HasCapability(assignment.Role, StageRole.Stinker))
            {
                continue;
            }
            Vector3 position = assignment.Player.transform.position;
            assignment.StinkerPreviousPosition = position;
            assignment.StinkerTrailAnchor = position;
            assignment.StinkerTravelDistance = 0f;
            _pendingTrailPoints[assignment.SteamId] = new List<Vector3>();
            _nextTrailEmissionAt[assignment.SteamId] = 0f;
        }
        _resolver.TryGetRandomUraniumValuable(out _);
    }

    internal void Tick(RoleAssignment assignment)
    {
        PlayerAvatar player = assignment.Player;
        if (!_active || player == null || !PlayerState.IsLiving(player))
        {
            return;
        }

        Vector3 previous = assignment.StinkerPreviousPosition;
        Vector3 current = player.transform.position;
        Vector3 delta = current - previous;
        assignment.StinkerPreviousPosition = current;
        delta.y = 0f;
        float distance = delta.magnitude;
        if (distance > 4f)
        {
            assignment.StinkerTrailAnchor = current;
            assignment.StinkerTravelDistance = 0f;
            PendingPoints(assignment.SteamId).Clear();
            _nextTrailEmissionAt[assignment.SteamId] = 0f;
            return;
        }

        if (!_config.StinkerAllowTruckSpawns.Value && PlayerState.IsInTruck(player))
        {
            assignment.StinkerTravelDistance = 0f;
            return;
        }

        assignment.StinkerTravelDistance += distance;
        float interval = _config.ClampedStinkerDistance;
        List<Vector3> pending = PendingPoints(assignment.SteamId);
        Vector3 direction = distance > 0.001f
            ? delta / distance
            : Vector3.zero;
        while (assignment.StinkerTravelDistance >= interval)
        {
            assignment.StinkerTravelDistance -= interval;
            Vector3 trailPoint = current - direction * assignment.StinkerTravelDistance;
            pending.Add(trailPoint);
            if (pending.Count > 64)
            {
                pending.RemoveAt(0);
            }
        }

        _nextTrailEmissionAt.TryGetValue(
            assignment.SteamId,
            out float nextEmissionAt);
        if (pending.Count > 0 && Time.time >= nextEmissionAt)
        {
            Vector3 candidate = pending[0];
            Vector3 separation = current - candidate;
            separation.y = 0f;
            bool isNewestPendingPoint = pending.Count == 1;
            bool newestPointIsSafe =
                separation.magnitude >= _config.ClampedStinkerSafetyDistance;
            if (!isNewestPendingPoint || newestPointIsSafe)
            {
                if (_resolver.TryGetRandomUraniumValuable(
                        out ResolvedRolePrefab resolved))
                {
                    pending.RemoveAt(0);
                    Vector3 position = FindFloorPosition(candidate);
                    _coroutineOwner.StartCoroutine(
                        SpawnAndBreak(position, resolved, _generation, assignment, pending));
                    _nextTrailEmissionAt[assignment.SteamId] =
                        Time.time + TrailEmissionIntervalSeconds;
                }
            }
        }
        assignment.StinkerTrailAnchor = current;
    }

    internal void Stop()
    {
        _active = false;
        _generation++;
        foreach (GameObject carrier in _carriers)
        {
            DestroyNetworkObject(carrier);
        }
        _carriers.Clear();
        _pendingTrailPoints.Clear();
        _nextTrailEmissionAt.Clear();
    }

    internal void RemovePlayer(string steamId)
    {
        _pendingTrailPoints.Remove(steamId);
        _nextTrailEmissionAt.Remove(steamId);
    }

    internal void AddPlayer(RoleAssignment assignment)
    {
        if (!RoleCatalog.HasCapability(assignment.Role, StageRole.Stinker) || assignment.Player == null)
        {
            return;
        }
        Vector3 position = assignment.Player.transform.position;
        assignment.StinkerPreviousPosition = position;
        assignment.StinkerTrailAnchor = position;
        assignment.StinkerTravelDistance = 0f;
        _pendingTrailPoints[assignment.SteamId] = new List<Vector3>();
        _nextTrailEmissionAt[assignment.SteamId] = 0f;
    }

    private List<Vector3> PendingPoints(string steamId)
    {
        if (!_pendingTrailPoints.TryGetValue(steamId, out List<Vector3>? points))
        {
            points = new List<Vector3>();
            _pendingTrailPoints[steamId] = points;
        }
        return points;
    }

    private IEnumerator SpawnAndBreak(
        Vector3 position,
        ResolvedRolePrefab resolved,
        int generation,
        RoleAssignment assignment,
        List<Vector3> sourcePoints)
    {
        GameObject? carrier = null;
        try
        {
            // Keep the carrier tracked throughout initialization and server delivery so
            // stage cleanup can cancel it without leaving a deferred break behind.
            try
            {
                carrier = SemiFunc.IsMultiplayer()
                    ? PhotonNetwork.InstantiateRoomObject(
                        resolved.ResourcePath,
                        position,
                        Quaternion.identity,
                        0)
                    : UnityEngine.Object.Instantiate(
                        resolved.Prefab,
                        position,
                        Quaternion.identity);
                StageFluxCompatibility.MarkInternalCarrier(carrier);
                _carriers.Add(carrier);
                SetCarrierPriceZero(carrier);
                foreach (Rigidbody body in carrier.GetComponentsInChildren<Rigidbody>(true))
                {
                    if (!body.isKinematic)
                    {
                        body.velocity = Vector3.zero;
                        body.angularVelocity = Vector3.zero;
                    }
                    body.isKinematic = true;
                    // Vanilla EnableRigidbody clears isKinematic after 0.1 seconds.
                    // Keep host-authoritative motion locked even after that reset.
                    body.constraints = RigidbodyConstraints.FreezeAll;
                }
                foreach (PhysGrabObject grabObject in carrier.GetComponentsInChildren<PhysGrabObject>(true))
                {
                    grabObject.OverrideGrabDisable(3f);
                }
            }
            catch (Exception exception)
            {
                StageRolesPlugin.ModLogger.LogWarning(
                    $"Stinker uranium carrier could not be created: {exception.Message}");
                yield break;
            }

            // A local frame does not let vanilla clients run Start(): instantiate and
            // break can otherwise arrive in the same Photon dispatch on those clients.
            yield return null;
            // Give the spawned valuable a short grace period in both solo and
            // multiplayer; also let remote clients initialize before breaking.
            yield return new WaitForSeconds(_config.StinkerBreakGraceSeconds.Value);
            if (!_active || generation != _generation || carrier == null ||
                !SemiFunc.IsMasterClientOrSingleplayer() ||
                !_pendingTrailPoints.TryGetValue(assignment.SteamId, out List<Vector3>? currentPoints) ||
                !ReferenceEquals(currentPoints, sourcePoints) ||
                !PlayerState.IsLiving(assignment.Player) ||
                (!_config.StinkerAllowTruckSpawns.Value && PlayerState.IsInTruck(assignment.Player)))
            {
                yield break;
            }
            Vector3 separation = assignment.Player.transform.position - position;
            separation.y = 0f;
            if (separation.magnitude < _config.ClampedStinkerSafetyDistance)
            {
                // Do not accumulate delayed breaks when the player doubles back.
                yield break;
            }

            try
            {
                UraniumScript? uranium =
                    carrier.GetComponent<UraniumScript>() ??
                    carrier.GetComponentInChildren<UraniumScript>(true);
                PhysGrabObjectImpactDetector? impactDetector =
                    carrier.GetComponent<PhysGrabObjectImpactDetector>() ??
                    carrier.GetComponentInChildren<PhysGrabObjectImpactDetector>(true);
                if (uranium == null || impactDetector == null)
                {
                    yield break;
                }
                if (SemiFunc.IsMultiplayer())
                {
                    PhotonView? view = impactDetector.GetComponent<PhotonView>();
                    if (view == null || view.ViewID == 0)
                    {
                        yield break;
                    }
                    // Do not destroy locally before the server has relayed the break.
                    // This is the existing vanilla RPC, not a client-mod requirement.
                    view.RPC(nameof(PhysGrabObjectImpactDetector.DestroyObjectRPC),
                        RpcTarget.AllViaServer, new object[] { true });
                }
                else
                {
                    impactDetector.DestroyObject(effects: true);
                }
                StageRolesPlugin.ModLogger.LogDebug(
                    "Stinker created a vanilla uranium break cloud.");
            }
            catch (Exception exception)
            {
                StageRolesPlugin.ModLogger.LogWarning(
                    $"Stinker uranium carrier could not be broken: {exception.Message}");
                yield break;
            }
            // Vanilla destruction normally removes the carrier itself. Keep a bounded
            // cleanup fallback for canceled delivery or an incomplete destruction.
            if (SemiFunc.IsMultiplayer())
            {
                yield return new WaitForSeconds(2f);
            }
        }
        finally
        {
            if (carrier is not null)
            {
                _carriers.Remove(carrier);
            }
            DestroyNetworkObject(carrier);
        }
    }

    private static void SetCarrierPriceZero(GameObject carrier)
    {
        foreach (ValuableObject valuable in carrier.GetComponentsInChildren<ValuableObject>(true))
        {
            // Set the initialized flag before Start's price coroutine can choose a
            // random price or add value to the level's available haul.
            valuable.DollarValueSetRPC(0f);
            if (!SemiFunc.IsMultiplayer())
            {
                continue;
            }
            PhotonView? view = valuable.GetComponent<PhotonView>() ??
                               valuable.GetComponentInParent<PhotonView>();
            if (view == null || view.ViewID == 0)
            {
                throw new InvalidOperationException("Stinker carrier has no valid price synchronization view.");
            }
            view.RPC(nameof(ValuableObject.DollarValueSetRPC), RpcTarget.Others, new object[] { 0f });
        }
    }

    private static Vector3 FindFloorPosition(Vector3 position)
    {
        Vector3 origin = position + Vector3.up * 1.5f;
        return Physics.Raycast(
                origin,
                Vector3.down,
                out RaycastHit hit,
                5f,
                Physics.AllLayers,
                QueryTriggerInteraction.Ignore)
            ? hit.point + Vector3.up * 0.08f
            : position + Vector3.up * 0.08f;
    }

    private static void DestroyNetworkObject(GameObject? instance)
    {
        if (instance == null)
        {
            return;
        }
        try
        {
            PhotonView? view = instance.GetComponent<PhotonView>() ??
                               instance.GetComponentInChildren<PhotonView>(true);
            if (SemiFunc.IsMultiplayer() && PhotonNetwork.IsMasterClient &&
                view != null && view.ViewID != 0)
            {
                PhotonNetwork.Destroy(view.gameObject);
            }
            else
            {
                UnityEngine.Object.Destroy(instance);
            }
        }
        catch (Exception exception)
        {
            StageRolesPlugin.ModLogger.LogDebug(
                $"Stinker carrier cleanup was deferred: {exception.Message}");
        }
    }
}
