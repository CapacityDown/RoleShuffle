using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace REPOJP.StageRoles;

internal sealed class DiverRoleRuntime
{
    private static readonly FieldInfo? PlayerInputDirectionRawField =
        AccessTools.Field(typeof(PlayerAvatar), "InputDirectionRaw");
    private const float MinimumFloorNormalY = 0.55f;
    private const float UnderfloorOffset = 1f;
    private const float RecoveryOffset = 1.35f;
    private const float EntryProbeHeight = 0.2f;
    private const float EntryProbeDistance = 1.6f;
    private const float MaximumEntryAimY = -0.35f;
    private const float ExitRayHeight = 0.25f;
    private const float ExitRayDistance = 2.5f;
    private const float ExitDetectionGraceSeconds = 1f;
    private const float ExitReleaseClearance = 1.25f;
    private const float MaximumLevelViewY = 0.35f;
    private const float ExitAssistProbeHeight = 1.5f;
    private const float ExitAssistMaximumGap = 0.8f;
    private const float MaximumExitElevationAboveEntry = 1.5f;
    private const float CeilingRecoverySearchStep = 0.5f;
    private const float CeilingRecoverySearchRadius = 3f;
    private const float CeilingRecoveryCollisionRadius = 0.3f;
    private const float ExitSettlementSeconds = 0.35f;
    private const float MaximumControlledDepth = 3f;
    private const float DeathRetryIntervalSeconds = 0.25f;
    private const float DeathHeadCorrectionSeconds = 10f;
    private const float CooldownDurationMultiplier = 1.5f;
    private readonly StageRolesConfig _config;
    private readonly Dictionary<string, DiveState> _states =
        new(StringComparer.Ordinal);
    private static readonly int[] CountdownMilestones =
        { 30, 20, 10, 5, 4, 3, 2, 1, 0 };

    private sealed class DiveState
    {
        internal DiveState(RoleAssignment assignment)
        {
            Assignment = assignment;
            PlayerTumble? tumble = ResolveTumble(assignment.Player);
            LastPosition = tumble != null
                ? tumble.transform.position
                : assignment.Player.transform.position;
        }

        internal RoleAssignment Assignment { get; set; }
        internal Vector3 LastPosition { get; set; }
        internal Vector3 LastVelocity { get; set; }
        internal Vector3 RecoveryPosition { get; set; }
        internal Vector3 ControlledPosition { get; set; }
        internal Vector3 LastExitProbePosition { get; set; }
        internal Vector3 ExitFloorPoint { get; set; }
        internal Vector3 ExitFloorNormal { get; set; }
        internal Vector3 ExitApproachPosition { get; set; }
        internal float EntryFloorY { get; set; }
        internal float MinimumControlledY { get; set; }
        internal bool AwaitingNeutralInput { get; set; }
        internal Quaternion RecoveryRotation { get; set; }
        internal bool Underfloor { get; set; }
        internal bool PendingDeath { get; set; }
        internal float DiveStartedAt { get; set; }
        internal float ExpiresAt { get; set; }
        internal float CooldownUntil { get; set; }
        internal float NextDeathAttemptAt { get; set; }
        internal int LastCountdownSecond { get; set; } = int.MaxValue;
        internal bool ReadyNotificationPending { get; set; }
        internal PlayerTumble? ActiveTumble { get; set; }
        internal PhysGrabObject? ActivePhys { get; set; }
        internal Rigidbody? ActiveRigidbody { get; set; }
        internal bool MissingBodyWarningLogged { get; set; }
        internal bool ExitPending { get; set; }
        internal bool ExitBelowSurface { get; set; }
        internal bool ZeroCountdownSent { get; set; }
        internal bool ExitSettling { get; set; }
        internal float ExitSettleUntil { get; set; }
        internal Vector3 TimeoutDeathPosition { get; set; }
        internal Quaternion TimeoutDeathRotation { get; set; }
        internal float DeathHeadCorrectionUntil { get; set; }
    }

