using System;
using System.Reflection;
using HarmonyLib;
using Photon.Pun;
using UnityEngine;

namespace REPOJP.StageRoles;

internal static class PlayerState
{
    private static readonly FieldInfo? DisabledField =
        AccessTools.Field(typeof(PlayerAvatar), "isDisabled");
    private static readonly FieldInfo? DeadField =
        AccessTools.Field(typeof(PlayerAvatar), "deadSet");
    private static readonly FieldInfo? SpectatingField =
        AccessTools.Field(typeof(PlayerAvatar), "spectating");
    private static readonly FieldInfo? HealthField =
        AccessTools.Field(typeof(PlayerHealth), "health");
    private static readonly FieldInfo? MaxHealthField =
        AccessTools.Field(typeof(PlayerHealth), "maxHealth");
    private static readonly FieldInfo? DeathHeadField =
        AccessTools.Field(typeof(PlayerAvatar), "playerDeathHead");
    private static readonly FieldInfo? DeathHeadSetupField =
        AccessTools.Field(typeof(PlayerDeathHead), "setup");
    private static readonly FieldInfo? DeathHeadTriggeredField =
        AccessTools.Field(typeof(PlayerDeathHead), "triggered");
    private static readonly FieldInfo? DeathHeadTriggeredTimerField =
        AccessTools.Field(typeof(PlayerDeathHead), "triggeredTimer");
    private static readonly FieldInfo? DeathHeadPhysGrabObjectField =
        AccessTools.Field(typeof(PlayerDeathHead), "physGrabObject");
    private static readonly FieldInfo? DeathHeadSpectatedField =
        AccessTools.Field(typeof(PlayerDeathHead), "spectated");
    private static readonly FieldInfo? InTruckField =
        AccessTools.Field(typeof(RoomVolumeCheck), "inTruck");

    internal static bool IsLiving(PlayerAvatar? player)
    {
        if (player == null || !player.gameObject.activeInHierarchy ||
            ReadBool(DisabledField, player) || ReadBool(DeadField, player) ||
            ReadBool(SpectatingField, player))
        {
            return false;
        }

        return player.playerHealth == null ||
               HealthField?.GetValue(player.playerHealth) is not int health ||
               health > 0;
    }

    internal static bool IsInTruck(PlayerAvatar? player) =>
        player?.RoomVolumeCheck != null &&
        ReadBool(InTruckField, player.RoomVolumeCheck);

    internal static bool HasDeathHead(PlayerAvatar? player) =>
        player != null && DeathHeadField?.GetValue(player) is PlayerDeathHead deathHead &&
        deathHead != null;

    internal static bool TryRequestDeathHeadRevival(PlayerAvatar? player)
    {
        if (player == null ||
            DeathHeadField?.GetValue(player) is not PlayerDeathHead deathHead ||
            deathHead == null ||
            !ReadBool(DeathHeadSetupField, deathHead) ||
            !ReadBool(DeathHeadTriggeredField, deathHead) ||
            DeathHeadTriggeredTimerField?.GetValue(deathHead) is not float timer ||
            timer > 0f ||
            DeathHeadPhysGrabObjectField?.GetValue(deathHead) is not PhysGrabObject)
        {
            return false;
        }
        player.Revive(false);
        return true;
    }

    internal static bool TryGetDeathHeadPosition(
        PlayerAvatar? player,
        out Vector3 position)
    {
        position = default;
        if (player == null ||
            DeathHeadField?.GetValue(player) is not PlayerDeathHead deathHead ||
            deathHead == null)
        {
            return false;
        }

        position = deathHead.transform.position;
        return true;
    }

    internal static bool TryGetDeathHeadRuntime(
        PlayerAvatar? player,
        out PlayerDeathHead? deathHead,
        out PhysGrabObject? physGrabObject,
        out bool spectated)
    {
        deathHead = null;
        physGrabObject = null;
        spectated = false;
        if (player == null ||
            DeathHeadField?.GetValue(player) is not PlayerDeathHead resolvedHead ||
            resolvedHead == null ||
            DeathHeadPhysGrabObjectField?.GetValue(resolvedHead) is not PhysGrabObject resolvedPhys ||
            resolvedPhys == null)
        {
            return false;
        }

        deathHead = resolvedHead;
        physGrabObject = resolvedPhys;
        spectated = ReadBool(DeathHeadSpectatedField, resolvedHead);
        return true;
    }

    internal static bool TryMoveDeathHead(
        PlayerAvatar? player,
        Vector3 position,
        Quaternion rotation)
    {
        if (!TryGetDeathHeadRuntime(
                player,
                out PlayerDeathHead? deathHead,
                out PhysGrabObject? physGrabObject,
                out _) ||
            deathHead == null || physGrabObject == null)
        {
            return false;
        }

        physGrabObject.Teleport(position, rotation);
        if (physGrabObject.rb != null)
        {
            physGrabObject.rb.position = position;
            physGrabObject.rb.rotation = rotation;
            physGrabObject.rb.velocity = Vector3.zero;
            physGrabObject.rb.angularVelocity = Vector3.zero;
        }
        deathHead.transform.position = position;
        deathHead.transform.rotation = rotation;
        return true;
    }

    internal static bool TryGetCurrentHealth(PlayerAvatar? player, out int health)
    {
        health = 0;
        return player?.playerHealth != null &&
               HealthField?.GetValue(player.playerHealth) is int currentHealth &&
               (health = currentHealth) >= 0;
    }

    internal static bool TryGetMaximumHealth(PlayerAvatar? player, out int health)
    {
        health = 0;
        return player?.playerHealth != null &&
               MaxHealthField?.GetValue(player.playerHealth) is int maximumHealth &&
               (health = maximumHealth) > 0;
    }

    internal static bool SetHealthSynchronized(PlayerAvatar? player, int health)
    {
        PlayerHealth? playerHealth = player?.playerHealth;
        if (playerHealth == null)
        {
            return false;
        }

        int maxHealth = MaxHealthField?.GetValue(playerHealth) is int maximum && maximum > 0
            ? maximum
            : Math.Max(health, 1);
        int targetHealth = Math.Clamp(health, 1, Math.Max(1, maxHealth));
        if (SemiFunc.IsMultiplayer())
        {
            PhotonView? view = playerHealth.GetComponent<PhotonView>() ??
                               player?.GetComponent<PhotonView>();
            if (view == null)
            {
                return false;
            }
            view.RPC(
                nameof(PlayerHealth.UpdateHealthRPC),
                RpcTarget.All,
                targetHealth,
                maxHealth,
                false,
                false);
            return true;
        }

        playerHealth.UpdateHealthRPC(
            targetHealth,
            maxHealth,
            false,
            false);
        return true;
    }

    private static bool ReadBool(FieldInfo? field, object instance) =>
        field?.GetValue(instance) is bool value && value;

}
