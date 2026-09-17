using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace REPOJP.StageRoles;

internal sealed class EventRoleRuntime
{
    private static readonly FieldInfo? IsLocalPlayerField = AccessTools.Field(typeof(PlayerAvatar), "isLocal");
    private static readonly FieldInfo? PlayerTumbleField =
        AccessTools.Field(typeof(PlayerAvatar), "tumble");
    private static readonly FieldInfo? TumbleIsTumblingField =
        AccessTools.Field(typeof(PlayerTumble), "isTumbling");
    private static readonly FieldInfo? DetectionOnlyField =
        AccessTools.Field(typeof(HurtCollider), "detectionOnly");
    private static readonly FieldInfo? IgnoreLocalPlayerField =
        AccessTools.Field(typeof(HurtCollider), "ignoreLocalPlayer");
    private static readonly string[] InfluencerMessages =
    {
        "Hey, over here!",
        "Look at me!",
        "Everybody, watch this!",
        "I am right here!",
        "Come and get me!"
    };

    private sealed class DynamicUpgradeState
    {
        internal readonly Dictionary<string, int> Baseline =
            new(StringComparer.Ordinal);
        internal readonly Dictionary<string, int> Granted =
            new(StringComparer.Ordinal);
        internal bool Initialized;
        internal int Tier = -1;
    }

    private sealed class ScalingCache
    {
        internal string Source = string.Empty;
        internal IReadOnlyList<UpgradeScalingRule> Rules =
            Array.Empty<UpgradeScalingRule>();
    }

    private sealed class PendingHit
    {
        internal int ColliderId;
        internal int BeforeHealth;
        internal bool EnemyOrigin;
        internal string AttackerSteamId = string.Empty;
        internal int ExpectedDamage;
        internal float ExpiresAt;
    }

    private sealed class PendingRevival
    {
        internal RoleAssignment Assignment = null!;
        internal int Health;
        internal bool Requested;
        internal float ExpiresAt;
    }

    private readonly StageRoleController _controller;
    private readonly StageRolesConfig _config;
    private readonly Dictionary<string, DynamicUpgradeState> _dynamicStates =
        new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<PendingHit>> _pendingHits =
        new(StringComparer.Ordinal);
    private readonly Dictionary<string, PendingRevival> _pendingRevivals =
        new(StringComparer.Ordinal);
    private readonly PendingDamageBudget _internalDamage = new();
    private readonly Dictionary<long, float> _riderKnockbackAllowedAt = new();
    private readonly Dictionary<string, ScalingCache> _scalingCache =
        new(StringComparer.Ordinal);
    private float _nextDynamicCheckAt;
    private float _lastTtsActivityAt;

    internal EventRoleRuntime(
        StageRoleController controller,
        StageRolesConfig config)
    {
        _controller = controller;
        _config = config;
    }

    internal void Begin(IReadOnlyList<RoleAssignment> assignments)
    {
        Stop();
        _nextDynamicCheckAt = 0f;
        _lastTtsActivityAt = Time.time;
        foreach (RoleAssignment assignment in assignments)
        {
            PrepareAssignment(assignment);
        }
    }

    internal void AddPlayer(RoleAssignment assignment) =>
        PrepareAssignment(assignment);

    internal void Stop()
    {
        foreach (KeyValuePair<string, DynamicUpgradeState> pair in _dynamicStates)
        {
            foreach (KeyValuePair<string, int> grant in pair.Value.Granted)
            {
                if (grant.Value == 0)
                {
                    continue;
                }
                UpgradeService.AddLevels(
                    pair.Key,
                    CommandName(grant.Key),
                    -grant.Value);
            }
        }
        _dynamicStates.Clear();
        _pendingHits.Clear();
        _pendingRevivals.Clear();
        _internalDamage.Clear();
        _riderKnockbackAllowedAt.Clear();
        _scalingCache.Clear();
        _nextDynamicCheckAt = 0f;
        _lastTtsActivityAt = 0f;
    }