    internal DiverRoleRuntime(StageRolesConfig config)
    {
        _config = config;
    }

    internal void Begin(IReadOnlyList<RoleAssignment> assignments)
    {
        Stop();
        RefreshAssignments(assignments);
    }

    internal void Tick(
        IReadOnlyList<RoleAssignment> assignments,
        RoleNotifier notifier)
    {
        RefreshAssignments(assignments);
        float now = Time.time;
        foreach (DiveState state in _states.Values)
        {
            PlayerAvatar player = state.Assignment.Player;
            if (!PlayerState.IsLiving(player))
            {
                if (now < state.DeathHeadCorrectionUntil)
                {
                    PlayerState.TryMoveDeathHead(
                        player,
                        state.TimeoutDeathPosition,
                        state.TimeoutDeathRotation);
                }
                if (state.Underfloor)
                {
                    CancelDiveAfterDeath(state);
                }
                state.PendingDeath = false;
                continue;
            }

            if (state.Underfloor)
            {
                int remaining = Mathf.Max(
                    0,
                    Mathf.CeilToInt(state.ExpiresAt - now));
                foreach (int milestone in CountdownMilestones)
                {
                    if (milestone >= state.LastCountdownSecond ||
                        milestone < remaining)
                    {
                        continue;
                    }

                    int announcedSecond = milestone;
                    bool queued = notifier.NotifyCountdown(
                        player,
                        announcedSecond.ToString(
                            System.Globalization.CultureInfo.InvariantCulture),
                        () => state.Underfloor || state.PendingDeath,
                        announcedSecond == 0
                            ? () => state.ZeroCountdownSent = true
                            : null);
                    if (announcedSecond == 0 && !queued)
                    {
                        state.ZeroCountdownSent = true;
                    }
                }
                state.LastCountdownSecond = remaining;
            }

            if (state.Underfloor && now >= state.ExpiresAt)
            {
                RestoreAboveFloor(state, startCooldown: false);
                state.TimeoutDeathPosition = state.RecoveryPosition;
                state.TimeoutDeathRotation = state.RecoveryRotation;
                state.DeathHeadCorrectionUntil =
                    now + DeathHeadCorrectionSeconds;
                state.PendingDeath = true;
                state.NextDeathAttemptAt =
                    now + DeathRetryIntervalSeconds;
            }

            if (state.ReadyNotificationPending &&
                !state.Underfloor && now >= state.CooldownUntil)
            {
                state.ReadyNotificationPending = false;
                notifier.NotifyConditional(
                    player,
                    "DiverReady",
                    () => !state.Underfloor && !state.PendingDeath &&
                          RoleCatalog.HasCapability(
                              state.Assignment.Role,
                              StageRole.Diver));
            }

            if (state.PendingDeath && state.ZeroCountdownSent &&
                now >= state.NextDeathAttemptAt)
            {
                state.NextDeathAttemptAt =
                    now + DeathRetryIntervalSeconds;
                PrepareTimeoutDeathPosition(state, player);
                player.playerHealth?.HurtOther(
                    100000,
                    Vector3.zero,
                    savingGrace: false);
            }
        }
    }

