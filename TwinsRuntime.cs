using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Photon.Pun;
using UnityEngine;

namespace REPOJP.StageRoles;

internal sealed partial class StageRoleController
{
    private sealed class TwinPair(RoleAssignment first, RoleAssignment second)
    {
        internal readonly RoleAssignment[] Members = { first, second };
        internal readonly PlayerAvatar[] Players = { first.Player, second.Player };
        internal readonly int[] Maximum = new int[2];
        internal readonly int[] SpeedBonus = new int[2];
        internal readonly Vector3[] Positions = { first.Player.transform.position, second.Player.transform.position };
        internal readonly TwinsCooperation Cooperation = new();
        internal readonly HashSet<PhysGrabObject> Cargo = new();
        internal int Health;
        internal int SharedMaximum => (int)Math.Min(int.MaxValue, (long)Maximum[0] + Maximum[1]);
        internal bool Dirty;
        internal bool Dead;
        internal bool DeathSent;
        internal PlayerAvatar? RevivalSource;
        internal float RevivalAt;
        internal float LastTick = Time.time;
        internal float NextTick;
    }

    private TwinPair? _twins;
    internal bool WritingTwinsHealth { get; private set; }
    private static readonly FieldInfo? TwinCrouching = AccessTools.Field(typeof(PlayerAvatar), "isCrouching");
    private static readonly FieldInfo? TwinSprinting = AccessTools.Field(typeof(PlayerAvatar), "isSprinting");
    private static readonly FieldInfo? TwinDead = AccessTools.Field(typeof(PlayerAvatar), "deadSet");
    private static readonly FieldInfo? TwinValue = AccessTools.Field(typeof(ValuableObject), "dollarValueCurrent");

    internal bool IsTwin(PlayerAvatar? player) => _twins != null && player != null &&
        (_twins.Players[0] == player || _twins.Players[1] == player);

    private int TwinIndex(PlayerAvatar player) => _twins?.Players[0] == player ? 0 : 1;

    private RoleAssignment? UnlinkTwinForInfection(RoleAssignment assignment)
    {
        RoleAssignment? partner = IsTwin(assignment.Player) ? _twins!.Members[1 - TwinIndex(assignment.Player)] : null;
        if (partner != null) StopTwins();
        return partner;
    }