    internal void Tick(IReadOnlyList<RoleAssignment> assignments)
    {
        foreach (RoleAssignment assignment in assignments)
        {
            if (!PlayerState.IsLiving(assignment.Player))
            {
                _internalDamage.Clear(assignment.SteamId);
                _pendingHits.Remove(assignment.SteamId);
            }
        }
        TickPendingRevivals();
        if (Time.time < _nextDynamicCheckAt)
        {
            return;
        }
        _nextDynamicCheckAt = Time.time + Mathf.Clamp(
            _config.InfluencerPlayerCheckIntervalSeconds.Value,
            0.1f,
            10f);

        foreach (RoleAssignment assignment in assignments)
        {
            bool influencer = RoleCatalog.HasCapability(
                assignment.Role,
                StageRole.Influencer);
            bool berserker = RoleCatalog.HasCapability(
                assignment.Role,
                StageRole.Berserker);
            if (!influencer && !berserker)
            {
                continue;
            }
            DynamicUpgradeState state = GetOrCreateState(assignment);
            if (!state.Initialized)
            {
                continue;
            }
            if (influencer && berserker)
            {
                ApplySuperbotScaling(
                    assignment,
                    state,
                    InfluencerPlayerCount(assignment, assignments),
                    BerserkerHealthPercent(assignment));
            }
            else
            {
                float condition = influencer
                    ? InfluencerPlayerCount(assignment, assignments)
                    : BerserkerHealthPercent(assignment);
                ApplyScaling(assignment, state, condition);
            }
        }
    }

    internal void LateTick(IReadOnlyList<RoleAssignment> assignments)
    {
        if (_controller.HasPendingAutomaticNotifications ||
            RoleNotifier.AnyNotificationBusy())
        {
            _lastTtsActivityAt = Time.time;
            return;
        }
        float quietPeriod = Mathf.Clamp(
            _config.InfluencerTtsQuietPeriodSeconds.Value,
            0f,
            10f);
        if (Time.time - _lastTtsActivityAt < quietPeriod ||
            !RoleNotifier.CanNotify())
        {
            return;
        }

        foreach (RoleAssignment assignment in assignments)
        {
            if (assignment.Role != StageRole.Influencer ||
                !PlayerState.IsLiving(assignment.Player) ||
                Time.time < assignment.InfluencerNextTtsAt)
            {
                continue;
            }
            string message = InfluencerMessages[
                UnityEngine.Random.Range(0, InfluencerMessages.Length)];
            if (!StageFluxCompatibility.TryReserveNotificationWindow(
                    "RoleShuffle.Influencer",
                    8f))
            {
                return;
            }
            try
            {
                assignment.Player.ChatMessageSend(message);
                EnemyDirector.instance?.SetInvestigate(
                    assignment.Player.transform.position,
                    Mathf.Clamp(
                        _config.InfluencerTtsInvestigateRadius.Value,
                        1f,
                        100f));
                assignment.InfluencerNextTtsAt =
                    Time.time + NextInfluencerInterval();
                _lastTtsActivityAt = Time.time;
            }
            catch (Exception exception)
            {
                StageRolesPlugin.ModLogger.LogDebug(
                    $"Influencer TTS was delayed: {exception.Message}");
            }
            break;
        }
    }

    internal bool HasCorrectiveRevivalPending(PlayerAvatar player)
    {
        string steamId = PlayerIdentity.SteamId(player);
        return !string.IsNullOrEmpty(steamId) &&
               _pendingRevivals.ContainsKey(steamId);
    }

    internal void ExternalPlayerRevived(PlayerAvatar player)
    {
        string steamId = PlayerIdentity.SteamId(player);
        if (!string.IsNullOrEmpty(steamId))
        {
            _pendingRevivals.Remove(steamId);
            _internalDamage.Clear(steamId);
            _pendingHits.Remove(steamId);
        }
    }

    internal void ApplyEnemyHitOverride(
        HurtCollider collider,
        PlayerAvatar? attacker)
    {
        PlayerAvatar? rider = ResolveVehicleDriver(collider);
        if (rider == null || !_controller.PlayerHasRole(rider, StageRole.Rider))
        {
            return;
        }
        collider.enemyDamage = Mathf.Clamp(
            Mathf.RoundToInt(
                collider.enemyDamage * Mathf.Clamp(
                    _config.RiderEnemyDamageMultiplier.Value,
                    1f,
                    10f)),
            0,
            100000);
    }