    internal void FixedTick()
    {
        foreach (DiveState state in _states.Values)
        {
            PlayerAvatar? player = state.Assignment.Player;
            if (player == null)
            {
                continue;
            }
            PlayerTumble? tumble = state.Underfloor
                ? state.ActiveTumble
                : ResolveTumble(player);
            if (tumble == null)
            {
                tumble = ResolveTumble(player);
            }
            PhysGrabObject? phys = state.Underfloor
                ? state.ActivePhys
                : ResolvePhysGrabObject(tumble);
            if (phys == null)
            {
                phys = ResolvePhysGrabObject(tumble);
            }
            Rigidbody? rigidbody = state.Underfloor
                ? state.ActiveRigidbody
                : phys != null ? phys.rb : null;
            if (rigidbody == null && phys != null)
            {
                rigidbody = phys.rb;
            }
            if (tumble == null || phys == null || rigidbody == null)
            {
                if (state.Underfloor && !state.MissingBodyWarningLogged)
                {
                    state.MissingBodyWarningLogged = true;
                    StageRolesPlugin.ModLogger.LogWarning(
                        $"Diver body reference was temporarily unavailable; " +
                        $"the dive remains active: {state.Assignment.SteamId}.");
                }
                continue;
            }

            Vector3 position = rigidbody.position;
            if (!state.Underfloor)
            {
                state.LastPosition = position;
                state.LastVelocity = rigidbody.velocity;
                continue;
            }

            if (!PlayerState.IsLiving(player))
            {
                continue;
            }

            if (state.ExitSettling)
            {
                state.ControlledPosition = state.RecoveryPosition;
                state.AwaitingNeutralInput = true;
                MaintainUnderfloorMovement(
                    state,
                    player,
                    tumble,
                    phys,
                    rigidbody);
                if (Time.time >= state.ExitSettleUntil)
                {
                    StageRolesPlugin.ModLogger.LogDebug(
                        $"Diver completed a safe fixed-floor return: " +
                        $"{state.Assignment.SteamId}.");
                    RestoreAboveFloor(state, startCooldown: true);
                }
                continue;
            }

            MaintainUnderfloorMovement(
                state,
                player,
                tumble,
                phys,
                rigidbody);
            Vector3 currentPosition = state.ControlledPosition;
            bool exitDetectionActive = Time.time >=
                state.DiveStartedAt + ExitDetectionGraceSeconds;
            bool levelView =
                TryGetViewForward(player, out Vector3 viewForward) &&
                Mathf.Abs(viewForward.y) <= MaximumLevelViewY;
            if (exitDetectionActive && !state.ExitPending &&
                TryFindCrossedFloor(
                    state.LastExitProbePosition,
                    currentPosition,
                    out RaycastHit crossedFloor))
            {
                state.ExitPending = true;
                state.ExitFloorPoint = crossedFloor.point;
                state.ExitFloorNormal = crossedFloor.normal;
                state.ExitApproachPosition = state.LastPosition;
                state.ExitBelowSurface = IsCeilingExit(
                    state,
                    crossedFloor);
            }

            if (exitDetectionActive && !state.ExitPending && levelView &&
                TryFindNearbyExitFloor(
                    currentPosition,
                    out RaycastHit nearbyFloor))
            {
                state.ExitPending = true;
                state.ExitFloorPoint = nearbyFloor.point;
                state.ExitFloorNormal = nearbyFloor.normal;
                state.ExitApproachPosition = state.LastPosition;
                state.ExitBelowSurface = IsCeilingExit(
                    state,
                    nearbyFloor);
            }

            if (exitDetectionActive && state.ExitPending)
            {
                float clearance = Vector3.Dot(
                    currentPosition - state.ExitFloorPoint,
                    state.ExitFloorNormal);
                if (clearance >= ExitReleaseClearance || levelView)
                {
                    if (state.ExitBelowSurface)
                    {
                        Vector3 preferred = new(
                            state.ExitApproachPosition.x,
                            state.ExitFloorPoint.y - RecoveryOffset,
                            state.ExitApproachPosition.z);
                        state.RecoveryPosition =
                            FindSafeCeilingRecoveryPosition(preferred);
                    }
                    else
                    {
                        state.RecoveryPosition = state.ExitFloorPoint +
                            state.ExitFloorNormal * RecoveryOffset;
                    }
                    state.RecoveryRotation =
                        FlatRotation(player.transform.rotation);
                    state.ControlledPosition = state.RecoveryPosition;
                    state.ExitSettling = true;
                    state.ExitSettleUntil =
                        Time.time + ExitSettlementSeconds;
                    state.ExpiresAt = Mathf.Max(
                        state.ExpiresAt,
                        state.ExitSettleUntil + 0.1f);
                    continue;
                }

                if (clearance < -0.1f)
                {
                    state.ExitPending = false;
                    state.ExitBelowSurface = false;
                    state.LastExitProbePosition = currentPosition;
                }
            }

            if (exitDetectionActive && !state.ExitPending &&
                currentPosition.y <
                state.LastExitProbePosition.y - 0.001f)
            {
                // Preserve the lowest probe height while travelling level or
                // rising slowly. Otherwise a per-frame rise below the crossing
                // epsilon can erase the cumulative floor crossing.
                state.LastExitProbePosition = currentPosition;
            }

            state.LastPosition = currentPosition;
            state.LastVelocity = Vector3.zero;
        }
    }