    private void BeginTwins()
    {
        if (_twins != null || !IsAuthority()) return;
        List<RoleAssignment> twins = _assignments.FindAll(a => a.Role == StageRole.Twins);
        if (twins.Count != 2 || !PlayerState.IsLiving(twins[0].Player) || !PlayerState.IsLiving(twins[1].Player)) return;
        TwinPair pair = new(twins[0], twins[1]);
        long total = 0;
        for (int i = 0; i < 2; i++)
        {
            if (!PlayerState.TryGetMaximumHealth(pair.Players[i], out pair.Maximum[i]) ||
                !PlayerState.TryGetCurrentHealth(pair.Players[i], out int health)) return;
            total += Math.Clamp(health, 0, pair.Maximum[i]);
        }
        pair.Health = (int)Math.Min(pair.SharedMaximum, total);
        pair.Dirty = true;
        _twins = pair;
        FlushTwinsHealth();
        for (int i = 0; i < 2; i++)
        {
            PlayerAvatar receiver = pair.Players[i];
            string name = string.Concat(PlayerIdentity.Name(pair.Players[1 - i]).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
            if (name.Length > 20) name = name.Substring(0, 20);
            _notifier.NotifyConditional(receiver, "Twin:" + name, () => _twins == pair && PlayerState.IsLiving(receiver));
        }
    }

    private void StopTwins()
    {
        TwinPair? pair = _twins;
        if (pair == null) return;
        _twins = null; // Disable observation before restoring either member.
        for (int i = 0; i < 2; i++)
        {
            SetTwinsSpeed(pair, i, false);
            int health = HealthUpgradeReset.ScaleRemaining(pair.Dead ? 0 : pair.Health, pair.SharedMaximum, pair.Maximum[i]);
            WriteTwinsHealth(pair.Players[i], health, pair.Maximum[i]);
            if (UpgradeService.Ready) PunManager.instance.UpdateStat("playerHealth", pair.Members[i].SteamId, health);
        }
    }

    internal void ObserveTwinsHealth(PlayerAvatar player, int previous, int current, int maximum)
    {
        TwinPair? pair = _twins;
        if (pair == null || WritingTwinsHealth || !RoleAssignmentsReady || !IsTwin(player) || previous < 0) return;
        if (pair.Dead)
        {
            // Native revival heals the owner to 1 before the configured rescue heal.
            // Use one source's final restored HP, never add the second revival's 1 HP.
            if (pair.RevivalSource == player && current > 0)
                pair.Health = Math.Max(pair.Health, Math.Clamp(current, 1, pair.SharedMaximum));
            return;
        }
        if (maximum != pair.SharedMaximum)
        {
            int i = TwinIndex(player);
            pair.Maximum[i] = (int)Math.Clamp((long)pair.Maximum[i] + maximum - pair.SharedMaximum, 1L, int.MaxValue / 2L);
        }
        int delta = current - previous;
        if (delta < 0) pair.Cooperation.InterruptRest();
        pair.Health = (int)Math.Clamp((long)pair.Health + delta, 0L, pair.SharedMaximum);
        pair.Dirty = true;
        if (current == 0 || pair.Health == 0) TwinDied(player);
    }

    internal void TwinDied(PlayerAvatar player)
    {
        TwinPair? pair = _twins;
        if (pair == null || WritingTwinsHealth || !RoleAssignmentsReady || !IsTwin(player)) return;
        if (pair.Health > 0 && TwinDead?.GetValue(player) is not true && PlayerState.TryGetCurrentHealth(player, out int current) && current > 0) return;
        if (pair.DeathSent && pair.RevivalSource == null) return;
        pair.Health = 0;
        pair.Dead = true;
        pair.DeathSent = true;
        pair.RevivalSource = null;
        pair.Cooperation.InterruptRest();
        for (int i = 0; i < 2; i++)
        {
            SetTwinsSpeed(pair, i, false);
            PlayerAvatar member = pair.Players[i];
            if (member == null || TwinDead?.GetValue(member) is true) continue;
            // Native death RPC includes the normal owner-side cleanup. HP=0 alone does not.
            if (SemiFunc.IsMultiplayer()) member.photonView.RPC(nameof(PlayerAvatar.PlayerDeathRPC), RpcTarget.All, -1);
            else member.PlayerDeathRPC(-1);
        }
        pair.Dirty = true;
    }

    internal void TwinRevived(PlayerAvatar player)
    {
        TwinPair? pair = _twins;
        if (pair == null || !pair.Dead || !IsTwin(player) || pair.RevivalSource != null || TwinDead?.GetValue(player) is not false) return;
        pair.RevivalSource = player;
        pair.RevivalAt = Time.time;
        pair.Health = PlayerState.TryGetCurrentHealth(player, out int health) ? Math.Max(1, health) : 1;
        PlayerAvatar other = pair.Players[1 - TwinIndex(player)];
        if (other != null && TwinDead?.GetValue(other) is true) PlayerState.TryRequestDeathHeadRevival(other);
    }

    private void TickTwins()
    {
        TwinPair? pair = _twins;
        if (pair == null) return;
        if (pair.Members[0].Role != StageRole.Twins || pair.Members[1].Role != StageRole.Twins ||
            !_assignments.Contains(pair.Members[0]) || !_assignments.Contains(pair.Members[1]) ||
            pair.Members[0].Player != pair.Players[0] || pair.Members[1].Player != pair.Players[1])
        { StopTwins(); return; }
        if (pair.Dead)
        {
            if (pair.RevivalSource != null && Time.time >= pair.RevivalAt + 0.35f)
            {
                PlayerAvatar other = pair.Players[1 - TwinIndex(pair.RevivalSource)];
                if (TwinDead?.GetValue(other) is true) PlayerState.TryRequestDeathHeadRevival(other);
                else
                {
                    pair.Dead = pair.DeathSent = false;
                    pair.Health = Math.Clamp(pair.Health, 1, pair.SharedMaximum);
                    pair.RevivalSource = null;
                    pair.Dirty = true;
                }
            }
            FlushTwinsHealth();
            return;
        }
        if (!PlayerState.IsLiving(pair.Players[0]) || !PlayerState.IsLiving(pair.Players[1])) return;
        if (Time.time >= pair.NextTick)
        {
            float elapsed = Math.Max(0.001f, Time.time - pair.LastTick);
            pair.LastTick = Time.time;
            pair.NextTick = Time.time + 0.1f;
            float distance = Vector3.Distance(pair.Players[0].transform.position, pair.Players[1].transform.position);
            bool rest = distance <= _config.TwinsRestRadius.Value;
            for (int i = 0; i < 2; i++)
            {
                PlayerAvatar player = pair.Players[i];
                Vector3 position = player.transform.position;
                Vector3 movement = position - pair.Positions[i];
                Vector3 toward = pair.Players[1 - i].transform.position - position;
                rest &= TwinCrouching?.GetValue(player) is true && movement.magnitude / elapsed < 0.12f;
                movement.y = toward.y = 0f;
                bool approaching = TwinSprinting?.GetValue(player) is true && movement.magnitude / elapsed > 0.3f &&
                    Vector3.Dot(movement.normalized, toward.normalized) > 0.5f;
                SetTwinsSpeed(pair, i, pair.Cooperation.Rendezvous(i, distance, approaching,
                    _config.TwinsRendezvousStartRange.Value, _config.TwinsRendezvousEndRange.Value));
                pair.Positions[i] = position;
            }
            if (pair.Cooperation.Rest(rest, false, Time.time, _config.TwinsRestHoldSeconds.Value, _config.TwinsRestCooldownSeconds.Value))
            {
                int amount = Mathf.CeilToInt(pair.SharedMaximum * _config.TwinsRestHealPercent.Value / 100f);
                pair.Health = (int)Math.Min(pair.SharedMaximum, (long)pair.Health + amount);
                pair.Dirty = true;
                foreach (RoleAssignment member in pair.Members)
                {
                    // Native Energy upgrades refill stamina. Restore the original level immediately.
                    if (UpgradeService.AddLevelsHostAuthoritative(member.SteamId, "Stamina", 1))
                        UpgradeService.AddLevelsHostAuthoritative(member.SteamId, "Stamina", -1);
                    _notifier.NotifyResponse(member.Player, "Rested");
                }
            }
            ObserveTwinsCargo(pair);
        }
        FlushTwinsHealth();
    }

    private void FlushTwinsHealth()
    {
        TwinPair? pair = _twins;
        if (pair == null || !pair.Dirty || (pair.Dead && pair.RevivalSource != null)) return;
        pair.Dirty = false;
        foreach (PlayerAvatar player in pair.Players) WriteTwinsHealth(player, pair.Health, pair.SharedMaximum);
    }

    private void WriteTwinsHealth(PlayerAvatar player, int health, int maximum)
    {
        if (player == null || player.playerHealth == null) return;
        bool previous = WritingTwinsHealth;
        WritingTwinsHealth = true;
        try
        {
            if (SemiFunc.IsMultiplayer())
                player.photonView.RPC(nameof(PlayerHealth.UpdateHealthRPC), RpcTarget.All, health, maximum, false, false);
            else player.playerHealth.UpdateHealthRPC(health, maximum, false, false);
        }
        finally { WritingTwinsHealth = previous; }
    }

    internal void ForgetTwinsSpeed(string steamId, string key)
    {
        if (_twins == null || key != "playerUpgradeSpeed") return;
        for (int i = 0; i < 2; i++) if (_twins.Members[i].SteamId == steamId) _twins.SpeedBonus[i] = 0;
    }

    private void SetTwinsSpeed(TwinPair pair, int i, bool active)
    {
        string id = pair.Members[i].SteamId;
        if (!UpgradeService.TryGetLevels(id, out var levels)) return;
        int current = levels.GetValueOrDefault("playerUpgradeSpeed", 0);
        int baseline = Math.Max(0, current - pair.SpeedBonus[i]);
        int target = active ? Math.Max(baseline, (int)Math.Ceiling((5d + baseline) * _config.TwinsRendezvousSpeedMultiplier.Value - 5d)) : baseline;
        target = Math.Max(baseline, Math.Min(200, target));
        if (target != current)
        {
            UpgradeService.AddLevelsHostAuthoritative(id, "Speed", target - current);
            if (UpgradeService.TryGetLevels(id, out var after))
                pair.SpeedBonus[i] = Math.Max(0, pair.SpeedBonus[i] + after.GetValueOrDefault("playerUpgradeSpeed", 0) - current);
        }
    }

    internal bool TwinsCarrying(PhysGrabObject? item)
    {
        TwinPair? pair = _twins;
        return RoleAssignmentsReady && pair != null && !pair.Dead && item != null &&
            item.GetComponent<ItemAttributes>() == null && TwinValuable(item) != null &&
            PlayerState.IsLiving(pair.Players[0]) && PlayerState.IsLiving(pair.Players[1]) &&
            UtilityRoleRuntime.IsHeldBy(item, pair.Members[0]) && UtilityRoleRuntime.IsHeldBy(item, pair.Members[1]);
    }

    internal float TwinsGrabMultiplier(PhysGrabObject item, PlayerAvatar player) =>
        IsTwin(player) && TwinsCarrying(item) ? _config.TwinsCarryStrengthMultiplier.Value : 1f;

    internal float TwinsCollisionMultiplier(PhysGrabObject item) => TwinsCarrying(item)
        ? 1f - _config.TwinsCollisionReductionPercent.Value / 100f : 1f;

    private static ValuableObject? TwinValuable(PhysGrabObject item) => item.GetComponent<ValuableObject>() ??
        item.GetComponentInChildren<ValuableObject>() ?? item.GetComponentInParent<ValuableObject>();

    private static bool TwinsDeliveryArea(PhysGrabObject item, bool refresh)
    {
        RoomVolumeCheck? rooms = item.GetComponent<RoomVolumeCheck>() ?? item.GetComponentInChildren<RoomVolumeCheck>() ?? item.GetComponentInParent<RoomVolumeCheck>();
        if (refresh && rooms != null) rooms.CheckSet();
        if (rooms?.CurrentRooms != null)
            foreach (RoomVolume room in rooms.CurrentRooms)
                if (room != null && (room.Truck || room.Extraction)) return true;
        return false;
    }

    private void ObserveTwinsCargo(TwinPair pair)
    {
        PhysGrabObject? shared = null;
        foreach (PhysGrabObject item in UnityEngine.Object.FindObjectsOfType<PhysGrabObject>())
            if (TwinsCarrying(item)) { shared = item; pair.Cargo.Add(item); break; }
        pair.Cooperation.ObserveCarry(shared == null ? 0 : shared.GetInstanceID(), shared != null,
            shared != null && TwinsDeliveryArea(shared, false), Time.time, _config.TwinsDeliveryCarrySeconds.Value);
        pair.Cargo.RemoveWhere(item => item == null);
        foreach (PhysGrabObject item in pair.Cargo) TryTwinsDelivery(pair, item, false);
    }

    internal void TwinsCargoReleased(PhysGrabObject item)
    {
        if (_twins != null && _twins.Cargo.Contains(item)) TryTwinsDelivery(_twins, item, true);
    }

    private void TryTwinsDelivery(TwinPair pair, PhysGrabObject item, bool refresh)
    {
        ValuableObject? valuable = TwinValuable(item);
        if (valuable == null || TwinValue?.GetValue(valuable) is not float value || !float.IsFinite(value)) return;
        PhotonView? view = valuable.GetComponent<PhotonView>() ?? valuable.GetComponentInParent<PhotonView>();
        if (SemiFunc.IsMultiplayer() && (view == null || view.ViewID == 0)) return;
        bool nearby = PlayerState.IsLiving(pair.Players[0]) && PlayerState.IsLiving(pair.Players[1]);
        foreach (PlayerAvatar player in pair.Players)
            nearby &= Vector3.Distance(player.transform.position, item.centerPoint) <= _config.TwinsDeliveryRadius.Value;
        int bonus = pair.Cooperation.Deliver(item.GetInstanceID(), TwinsDeliveryArea(item, refresh), nearby,
            value, _config.TwinsDeliveryBonusPercent.Value, _config.TwinsDeliveryStageLimit.Value);
        if (bonus == 0) return;
        if (SemiFunc.IsMultiplayer()) view!.RPC(nameof(ValuableObject.DollarValueSetRPC), RpcTarget.All, value + bonus);
        else valuable.DollarValueSetRPC(value + bonus);
        foreach (PlayerAvatar player in pair.Players) _notifier.NotifyResponse(player, "Delivered");
    }
}
