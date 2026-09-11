using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

namespace REPOJP.StageRoles;

internal sealed class BomberRoleRuntime
{
    private const float DuctTapedSpawnBehindDistance = 2f;

    private readonly struct GrenadeOwner
    {
        internal GrenadeOwner(
            PhysGrabObject grenade,
            string steamId,
            int playerViewId)
        {
            Grenade = grenade;
            SteamId = steamId;
            PlayerViewId = playerViewId;
        }

        internal PhysGrabObject Grenade { get; }
        internal string SteamId { get; }
        internal int PlayerViewId { get; }
    }

    private readonly MonoBehaviour _coroutineOwner;
    private readonly StageRolesConfig _config;
    private readonly VanillaRolePrefabResolver _resolver;
    private readonly Dictionary<string, List<GameObject>> _instances = new(StringComparer.Ordinal);
    private readonly Dictionary<int, GrenadeOwner> _grenadeOwners = new();
    private readonly Dictionary<(int GrenadeId, int PlayerViewId), float>
        _deniedReleaseUntil = new();
    private int _generation;
    private bool _active;

    internal BomberRoleRuntime(
        MonoBehaviour coroutineOwner,
        StageRolesConfig config,
        VanillaRolePrefabResolver resolver)
    {
        _coroutineOwner = coroutineOwner;
        _config = config;
        _resolver = resolver;
    }

    internal void Begin()
    {
        Stop();
        _active = true;
        _generation++;
    }

    internal void Tick(RoleAssignment assignment)
    {
        PlayerAvatar player = assignment.Player;
        if (!_active || player == null || !PlayerState.IsLiving(player))
        {
            return;
        }

        Vector3 current = player.transform.position;
        Vector3 delta = current - assignment.PreviousPosition;
        assignment.PreviousPosition = current;
        delta.y = 0f;
        float distance = delta.magnitude;
        if (distance > 4f)
        {
            return;
        }

        if (!_config.BomberAllowTruckSpawns.Value &&
            PlayerState.IsInTruck(player))
        {
            // Movement inside the truck must never be carried into the stage.
            // Keep the latest position as the outside-distance origin and clear
            // any partial distance accumulated before re-entering the truck.
            assignment.TravelDistance = 0f;
            return;
        }

        assignment.TravelDistance += distance;
        if (assignment.TravelDistance < _config.ClampedBomberDistance)
        {
            return;
        }

        assignment.TravelDistance -= _config.ClampedBomberDistance;
        if (!_resolver.TryGetRandomGrenade(_config, out ResolvedRolePrefab resolved))
        {
            return;
        }

        MakeRoomForSpawn(assignment.SteamId);
        Vector3 spawnPoint = current;
        if (resolved.GrenadeKind == VanillaGrenadeKind.DuctTaped)
        {
            Vector3 travelDirection = delta.sqrMagnitude > 0.0001f
                ? delta.normalized
                : player.transform.forward;
            travelDirection.y = 0f;
            if (travelDirection.sqrMagnitude > 0.0001f)
            {
                spawnPoint -= travelDirection.normalized *
                    DuctTapedSpawnBehindDistance;
            }
        }

        Vector3 position = FindFloorPosition(spawnPoint);
        _coroutineOwner.StartCoroutine(
            SpawnAndArm(assignment.SteamId, player, position, resolved, _generation));
    }

    internal void Stop()
    {
        _active = false;
        _generation++;
        foreach (List<GameObject> list in _instances.Values)
        {
            foreach (GameObject instance in list)
            {
                DestroyNetworkObject(instance);
            }
        }
        _instances.Clear();
        _grenadeOwners.Clear();
        _deniedReleaseUntil.Clear();
    }

    internal void RemovePlayer(string steamId)
    {
        if (!_instances.Remove(steamId, out List<GameObject> instances))
        {
            return;
        }

        HashSet<int> removedGrenadeIds = new();
        foreach (GameObject instance in instances)
        {
            if (instance != null)
            {
                PhysGrabObject? grenade =
                    instance.GetComponent<PhysGrabObject>() ??
                    instance.GetComponentInChildren<PhysGrabObject>(true);
                if (grenade != null)
                {
                    removedGrenadeIds.Add(grenade.GetInstanceID());
                    _grenadeOwners.Remove(grenade.GetInstanceID());
                }
            }
            DestroyNetworkObject(instance);
        }

        if (removedGrenadeIds.Count > 0)
        {
            List<(int GrenadeId, int PlayerViewId)> deniedToRemove = new();
            foreach ((int GrenadeId, int PlayerViewId) key in
                     _deniedReleaseUntil.Keys)
            {
                if (removedGrenadeIds.Contains(key.GrenadeId))
                {
                    deniedToRemove.Add(key);
                }
            }
            foreach ((int GrenadeId, int PlayerViewId) key in deniedToRemove)
            {
                _deniedReleaseUntil.Remove(key);
            }
        }
    }