    internal void RegisterPlayerHit(
        HurtCollider collider,
        PlayerAvatar target,
        PlayerAvatar? attacker)
    {
        if (collider == null || target == null ||
            !PlayerState.IsLiving(target) ||
            DetectionOnlyField?.GetValue(collider) is true ||
            collider.ignorePlayers?.Contains(target) == true ||
            (IsLocalPlayerField?.GetValue(target) is true && IgnoreLocalPlayerField?.GetValue(collider) is true))
        {
            return;
        }
        string targetId = PlayerIdentity.SteamId(target);
        if (string.IsNullOrEmpty(targetId))
        {
            return;
        }

        bool enemyOrigin = collider.enemyHost != null;
        string attackerId = PlayerIdentity.SteamId(attacker);
        if (PlayerState.TryGetCurrentHealth(target, out int beforeHealth))
        {
            int expectedDamage = Mathf.Max(0, collider.playerDamage);
            if (collider.playerKill &&
                PlayerState.TryGetCurrentHealth(target, out int currentHealth))
            {
                expectedDamage = currentHealth;
            }
            if (!_pendingHits.TryGetValue(targetId, out List<PendingHit> hits))
                _pendingHits[targetId] = hits = new List<PendingHit>();
            int colliderId = collider.GetInstanceID();
            hits.RemoveAll(hit => hit.ExpiresAt < Time.time || hit.ColliderId == colliderId);
            // Bound the list and fail closed while collisions are ambiguous.
            if (hits.Count >= 16) return;
            hits.Add(new PendingHit
            {
                ColliderId = colliderId,
                BeforeHealth = beforeHealth,
                EnemyOrigin = enemyOrigin,
                AttackerSteamId = ReferenceEquals(attacker, target) ? string.Empty : attackerId,
                ExpectedDamage = expectedDamage,
                ExpiresAt = Time.time + 1.25f
            });
        }

        ApplyRiderPlayerKnockback(collider, target);
    }

    internal void EndPlayerHit(HurtCollider collider, PlayerAvatar target)
    {
        // A local Hurt call executes within this scope. If no HP was lost,
        // discard the candidate immediately, including on exceptional exits.
        if (target == null || (SemiFunc.IsMultiplayer() && !target.photonView.IsMine))
            return;
        string id = PlayerIdentity.SteamId(target);
        if (_pendingHits.TryGetValue(id, out List<PendingHit> hits))
        {
            hits.RemoveAll(hit => hit.ColliderId == collider.GetInstanceID());
            if (hits.Count == 0) _pendingHits.Remove(id);
        }
    }

    internal void ObserveHealthUpdate(
        PlayerAvatar player,
        int previousHealth,
        int currentHealth)
    {
        if (player == null || previousHealth <= currentHealth || previousHealth <= 0)
        {
            return;
        }
        string targetId = PlayerIdentity.SteamId(player);
        if (string.IsNullOrEmpty(targetId))
        {
            return;
        }
        if (_internalDamage.Observe(targetId, previousHealth, currentHealth))
        {
            return;
        }
        if (_internalDamage.Reserved(targetId) > 0)
        {
            // Do not misattribute an unresolved transfer or overlapping hit.
            _pendingHits.Remove(targetId);
            return;
        }
        if (!_pendingHits.Remove(targetId, out List<PendingHit> hits))
        {
            return;
        }
        hits.RemoveAll(candidate => Time.time > candidate.ExpiresAt);
        if (hits.Count != 1) return;
        PendingHit hit = hits[0];

        int observedLoss = previousHealth - currentHealth;
        bool remote = SemiFunc.IsMultiplayer() && !player.photonView.IsMine;
        if (remote && (hit.BeforeHealth != previousHealth ||
            (currentHealth > 0 ? hit.ExpectedDamage != observedLoss : hit.ExpectedDamage < observedLoss)))
            return;
        if (!hit.EnemyOrigin)
        {
            ApplyWerewolfDamage(hit, player, observedLoss);
            return;
        }
        ApplyBodyguardTransfer(
            player,
            previousHealth,
            currentHealth,
            currentHealth <= 0
                ? Mathf.Max(observedLoss, hit.ExpectedDamage)
                : observedLoss);
    }