    internal void PlayerTumbleStarted(PlayerTumble? tumble)
    {
        if (tumble is null || tumble == null)
        {
            return;
        }
        PlayerAvatar? player = tumble.playerAvatar;
        if (player == null || !PlayerState.IsLiving(player))
        {
            return;
        }
        if (!TryGetViewForward(player, out Vector3 viewForward) ||
            viewForward.y > MaximumEntryAimY)
        {
            return;
        }

        string steamId = PlayerIdentity.SteamId(player);
        if (string.IsNullOrEmpty(steamId) ||
            !_states.TryGetValue(steamId, out DiveState state) ||
            state.Underfloor || state.PendingDeath ||
            Time.time < state.CooldownUntil)
        {
            return;
        }

        PhysGrabObject? phys = ResolvePhysGrabObject(tumble);
        Rigidbody? rigidbody = phys != null ? phys.rb : null;
        if (phys == null || rigidbody == null ||
            !TryFindEntryFloor(rigidbody.position, out RaycastHit floor))
        {
            return;
        }

        Vector3 velocity = rigidbody.velocity;
        StartDive(
            state,
            player,
            tumble,
            phys,
            rigidbody,
            floor.point,
            floor.normal,
            velocity);
        StageRolesPlugin.ModLogger.LogDebug(
            $"Diver entered below a fixed floor from player tumble input: {steamId}.");
    }

    internal bool ShouldKeepTumbling(PlayerTumble? tumble)
    {
        PlayerAvatar? player = tumble != null ? tumble.playerAvatar : null;
        if (player == null)
        {
            return false;
        }

        string steamId = PlayerIdentity.SteamId(player);
        return !string.IsNullOrEmpty(steamId) &&
               _states.TryGetValue(steamId, out DiveState state) &&
               state.Underfloor &&
               !state.PendingDeath &&
               PlayerState.IsLiving(player);
    }

    private static bool TryFindEntryFloor(
        Vector3 position,
        out RaycastHit floor)
    {
        floor = default;
        RaycastHit[] hits = Physics.RaycastAll(
            position + Vector3.up * EntryProbeHeight,
            Vector3.down,
            EntryProbeDistance,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore);
        bool found = false;
        float nearestDistance = float.MaxValue;
        foreach (RaycastHit hit in hits)
        {
            if (hit.distance >= nearestDistance ||
                hit.normal.y < MinimumFloorNormalY ||
                !IsFixedFloor(hit.collider))
            {
                continue;
            }
            floor = hit;
            nearestDistance = hit.distance;
            found = true;
        }
        return found;
    }