    internal bool AllowGrabStart(
        PhysGrabObject grenade,
        int playerViewId,
        PhotonMessageInfo messageInfo,
        bool requesterIsBomber)
    {
        if (!_active || grenade == null ||
            !_grenadeOwners.TryGetValue(
                grenade.GetInstanceID(),
                out GrenadeOwner owner) ||
            !ReferenceEquals(owner.Grenade, grenade))
        {
            return true;
        }

        PhysGrabber? grabber = ResolveGrabber(playerViewId);
        if (grabber?.photonView == null ||
            !SemiFunc.OwnerOnlyRPC(messageInfo, grabber.photonView))
        {
            return true;
        }

        string grabberSteamId = PlayerIdentity.SteamId(grabber.playerAvatar);
        bool isOwner = playerViewId == owner.PlayerViewId ||
                       (!string.IsNullOrEmpty(owner.SteamId) &&
                        string.Equals(
                            grabberSteamId,
                            owner.SteamId,
                            StringComparison.Ordinal));
        if (isOwner || requesterIsBomber)
        {
            return true;
        }

        PruneDeniedReleases();
        _deniedReleaseUntil[(grenade.GetInstanceID(), playerViewId)] =
            Time.time + 2f;
        try
        {
            PhotonView? grenadeView =
                grenade.GetComponent<PhotonView>() ??
                grenade.GetComponentInParent<PhotonView>() ??
                grenade.GetComponentInChildren<PhotonView>(true);
            int grenadeViewId = grenadeView != null
                ? grenadeView.ViewID
                : -1;
            grabber.OverrideGrabRelease(grenadeViewId, 0.5f);
            StageRolesPlugin.ModLogger.LogDebug(
                $"Rejected a non-owner grab of Bomber grenade by {grabberSteamId}.");
        }
        catch (Exception exception)
        {
            StageRolesPlugin.ModLogger.LogWarning(
                $"Could not release a non-owner Bomber grenade grab: {exception.Message}");
        }
        return false;
    }

    internal bool AllowGrabEnd(
        PhysGrabObject grenade,
        int playerViewId,
        PhotonMessageInfo messageInfo)
    {
        if (grenade == null)
        {
            return true;
        }
        (int GrenadeId, int PlayerViewId) key =
            (grenade.GetInstanceID(), playerViewId);
        if (!_deniedReleaseUntil.ContainsKey(key))
        {
            return true;
        }

        PhysGrabber? grabber = ResolveGrabber(playerViewId);
        if (grabber?.photonView == null ||
            !SemiFunc.OwnerOnlyRPC(messageInfo, grabber.photonView))
        {
            return true;
        }
        _deniedReleaseUntil.Remove(key);
        return false;
    }

    private void MakeRoomForSpawn(string steamId)
    {
        if (!_instances.TryGetValue(steamId, out List<GameObject> list))
        {
            return;
        }

        list.RemoveAll(instance => instance == null);
        while (list.Count >= _config.ClampedBomberMaximum)
        {
            GameObject oldest = list[0];
            list.RemoveAt(0);
            ForgetGrenadeTracking(oldest);
            DestroyNetworkObject(oldest);
            StageRolesPlugin.ModLogger.LogDebug(
                $"Removed the oldest Bomber grenade for player {steamId} " +
                "before placing a replacement.");
        }
    }

