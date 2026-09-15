using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

namespace REPOJP.StageRoles;

internal sealed class TricksterRoleRuntime
{
    private const float StateRefreshSeconds = 0.5f;
    private readonly MonoBehaviour _coroutineOwner;
    private readonly StageRolesConfig _config;
    private readonly VanillaRolePrefabResolver _resolver;
    private readonly Dictionary<string, ActiveDecoy> _active = new(StringComparer.Ordinal);
    private readonly HashSet<string> _pendingPlayers = new(StringComparer.Ordinal);
    private readonly Dictionary<string, float> _nextPlacementAt = new(StringComparer.Ordinal);
    private readonly List<GameObject> _spawnedObjects = new();
    private int _generation;

    internal AbilityValue Status(string steamId) => _active.TryGetValue(steamId, out ActiveDecoy decoy)
        ? new AbilityValue(AbilityMetric.DecoyActive, Mathf.CeilToInt(Mathf.Max(0f, decoy.ExpiresAt - Time.time)), 0)
        : new AbilityValue(AbilityMetric.DecoyCooldown, _nextPlacementAt.TryGetValue(steamId, out float at)
            ? Mathf.CeilToInt(Mathf.Max(0f, at - Time.time)) : 0, 0);

    internal TricksterRoleRuntime(
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
        _resolver.TryGetScreamDoll(out _);
    }

    internal bool TryPlace(RoleAssignment assignment)
    {
        if (!RoleCatalog.HasCapability(
                assignment.Role,
                StageRole.Trickster) ||
            !PlayerState.IsLiving(assignment.Player) ||
            PlayerState.IsInTruck(assignment.Player) ||
            _active.ContainsKey(assignment.SteamId) ||
            _pendingPlayers.Contains(assignment.SteamId) ||
            (_nextPlacementAt.TryGetValue(assignment.SteamId, out float readyAt) &&
             Time.time < readyAt) ||
            !_resolver.TryGetScreamDoll(out ResolvedRolePrefab resolved))
        {
            return false;
        }

        Vector3 position = PlacementPosition(assignment.Player);
        try
        {
            GameObject instance = SemiFunc.IsMultiplayer()
                ? PhotonNetwork.Instantiate(
                    resolved.ResourcePath,
                    position,
                    Quaternion.identity,
                    0)
                : UnityEngine.Object.Instantiate(
                    resolved.Prefab,
                    position,
                    Quaternion.identity);
            instance.name = "RoleShuffle_TricksterDecoy";
            StageFluxCompatibility.MarkInternalCarrier(instance);
            _spawnedObjects.Add(instance);
            float expiresAt = Time.time + Mathf.Clamp(
                _config.TricksterActiveSeconds.Value,
                1f,
                120f);
            _nextPlacementAt.Remove(assignment.SteamId);
            int generation = _generation;
            _pendingPlayers.Add(assignment.SteamId);
            _coroutineOwner.StartCoroutine(
                InitializeAfterSpawn(
                    instance,
                    assignment.SteamId,
                    expiresAt,
                    false,
                    generation));
            return true;
        }
        catch (Exception exception)
        {
            _pendingPlayers.Remove(assignment.SteamId);
            StageRolesPlugin.ModLogger.LogWarning(
                $"Trickster decoy placement failed: {exception.Message}");
            return false;
        }
    }

    internal void Tick(
        IReadOnlyList<RoleAssignment> assignments,
        RoleNotifier notifier)
    {
        NotifyReady(assignments, notifier);
        if (_active.Count == 0)
        {
            return;
        }

        List<string>? expired = null;
        foreach (KeyValuePair<string, ActiveDecoy> pair in _active)
        {
            ActiveDecoy decoy = pair.Value;
            if (decoy.Instance == null || Time.time >= decoy.ExpiresAt)
            {
                (expired ??= new List<string>()).Add(pair.Key);
                continue;
            }
            if (Time.time >= decoy.NextRefreshAt)
            {
                Lock(decoy.PhysObject);
                SetScreamState(decoy.ScreamDoll, ScreamDollValuable.States.Active);
                decoy.NextRefreshAt = Time.time + StateRefreshSeconds;
            }
            if (Time.time >= decoy.NextPulseAt)
            {
                decoy.AffectedEnemy |= Pulse(decoy.Instance.transform.position) > 0;
                decoy.NextPulseAt = Time.time +
                    Mathf.Clamp(_config.TricksterPulseIntervalSeconds.Value, 0.25f, 10f);
            }
        }
        if (expired == null)
        {
            return;
        }
        foreach (string steamId in expired)
        {
            DestroyForPlayer(steamId, startCooldown: true);
        }
    }