    private void ApplyWerewolfDamage(
        PendingHit hit,
        PlayerAvatar victim,
        int observedLoss)
    {
        if (string.IsNullOrEmpty(hit.AttackerSteamId) ||
            !_controller.RoleAssignedToPlayer(hit.AttackerSteamId, StageRole.Werewolf))
        {
            return;
        }
        int extraDamage = Mathf.Max(
            0,
            Mathf.RoundToInt(
                observedLoss *
                (Mathf.Clamp(
                    _config.WerewolfPlayerDamageMultiplier.Value,
                    1f,
                    10f) - 1f)));
        if (extraDamage <= 0)
        {
            return;
        }
        try
        {
            SendInternalDamage(victim, extraDamage);
        }
        catch (Exception exception)
        {
            StageRolesPlugin.ModLogger.LogDebug(
                $"Werewolf extra damage was skipped: {exception.Message}");
        }
    }

    private void ApplyBodyguardTransfer(
        PlayerAvatar victim,
        int previousHealth,
        int currentHealth,
        int incomingDamage)
    {
        RoleAssignment? guard = FindNearestBodyguard(victim);
        if (guard == null ||
            !PlayerState.TryGetCurrentHealth(guard.Player, out int guardHealth) ||
            guardHealth <= 1)
        {
            return;
        }
        int requested = Mathf.CeilToInt(
            incomingDamage * Mathf.Clamp(
                _config.BodyguardDamageSharePercent.Value,
                0f,
                100f) / 100f);
        int transferred = Mathf.Min(requested,
            _internalDamage.Available(guard.SteamId, guardHealth));
        if (transferred <= 0)
        {
            return;
        }

        try
        {
            if (!SendInternalDamage(guard.Player, transferred))
                return;
        }
        catch (Exception exception)
        {
            StageRolesPlugin.ModLogger.LogDebug(
                $"Bodyguard transferred damage was skipped: {exception.Message}");
            return;
        }

        int correctedHealth = previousHealth - incomingDamage + transferred;
        if (currentHealth > 0)
        {
            victim.playerHealth?.HealOther(transferred, false);
            return;
        }
        if (correctedHealth <= 0)
        {
            return;
        }
        string victimId = PlayerIdentity.SteamId(victim);
        RoleAssignment? victimAssignment = FindAssignment(victimId);
        if (victimAssignment == null)
        {
            return;
        }
        _pendingRevivals[victimId] = new PendingRevival
        {
            Assignment = victimAssignment,
            Health = correctedHealth,
            ExpiresAt = Time.time + 6f
        };
    }

    private void TickPendingRevivals()
    {
        if (_pendingRevivals.Count == 0)
        {
            return;
        }
        List<string> completed = new();
        foreach (KeyValuePair<string, PendingRevival> pair in _pendingRevivals)
        {
            PendingRevival pending = pair.Value;
            PlayerAvatar player = pending.Assignment.Player;
            if (Time.time > pending.ExpiresAt)
            {
                completed.Add(pair.Key);
                continue;
            }
            if (PlayerState.IsLiving(player))
            {
                PlayerState.SetHealthSynchronized(player, pending.Health);
                completed.Add(pair.Key);
                continue;
            }
            if (!pending.Requested)
            {
                try
                {
                    pending.Requested =
                        PlayerState.TryRequestDeathHeadRevival(player);
                }
                catch (Exception exception)
                {
                    pending.Requested = false;
                    StageRolesPlugin.ModLogger.LogDebug(
                        $"Bodyguard corrective revival was delayed: {exception.Message}");
                }
            }
        }
        foreach (string steamId in completed)
        {
            _pendingRevivals.Remove(steamId);
        }
    }

    private RoleAssignment? FindNearestBodyguard(PlayerAvatar victim)
    {
        float radius = Mathf.Clamp(_config.BodyguardRadius.Value, 1f, 100f);
        float nearestSquared = radius * radius;
        RoleAssignment? nearest = null;
        foreach (RoleAssignment assignment in _controller.Assignments)
        {
            if (!RoleCatalog.HasCapability(
                    assignment.Role,
                    StageRole.Bodyguard) ||
                ReferenceEquals(assignment.Player, victim) ||
                !PlayerState.IsLiving(assignment.Player) ||
                !PlayerState.TryGetCurrentHealth(assignment.Player, out int health) ||
                _internalDamage.Available(assignment.SteamId, health) <= 0)
            {
                continue;
            }
            float distanceSquared =
                (assignment.Player.transform.position - victim.transform.position)
                .sqrMagnitude;
            if (distanceSquared > nearestSquared)
            {
                continue;
            }
            nearestSquared = distanceSquared;
            nearest = assignment;
        }
        return nearest;
    }