    private void StartDive(
        DiveState state,
        PlayerAvatar player,
        PlayerTumble tumble,
        PhysGrabObject phys,
        Rigidbody rigidbody,
        Vector3 floorPoint,
        Vector3 floorNormal,
        Vector3 velocity)
    {
        state.Underfloor = true;
        state.PendingDeath = false;
        state.DiveStartedAt = Time.time;
        float duration = Mathf.Clamp(
            _config.DiverUnderfloorDurationSeconds.Value,
            1f,
            120f);
        state.ExpiresAt = Time.time + duration;
        state.LastCountdownSecond = Mathf.CeilToInt(duration) + 1;
        state.ReadyNotificationPending = false;
        state.ActiveTumble = tumble;
        state.ActivePhys = phys;
        state.ActiveRigidbody = rigidbody;
        state.MissingBodyWarningLogged = false;
        state.ExitPending = false;
        state.ExitBelowSurface = false;
        state.ZeroCountdownSent = false;
        state.ExitSettling = false;
        state.DeathHeadCorrectionUntil = 0f;
        Vector3 aboveEntry = floorPoint + floorNormal * RecoveryOffset;
        bool enteredFromOutsideRoom =
            player.RoomVolumeCheck != null &&
            player.RoomVolumeCheck.CurrentRooms != null &&
            player.RoomVolumeCheck.CurrentRooms.Count == 0;
        state.RecoveryPosition = enteredFromOutsideRoom
            ? FindSafeCeilingRecoveryPosition(new Vector3(
                floorPoint.x,
                floorPoint.y - RecoveryOffset,
                floorPoint.z))
            : aboveEntry;
        state.RecoveryRotation = FlatRotation(player.transform.rotation);
        state.EntryFloorY = floorPoint.y;
        SetTumbleColliders(tumble, enabled: false);
        Vector3 entryPosition = floorPoint - floorNormal * UnderfloorOffset;
        phys.Teleport(entryPosition, rigidbody.rotation);
        rigidbody.position = entryPosition;
        rigidbody.velocity = Vector3.ProjectOnPlane(velocity, floorNormal);
        rigidbody.angularVelocity = Vector3.zero;
        state.ControlledPosition = entryPosition;
        state.LastExitProbePosition = entryPosition;
        state.MinimumControlledY = floorPoint.y - MaximumControlledDepth;
        state.AwaitingNeutralInput = true;
        state.LastPosition = entryPosition;
        state.LastVelocity = rigidbody.velocity;
        MaintainUnderfloorMovement(
            state,
            player,
            tumble,
            phys,
            rigidbody);
    }

    internal void Stop()
    {
        foreach (DiveState state in _states.Values)
        {
            if (state.Underfloor)
            {
                RestoreAboveFloor(state, startCooldown: false);
            }
        }
        _states.Clear();
    }

    private void RefreshAssignments(IReadOnlyList<RoleAssignment> assignments)
    {
        HashSet<string> activeIds = new(StringComparer.Ordinal);
        foreach (RoleAssignment assignment in assignments)
        {
            if (!RoleCatalog.HasCapability(assignment.Role, StageRole.Diver))
            {
                continue;
            }
            activeIds.Add(assignment.SteamId);
            if (!_states.TryGetValue(assignment.SteamId, out DiveState state))
            {
                state = new DiveState(assignment);
                _states.Add(assignment.SteamId, state);
            }
            else
            {
                state.Assignment = assignment;
            }
        }

        List<string> removed = new();
        foreach (KeyValuePair<string, DiveState> pair in _states)
        {
            if (!activeIds.Contains(pair.Key))
            {
                removed.Add(pair.Key);
            }
        }
        foreach (string steamId in removed)
        {
            DiveState state = _states[steamId];
            if (state.Underfloor)
            {
                RestoreAboveFloor(state, startCooldown: false);
            }
            _states.Remove(steamId);
        }
    }