    private void NotifyReady(
        IReadOnlyList<RoleAssignment> assignments,
        RoleNotifier notifier)
    {
        foreach (RoleAssignment assignment in assignments)
        {
            if (!RoleCatalog.HasCapability(
                    assignment.Role,
                    StageRole.Trickster) ||
                !_nextPlacementAt.TryGetValue(
                    assignment.SteamId,
                    out float readyAt) ||
                Time.time < readyAt)
            {
                continue;
            }

            _nextPlacementAt.Remove(assignment.SteamId);
            notifier.Notify(assignment.Player, "TricksterReady");
        }
    }

    internal void RemovePlayer(string steamId)
    {
        DestroyForPlayer(steamId, startCooldown: false);
        _nextPlacementAt.Remove(steamId);
    }

    internal bool IsDecoy(PhysGrabObject physObject)
    {
        if (physObject == null)
        {
            return false;
        }
        Transform target = physObject.transform;
        foreach (GameObject instance in _spawnedObjects)
        {
            if (instance != null &&
                (ReferenceEquals(instance.transform, target) ||
                 target.IsChildOf(instance.transform)))
            {
                return true;
            }
        }
        return false;
    }

    internal bool MaintainActiveState(ScreamDollValuable screamDoll)
    {
        if (screamDoll == null)
        {
            return false;
        }

        PhysGrabObject? physObject =
            screamDoll.GetComponent<PhysGrabObject>() ??
            screamDoll.GetComponentInParent<PhysGrabObject>();
        if (physObject == null || !IsDecoy(physObject))
        {
            return false;
        }

        // Vanilla returns an active Scream Doll to Idle whenever no grabber is
        // registered. Trickster decoys are intentionally fixed without a real
        // grabber, so restore the synthetic grabbed state immediately before
        // the vanilla Active-state check instead of repeatedly restarting it.
        Lock(physObject);
        return true;
    }

    internal void Stop()
    {
        _generation++;
        foreach (GameObject instance in _spawnedObjects)
        {
            DestroyNetworkObject(instance);
        }
        _active.Clear();
        _pendingPlayers.Clear();
        _spawnedObjects.Clear();
        _nextPlacementAt.Clear();
    }

    private IEnumerator InitializeAfterSpawn(
        GameObject instance,
        string steamId,
        float expiresAt,
        bool alreadyAffectedEnemy,
        int generation)
    {
        yield return null;
        if (instance == null || generation != _generation)
        {
            _pendingPlayers.Remove(steamId);
            DestroyNetworkObject(instance);
            _spawnedObjects.RemoveAll(
                candidate => candidate == null || ReferenceEquals(candidate, instance));
            yield break;
        }

        ScreamDollValuable? screamDoll =
            instance.GetComponentInChildren<ScreamDollValuable>(true);
        PhysGrabObject? physObject =
            instance.GetComponentInChildren<PhysGrabObject>(true);
        ValuableObject? valuable =
            instance.GetComponentInChildren<ValuableObject>(true);
        if (screamDoll == null || physObject == null)
        {
            _pendingPlayers.Remove(steamId);
            StageRolesPlugin.ModLogger.LogWarning(
                "Trickster decoy was removed because its vanilla components did not initialize.");
            DestroyNetworkObject(instance);
            _spawnedObjects.Remove(instance);
            yield break;
        }

        Lock(physObject);
        SetValueZero(valuable);
        SetScreamState(screamDoll, ScreamDollValuable.States.Active);
        ActiveDecoy decoy = new(
            instance,
            screamDoll,
            physObject,
            expiresAt,
            Time.time + Mathf.Clamp(
                _config.TricksterPulseIntervalSeconds.Value,
                0.25f,
                10f),
            Time.time + StateRefreshSeconds);
        decoy.AffectedEnemy = alreadyAffectedEnemy ||
                              Pulse(instance.transform.position) > 0;
        _active[steamId] = decoy;
        _pendingPlayers.Remove(steamId);
        StageRolesPlugin.ModLogger.LogInfo(
            $"Trickster placed a decoy for player {steamId}.");
    }

    private void DestroyForPlayer(string steamId, bool startCooldown)
    {
        if (!_active.Remove(steamId, out ActiveDecoy decoy))
        {
            return;
        }
        if (startCooldown)
        {
            float cooldown = decoy.AffectedEnemy
                ? Mathf.Clamp(_config.TricksterCooldownSeconds.Value, 1f, 300f)
                : Mathf.Clamp(
                    _config.TricksterNoTargetCooldownSeconds.Value,
                    0f,
                    300f);
            _nextPlacementAt[steamId] = Time.time + cooldown;
        }
        try
        {
            SetScreamState(decoy.ScreamDoll, ScreamDollValuable.States.Idle);
        }
        catch
        {
            // Authoritative destruction below is sufficient.
        }
        DestroyNetworkObject(decoy.Instance);
        _spawnedObjects.Remove(decoy.Instance);
    }