    private RoleAssignment? FindAssignment(string steamId)
    {
        foreach (RoleAssignment assignment in _controller.Assignments)
        {
            if (string.Equals(
                    assignment.SteamId,
                    steamId,
                    StringComparison.Ordinal))
            {
                return assignment;
            }
        }
        return null;
    }

    private void ApplyRiderPlayerKnockback(
        HurtCollider collider,
        PlayerAvatar target)
    {
        PlayerAvatar? rider = ResolveVehicleDriver(collider);
        PlayerTumble? tumble =
            PlayerTumbleField?.GetValue(target) as PlayerTumble;
        bool isTumbling = tumble != null &&
            TumbleIsTumblingField?.GetValue(tumble) is bool value && value;
        if (rider == null || ReferenceEquals(rider, target) ||
            !_controller.PlayerHasRole(rider, StageRole.Rider) ||
            tumble == null ||
            (collider.playerTumbleTime <= 0f && !isTumbling))
        {
            return;
        }
        long key = ((long)collider.GetInstanceID() << 32) ^
                   (uint)target.GetInstanceID();
        if (_riderKnockbackAllowedAt.TryGetValue(key, out float allowedAt) &&
            Time.time < allowedAt)
        {
            return;
        }
        _riderKnockbackAllowedAt[key] = Time.time + Mathf.Max(
            0.25f,
            collider.playerDamageCooldown);

        float multiplier = Mathf.Clamp(
            _config.RiderPlayerKnockbackMultiplier.Value,
            1f,
            10f);
        float vanillaForce = Mathf.Max(
            collider.playerTumbleForce,
            collider.playerHitForce);
        float extraForce = vanillaForce * (multiplier - 1f);
        if (extraForce <= 0f)
        {
            return;
        }
        Vector3 direction = target.transform.position - collider.transform.position;
        if (direction.sqrMagnitude <= 0.0001f)
        {
            direction = collider.transform.forward;
        }
        direction.Normalize();
        try
        {
            if (collider.playerTumbleTime > 0f)
            {
                tumble.TumbleRequest(true, false);
                tumble.TumbleOverrideTime(collider.playerTumbleTime);
            }
            tumble.TumbleForce(direction * extraForce);
        }
        catch (Exception exception)
        {
            StageRolesPlugin.ModLogger.LogDebug(
                $"Rider player knockback was skipped: {exception.Message}");
        }
    }

    private PlayerAvatar? ResolveVehicleDriver(HurtCollider collider)
    {
        ItemVehicle? vehicle = collider.GetComponentInParent<ItemVehicle>();
        if (vehicle?.seats == null || vehicle.seats.Length == 0)
        {
            return null;
        }
        return vehicle.seats[0]?.seatedPlayer;
    }

    private void PrepareAssignment(RoleAssignment assignment)
    {
        if (assignment.Role == StageRole.Influencer)
        {
            assignment.InfluencerNextTtsAt = Time.time + NextInfluencerInterval();
        }
        if (RoleCatalog.HasCapability(
                assignment.Role,
                StageRole.Influencer) ||
            RoleCatalog.HasCapability(
                assignment.Role,
                StageRole.Berserker))
        {
            GetOrCreateState(assignment);
        }
    }

    private DynamicUpgradeState GetOrCreateState(RoleAssignment assignment)
    {
        if (_dynamicStates.TryGetValue(
                assignment.SteamId,
                out DynamicUpgradeState state))
        {
            TryInitializeState(assignment, state);
            return state;
        }
        state = new DynamicUpgradeState();
        TryInitializeState(assignment, state);
        _dynamicStates[assignment.SteamId] = state;
        return state;
    }