    private void MaintainUnderfloorMovement(
        DiveState state,
        PlayerAvatar player,
        PlayerTumble tumble,
        PhysGrabObject phys,
        Rigidbody rigidbody)
    {
        tumble.TumbleRequest(_isTumbling: true, _playerInput: false);
        tumble.TumbleOverrideTime(0.5f);
        tumble.DisableCustomGravity(0.5f);
        tumble.OverrideEnemyHurt(0.2f);
        phys.OverrideZeroGravity(0.5f);
        phys.OverrideDrag(1f);
        phys.OverrideAngularDrag(1.5f);
        phys.DisableDeathPitEffect(0.5f);
        rigidbody.useGravity = false;
        SetTumbleColliders(tumble, enabled: false);

        Transform view = player.localCamera != null
            ? player.localCamera.GetOverrideTransform()
            : player.transform;
        Vector3 input = ReadInputDirection(player);
        if (state.AwaitingNeutralInput)
        {
            if (input.sqrMagnitude <= 0.01f)
            {
                state.AwaitingNeutralInput = false;
            }
            input = Vector3.zero;
        }
        Vector3 direction = view.forward * input.z + view.right * input.x;
        if (direction.sqrMagnitude > 1f)
        {
            direction.Normalize();
        }
        float speed = Mathf.Clamp(_config.DiverMovementForce.Value, 1f, 30f);
        if (direction.sqrMagnitude > 0.0001f)
        {
            state.ControlledPosition +=
                direction * speed * Time.fixedDeltaTime;
            state.ControlledPosition = new Vector3(
                state.ControlledPosition.x,
                Mathf.Max(
                    state.MinimumControlledY,
                    state.ControlledPosition.y),
                state.ControlledPosition.z);
        }
        phys.Teleport(state.ControlledPosition, rigidbody.rotation);
        rigidbody.position = state.ControlledPosition;
        rigidbody.velocity = Vector3.zero;
        rigidbody.angularVelocity = Vector3.zero;
    }

    private static void CancelDiveAfterDeath(DiveState state)
    {
        state.Underfloor = false;
        state.PendingDeath = false;
        state.ReadyNotificationPending = false;
        state.AwaitingNeutralInput = false;
        state.LastCountdownSecond = int.MaxValue;
        state.LastVelocity = Vector3.zero;
        state.ActiveTumble = null;
        state.ActivePhys = null;
        state.ActiveRigidbody = null;
        state.MissingBodyWarningLogged = false;
        state.ExitPending = false;
        state.ExitBelowSurface = false;
        state.ZeroCountdownSent = false;
        state.ExitSettling = false;
    }

    private void RestoreAboveFloor(DiveState state, bool startCooldown)
    {
        PlayerAvatar player = state.Assignment.Player;
        PlayerTumble? tumble = state.ActiveTumble;
        if (tumble == null)
        {
            tumble = ResolveTumble(player);
        }
        PhysGrabObject? phys = state.ActivePhys;
        if (phys == null)
        {
            phys = ResolvePhysGrabObject(tumble);
        }
        Rigidbody? rigidbody = state.ActiveRigidbody;
        if (rigidbody == null && phys != null)
        {
            rigidbody = phys.rb;
        }
        state.Underfloor = false;
        state.LastCountdownSecond = int.MaxValue;
        state.AwaitingNeutralInput = false;
        if (startCooldown)
        {
            float previousDiveSeconds = Mathf.Max(
                0f,
                Time.time - state.DiveStartedAt);
            state.CooldownUntil = Time.time +
                previousDiveSeconds * CooldownDurationMultiplier;
            state.ReadyNotificationPending = true;
        }
        if (player == null)
        {
            return;
        }

        Vector3 position = state.RecoveryPosition;
        Quaternion rotation = state.RecoveryRotation;
        if (phys != null)
        {
            phys.Teleport(position, rotation);
        }
        if (rigidbody != null)
        {
            rigidbody.position = position;
            rigidbody.rotation = rotation;
            rigidbody.velocity = Vector3.zero;
            rigidbody.angularVelocity = Vector3.zero;
        }
        player.Spawn(position, rotation);
        player.FallDamageResetSet(2f);
        if (tumble != null)
        {
            // TumbleRequest skips synchronization when its local state already
            // says false. Diver can intentionally keep the authoritative body
            // tumbling longer than that local flag, so always publish the final
            // vanilla tumble state when returning above a floor.
            tumble.TumbleSet(_isTumbling: false, _playerInput: false);
            SetTumbleColliders(tumble, enabled: false);
        }
        state.LastPosition = position;
        state.LastVelocity = Vector3.zero;
        state.ActiveTumble = null;
        state.ActivePhys = null;
        state.ActiveRigidbody = null;
        state.MissingBodyWarningLogged = false;
        state.ExitPending = false;
        state.ExitBelowSurface = false;
        state.ExitSettling = false;
    }