    private int Pulse(Vector3 position)
    {
        EnemyDirector? director = EnemyDirector.instance;
        if (director == null)
        {
            return 0;
        }

        float radius = Mathf.Clamp(
            _config.TricksterInvestigateRadius.Value,
            1f,
            100f);
        int eligibleEnemies = CountEligibleEnemies(
            director.enemiesSpawned,
            position,
            radius);
        director.SetInvestigate(position, radius, pathfindOnly: false);
        return eligibleEnemies;
    }

    private static int CountEligibleEnemies(
        IReadOnlyList<EnemyParent> enemies,
        Vector3 position,
        float radius)
    {
        int count = 0;
        foreach (EnemyParent enemyParent in enemies)
        {
            if (enemyParent == null || !enemyParent.gameObject.activeInHierarchy)
            {
                continue;
            }
            Enemy? enemy = enemyParent.GetComponentInChildren<Enemy>(true);
            EnemyStateInvestigate? investigate =
                enemyParent.GetComponentInChildren<EnemyStateInvestigate>(true);
            if (enemy == null || investigate == null ||
                enemy.CurrentState is EnemyState.Spawn or
                    EnemyState.Stunned or EnemyState.LookUnder or
                    EnemyState.Despawn)
            {
                continue;
            }
            float rangeMultiplier = Mathf.Max(0.01f, investigate.rangeMultiplier);
            if (Vector3.Distance(position, enemy.transform.position) /
                rangeMultiplier >= radius)
            {
                continue;
            }
            if (investigate.OnlyEvent ||
                enemy.CurrentState is EnemyState.Roaming or
                    EnemyState.Investigate or EnemyState.ChaseSlow or
                    EnemyState.ChaseEnd)
            {
                count++;
            }
        }
        return count;
    }

    private Vector3 PlacementPosition(PlayerAvatar player)
    {
        Transform body = player.playerTransform != null
            ? player.playerTransform
            : player.transform;
        Vector3 forward = body.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f)
        {
            forward = Vector3.forward;
        }
        forward.Normalize();
        Vector3 requested = player.transform.position + forward *
            Mathf.Clamp(_config.TricksterPlacementDistance.Value, 0.5f, 10f);
        Vector3 rayOrigin = requested + Vector3.up * 2f;
        return Physics.Raycast(
                rayOrigin,
                Vector3.down,
                out RaycastHit hit,
                5f,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore)
            ? hit.point + Vector3.up * 0.05f
            : requested;
    }

    private static void Lock(PhysGrabObject physObject)
    {
        physObject.grabbed = true;
        physObject.OverrideKinematic(1f);
        physObject.OverrideGrabDisable(1f);
        physObject.OverrideIndestructible(1f);
    }

    private static void SetValueZero(ValuableObject? valuable)
    {
        if (valuable == null)
        {
            return;
        }
        PhotonView? view = valuable.GetComponent<PhotonView>() ??
                           valuable.GetComponentInParent<PhotonView>();
        if (SemiFunc.IsMultiplayer() && view != null && view.ViewID != 0)
        {
            view.RPC(nameof(ValuableObject.DollarValueSetRPC), RpcTarget.All, 0f);
        }
        else
        {
            valuable.DollarValueSetRPC(0f);
        }
    }

    private static void SetScreamState(
        ScreamDollValuable screamDoll,
        ScreamDollValuable.States state)
    {
        PhotonView? view = screamDoll.GetComponent<PhotonView>() ??
                           screamDoll.GetComponentInParent<PhotonView>();
        if (SemiFunc.IsMultiplayer() && view != null && view.ViewID != 0)
        {
            view.RPC(nameof(ScreamDollValuable.SetStateRPC), RpcTarget.All, state);
        }
        else
        {
            screamDoll.SetStateRPC(state);
        }
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
                $"Trickster decoy cleanup was deferred: {exception.Message}");
        }
    }

    private sealed class ActiveDecoy
    {
        internal ActiveDecoy(
            GameObject instance,
            ScreamDollValuable screamDoll,
            PhysGrabObject physObject,
            float expiresAt,
            float nextPulseAt,
            float nextRefreshAt)
        {
            Instance = instance;
            ScreamDoll = screamDoll;
            PhysObject = physObject;
            ExpiresAt = expiresAt;
            NextPulseAt = nextPulseAt;
            NextRefreshAt = nextRefreshAt;
        }

        internal GameObject Instance { get; }
        internal ScreamDollValuable ScreamDoll { get; }
        internal PhysGrabObject PhysObject { get; }
        internal float ExpiresAt { get; }
        internal float NextPulseAt { get; set; }
        internal float NextRefreshAt { get; set; }
        internal bool AffectedEnemy { get; set; }
    }
}