    private static void TryInitializeState(
        RoleAssignment assignment,
        DynamicUpgradeState state)
    {
        if (state.Initialized)
        {
            return;
        }
        if (UpgradeService.TryGetLevels(
                assignment.SteamId,
                out Dictionary<string, int> current))
        {
            foreach (string dictionaryName in DynamicDictionaryNames())
            {
                state.Baseline[dictionaryName] =
                    KingUpgradeAura.WithoutBonus(assignment.SteamId, dictionaryName,
                        current.GetValueOrDefault(dictionaryName, 0));
                state.Granted[dictionaryName] = 0;
            }
            state.Initialized = true;
        }
    }

    private void ApplyScaling(
        RoleAssignment assignment,
        DynamicUpgradeState state,
        float condition)
    {
        IReadOnlyDictionary<string, ConfigEntry<string>> settings =
            assignment.Role == StageRole.Influencer
                ? _config.InfluencerUpgradeScaling
                : _config.BerserkerUpgradeScaling;
        foreach (DynamicUpgradeDefinition upgrade in RoleUpgradeScaling.Definitions)
        {
            int baseline = state.Baseline.GetValueOrDefault(
                upgrade.DictionaryName,
                0);
            int previousGrant = state.Granted.GetValueOrDefault(
                upgrade.DictionaryName,
                0);
            bool roleControlsUpgrade = settings.TryGetValue(
                upgrade.Name,
                out ConfigEntry<string>? entry);
            int target = roleControlsUpgrade
                ? ResolveScalingTarget(
                    assignment.Role,
                    upgrade,
                    entry!.Value,
                    condition)
                : baseline;
            int desiredGrant = roleControlsUpgrade
                ? Math.Max(0, target - baseline)
                : 0;
            int delta = desiredGrant - previousGrant;
            if (delta != 0 && UpgradeService.AddLevels(
                    assignment.SteamId,
                    upgrade.CommandName,
                    delta))
            {
                state.Granted[upgrade.DictionaryName] = desiredGrant;
            }
        }
    }

    private void ApplySuperbotScaling(
        RoleAssignment assignment,
        DynamicUpgradeState state,
        float nearbyPlayers,
        float healthPercent)
    {
        foreach (DynamicUpgradeDefinition upgrade in RoleUpgradeScaling.Definitions)
        {
            int baseline = state.Baseline.GetValueOrDefault(
                upgrade.DictionaryName,
                0);
            int previousGrant = state.Granted.GetValueOrDefault(
                upgrade.DictionaryName,
                0);
            int target = baseline;
            if (_config.InfluencerUpgradeScaling.TryGetValue(
                    upgrade.Name,
                    out ConfigEntry<string>? influencerEntry))
            {
                target = Math.Max(
                    target,
                    ResolveScalingTarget(
                        StageRole.Influencer,
                        upgrade,
                        influencerEntry.Value,
                        nearbyPlayers));
            }
            if (_config.BerserkerUpgradeScaling.TryGetValue(
                    upgrade.Name,
                    out ConfigEntry<string>? berserkerEntry))
            {
                target = Math.Max(
                    target,
                    ResolveScalingTarget(
                        StageRole.Berserker,
                        upgrade,
                        berserkerEntry.Value,
                        healthPercent));
            }

            int desiredGrant = Math.Max(0, target - baseline);
            int delta = desiredGrant - previousGrant;
            if (delta != 0 && UpgradeService.AddLevels(
                    assignment.SteamId,
                    upgrade.CommandName,
                    delta))
            {
                state.Granted[upgrade.DictionaryName] = desiredGrant;
            }
        }
    }

    private int ResolveScalingTarget(
        StageRole role,
        DynamicUpgradeDefinition upgrade,
        string source,
        float condition)
    {
        string cacheKey = role + ":" + upgrade.Name;
        if (!_scalingCache.TryGetValue(cacheKey, out ScalingCache cache) ||
            !string.Equals(cache.Source, source, StringComparison.Ordinal))
        {
            cache = new ScalingCache { Source = source };
            RoleUpgradeScaling.TryParse(
                source,
                role == StageRole.Influencer ? 30 : 100,
                upgrade.MaximumLevel,
                out IReadOnlyList<UpgradeScalingRule> rules,
                out string error);
            cache.Rules = rules;
            _scalingCache[cacheKey] = cache;
            if (!string.IsNullOrEmpty(error))
            {
                StageRolesPlugin.ModLogger.LogWarning(
                    $"Ignored invalid {cacheKey} scaling pair(s): {error}");
            }
        }
        if (cache.Rules.Count == 0)
        {
            return 0;
        }

        int target = 0;
        if (role == StageRole.Influencer)
        {
            foreach (UpgradeScalingRule rule in cache.Rules)
            {
                if (condition >= rule.Condition)
                {
                    target = rule.Level;
                }
            }
        }
        else
        {
            for (int index = 0; index < cache.Rules.Count; index++)
            {
                if (condition <= cache.Rules[index].Condition)
                {
                    target = cache.Rules[index].Level;
                    break;
                }
            }
        }
        return target;
    }