    private static void PrepareTimeoutDeathPosition(
        DiveState state,
        PlayerAvatar player)
    {
        Vector3 position = state.TimeoutDeathPosition;
        Quaternion rotation = state.TimeoutDeathRotation;
        player.Spawn(position, rotation);
        player.transform.position = position;
        player.transform.rotation = rotation;
        Rigidbody? avatarBody = player.GetComponent<Rigidbody>();
        if (avatarBody != null)
        {
            avatarBody.position = position;
            avatarBody.rotation = rotation;
            avatarBody.velocity = Vector3.zero;
            avatarBody.angularVelocity = Vector3.zero;
        }
        PlayerAvatarCollision? avatarCollision =
            player.GetComponent<PlayerAvatarCollision>();
        if (avatarCollision != null &&
            avatarCollision.CollisionTransform != null)
        {
            avatarCollision.CollisionTransform.position = position;
            avatarCollision.CollisionTransform.rotation = rotation;
        }
        PlayerTumble? tumble = ResolveTumble(player);
        PhysGrabObject? tumblePhys = ResolvePhysGrabObject(tumble);
        if (tumblePhys != null)
        {
            tumblePhys.Teleport(position, rotation);
            if (tumblePhys.rb != null)
            {
                tumblePhys.rb.position = position;
                tumblePhys.rb.rotation = rotation;
                tumblePhys.rb.velocity = Vector3.zero;
                tumblePhys.rb.angularVelocity = Vector3.zero;
            }
        }
    }

    private static bool TryFindCrossedFloor(
        Vector3 previousPosition,
        Vector3 currentPosition,
        out RaycastHit floorHit)
    {
        floorHit = default;
        float upwardDistance = currentPosition.y - previousPosition.y;
        if (upwardDistance <= 0.01f)
        {
            return false;
        }

        Vector3 origin = currentPosition + Vector3.up * ExitRayHeight;
        float rayDistance = Mathf.Max(
            ExitRayDistance,
            upwardDistance + ExitRayHeight + 0.5f);
        RaycastHit[] hits = Physics.RaycastAll(
            origin,
            Vector3.down,
            rayDistance,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore);
        bool found = false;
        float highestFloor = float.MinValue;
        foreach (RaycastHit hit in hits)
        {
            if (!IsFixedFloor(hit.collider) ||
                hit.normal.y < MinimumFloorNormalY ||
                previousPosition.y >= hit.point.y - 0.02f ||
                currentPosition.y <= hit.point.y + 0.02f ||
                hit.point.y <= highestFloor)
            {
                continue;
            }

            floorHit = hit;
            highestFloor = hit.point.y;
            found = true;
        }
        return found;
    }

    private static bool TryFindNearbyExitFloor(
        Vector3 currentPosition,
        out RaycastHit floorHit)
    {
        floorHit = default;
        RaycastHit[] hits = Physics.RaycastAll(
            currentPosition + Vector3.up * ExitAssistProbeHeight,
            Vector3.down,
            ExitAssistProbeHeight + ExitAssistMaximumGap + 0.5f,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore);
        bool found = false;
        float nearestGap = float.MaxValue;
        foreach (RaycastHit hit in hits)
        {
            if (!IsFixedFloor(hit.collider) ||
                hit.normal.y < MinimumFloorNormalY)
            {
                continue;
            }

            float gap = hit.point.y - currentPosition.y;
            if (gap < -0.5f || gap > ExitAssistMaximumGap ||
                Mathf.Abs(gap) >= nearestGap)
            {
                continue;
            }

            floorHit = hit;
            nearestGap = Mathf.Abs(gap);
            found = true;
        }
        return found;
    }