    private void ForgetGrenadeTracking(GameObject? instance)
    {
        if (instance == null)
        {
            return;
        }

        PhysGrabObject? grenade =
            instance.GetComponent<PhysGrabObject>() ??
            instance.GetComponentInChildren<PhysGrabObject>(true);
        if (grenade == null)
        {
            return;
        }

        int grenadeId = grenade.GetInstanceID();
        _grenadeOwners.Remove(grenadeId);
        List<(int GrenadeId, int PlayerViewId)> deniedToRemove = new();
        foreach ((int GrenadeId, int PlayerViewId) key in _deniedReleaseUntil.Keys)
        {
            if (key.GrenadeId == grenadeId)
            {
                deniedToRemove.Add(key);
            }
        }
        foreach ((int GrenadeId, int PlayerViewId) key in deniedToRemove)
        {
            _deniedReleaseUntil.Remove(key);
        }
    }

    private IEnumerator SpawnAndArm(
        string steamId,
        PlayerAvatar player,
        Vector3 position,
        ResolvedRolePrefab resolved,
        int generation)
    {
        GameObject? instance = null;
        try
        {
            instance = SemiFunc.IsMultiplayer()
                ? PhotonNetwork.Instantiate(resolved.ResourcePath, position, Quaternion.identity, 0)
                : UnityEngine.Object.Instantiate(resolved.Prefab, position, Quaternion.identity);
            StageFluxCompatibility.MarkInternalCarrier(instance);
            if (!_instances.TryGetValue(steamId, out List<GameObject> list))
            {
                list = new List<GameObject>();
                _instances[steamId] = list;
            }
            list.Add(instance);
            PhysGrabObject? physGrabObject =
                instance.GetComponent<PhysGrabObject>() ??
                instance.GetComponentInChildren<PhysGrabObject>(true);
            if (physGrabObject != null)
            {
                _grenadeOwners[physGrabObject.GetInstanceID()] = new GrenadeOwner(
                    physGrabObject,
                    steamId,
                    player.photonView?.ViewID ?? 0);
            }
        }
        catch (Exception exception)
        {
            StageRolesPlugin.ModLogger.LogWarning(
                $"Could not place a Bomber grenade: {exception.Message}");
            yield break;
        }

        yield return null;
        if (!_active || generation != _generation || instance == null || player == null)
        {
            DestroyNetworkObject(instance);
            yield break;
        }

        try
        {
            ItemGrenade? grenade = instance.GetComponent<ItemGrenade>() ??
                                    instance.GetComponentInChildren<ItemGrenade>(true);
            ItemToggle? toggle = instance.GetComponent<ItemToggle>() ??
                                 instance.GetComponentInChildren<ItemToggle>(true);
            if (grenade == null || toggle == null)
            {
                DestroyNetworkObject(instance);
                yield break;
            }

            grenade.isSpawnedGrenade = true;
            grenade.SetActivator(player);
            toggle.ToggleItem(true, player.photonView?.ViewID ?? 0);
            StageRolesPlugin.ModLogger.LogDebug(
                $"Bomber placed an armed grenade for player {steamId}.");
        }
        catch (Exception exception)
        {
            StageRolesPlugin.ModLogger.LogWarning(
                $"Could not arm a Bomber grenade: {exception.Message}");
            DestroyNetworkObject(instance);
        }
    }

    private static PhysGrabber? ResolveGrabber(int playerViewId)
    {
        try
        {
            PhotonView? playerView = PhotonView.Find(playerViewId);
            return playerView != null
                ? playerView.GetComponent<PhysGrabber>()
                : null;
        }
        catch
        {
            return null;
        }
    }

    private void PruneDeniedReleases()
    {
        if (_deniedReleaseUntil.Count == 0)
        {
            return;
        }
        List<(int GrenadeId, int PlayerViewId)> expired = new();
        foreach (KeyValuePair<(int GrenadeId, int PlayerViewId), float> entry in
                 _deniedReleaseUntil)
        {
            if (Time.time > entry.Value)
            {
                expired.Add(entry.Key);
            }
        }
        foreach ((int GrenadeId, int PlayerViewId) key in expired)
        {
            _deniedReleaseUntil.Remove(key);
        }
    }

    private static Vector3 FindFloorPosition(Vector3 playerPosition)
    {
        Vector3 origin = playerPosition + Vector3.up * 1.5f;
        return Physics.Raycast(
                origin,
                Vector3.down,
                out RaycastHit hit,
                5f,
                Physics.AllLayers,
                QueryTriggerInteraction.Ignore)
            ? hit.point + Vector3.up * 0.08f
            : playerPosition + Vector3.up * 0.08f;
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
                $"Bomber grenade cleanup deferred to scene unload: {exception.Message}");
        }
    }
}