    private int InfluencerPlayerCount(
        RoleAssignment influencer,
        IReadOnlyList<RoleAssignment> assignments)
    {
        if (!PlayerState.IsLiving(influencer.Player))
        {
            return 0;
        }
        float radius = Mathf.Clamp(_config.InfluencerRadius.Value, 1f, 100f);
        float radiusSquared = radius * radius;
        int count = 0;
        foreach (RoleAssignment other in assignments)
        {
            if (ReferenceEquals(other, influencer) ||
                !PlayerState.IsLiving(other.Player) ||
                (other.Player.transform.position - influencer.Player.transform.position)
                .sqrMagnitude > radiusSquared)
            {
                continue;
            }
            count++;
        }
        return Mathf.Clamp(count, 0, 30);
    }

    private float BerserkerHealthPercent(RoleAssignment assignment)
    {
        if (!PlayerState.IsLiving(assignment.Player) ||
            !PlayerState.TryGetCurrentHealth(assignment.Player, out int health) ||
            !PlayerState.TryGetMaximumHealth(assignment.Player, out int maximum))
        {
            return 101f;
        }
        return Mathf.Clamp(health * 100f / maximum, 0f, 100f);
    }

    private float NextInfluencerInterval()
    {
        float minimum = Mathf.Clamp(
            _config.InfluencerMinimumTtsIntervalSeconds.Value,
            1f,
            300f);
        float maximum = Mathf.Clamp(
            _config.InfluencerMaximumTtsIntervalSeconds.Value,
            1f,
            300f);
        if (minimum > maximum)
        {
            (minimum, maximum) = (maximum, minimum);
        }
        return UnityEngine.Random.Range(minimum, maximum);
    }

    private bool SendInternalDamage(PlayerAvatar player, int amount)
    {
        string steamId = PlayerIdentity.SteamId(player);
        if (string.IsNullOrEmpty(steamId) || player.playerHealth == null ||
            !PlayerState.TryGetCurrentHealth(player, out int before))
            return false;
        PendingDamageBudget.Request request = _internalDamage.Add(steamId, before, amount);
        bool local = !SemiFunc.IsMultiplayer() || player.photonView.IsMine;
        try
        {
            player.playerHealth.HurtOther(amount, Vector3.zero, false);
            if (local)
            {
                return PlayerState.TryGetCurrentHealth(player, out int after) && after < before;
            }
            return true;
        }
        finally
        {
            // Local results are synchronous. Remote reservations are released
            // only by the matching health update, never by a timeout.
            if (local) _internalDamage.Remove(request);
        }
    }

    private static IEnumerable<string> DynamicDictionaryNames()
    {
        foreach (DynamicUpgradeDefinition upgrade in RoleUpgradeScaling.Definitions)
        {
            yield return upgrade.DictionaryName;
        }
    }

    private static string CommandName(string dictionaryName) =>
        dictionaryName switch
        {
            "playerUpgradeStrength" => "Strength",
            "playerUpgradeSpeed" => "Speed",
            "playerUpgradeStamina" => "Stamina",
            "playerUpgradeExtraJump" => "ExtraJump",
            "playerUpgradeTumbleWings" => "TumbleWings",
            "playerUpgradeHealth" => "Health",
            "playerUpgradeRange" => "Range",
            "playerUpgradeLaunch" => "Launch",
            "playerUpgradeTumbleClimb" => "TumbleClimb",
            "playerUpgradeCrouchRest" => "CrouchRest",
            "playerUpgradeMapPlayerCount" => "MapPlayerCount",
            "playerUpgradeDeathHeadBattery" => "DeathHeadBattery",
            _ => throw new ArgumentOutOfRangeException(nameof(dictionaryName))
        };
}