    private static bool IsCeilingExit(
        DiveState state,
        RaycastHit floorHit) =>
        floorHit.point.y >
        state.EntryFloorY + MaximumExitElevationAboveEntry;

    private static Vector3 FindSafeCeilingRecoveryPosition(
        Vector3 preferred)
    {
        if (IsSafeCeilingRecoveryPosition(preferred))
        {
            return preferred;
        }

        int steps = Mathf.CeilToInt(
            CeilingRecoverySearchRadius / CeilingRecoverySearchStep);
        for (int step = 1; step <= steps; step++)
        {
            float radius = step * CeilingRecoverySearchStep;
            for (int directionIndex = 0; directionIndex < 8; directionIndex++)
            {
                float angle = directionIndex * 45f * Mathf.Deg2Rad;
                Vector3 candidate = preferred + new Vector3(
                    Mathf.Cos(angle) * radius,
                    0f,
                    Mathf.Sin(angle) * radius);
                if (IsSafeCeilingRecoveryPosition(candidate))
                {
                    return candidate;
                }
            }
        }
        return preferred;
    }

    private static bool IsSafeCeilingRecoveryPosition(Vector3 position)
    {
        int roomVolumeMask = LayerMask.GetMask("RoomVolume");
        if (roomVolumeMask != 0 &&
            !Physics.CheckSphere(
                position,
                0.1f,
                roomVolumeMask,
                QueryTriggerInteraction.Collide))
        {
            return false;
        }

        LayerMask obstructionMask = SemiFunc.LayerMaskGetVisionObstruct();
        return !Physics.CheckCapsule(
            position + Vector3.up * 0.35f,
            position - Vector3.up * 0.35f,
            CeilingRecoveryCollisionRadius,
            obstructionMask,
            QueryTriggerInteraction.Ignore);
    }

    private static bool IsFixedFloor(Collider? collider)
    {
        if (collider == null || collider.isTrigger ||
            collider.GetComponentInParent<PhysGrabObject>() != null ||
            collider.GetComponentInParent<PlayerAvatar>() != null ||
            collider.GetComponentInParent<Enemy>() != null)
        {
            return false;
        }
        Rigidbody? attached = collider.attachedRigidbody;
        return attached == null || attached.isKinematic;
    }

    private static Quaternion FlatRotation(Quaternion rotation) =>
        Quaternion.Euler(0f, rotation.eulerAngles.y, 0f);

    private static PlayerTumble? ResolveTumble(PlayerAvatar? player) =>
        player != null
            ? player.GetComponentInChildren<PlayerTumble>(includeInactive: true)
            : null;

    private static PhysGrabObject? ResolvePhysGrabObject(
        PlayerTumble? tumble) =>
        tumble != null ? tumble.GetComponent<PhysGrabObject>() : null;

    private static Vector3 ReadInputDirection(PlayerAvatar player)
    {
        try
        {
            if (ReferenceEquals(player, SemiFunc.PlayerGetLocal()) &&
                PlayerController.instance != null)
            {
                return PlayerController.instance.InputDirectionRaw;
            }
            return PlayerInputDirectionRawField?.GetValue(player) is Vector3 input
                ? input
                : Vector3.zero;
        }
        catch
        {
            return Vector3.zero;
        }
    }

    private static bool TryGetViewForward(
        PlayerAvatar player,
        out Vector3 forward)
    {
        forward = Vector3.zero;
        try
        {
            Transform? view = player.localCamera != null
                ? player.localCamera.GetOverrideTransform()
                : null;
            if (view == null || view.forward.sqrMagnitude <= 0.001f)
            {
                return false;
            }
            forward = view.forward.normalized;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void SetTumbleColliders(PlayerTumble tumble, bool enabled)
    {
        if (tumble.colliders == null)
        {
            return;
        }
        foreach (Collider collider in tumble.colliders)
        {
            if (collider != null)
            {
                collider.enabled = enabled;
            }
        }
    }
}
