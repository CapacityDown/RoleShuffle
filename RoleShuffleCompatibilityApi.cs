using System;
using UnityEngine;

namespace REPOJP.StageRoles;

/// <summary>
/// Optional host-side integration surface for other mods. Callers should resolve
/// this type lazily and continue without it when RoleShuffle is absent.
/// </summary>
public static class RoleShuffleCompatibilityApi
{
    public const int ApiVersion = 2;

    public static bool CanUseAsUnseenEnemyTarget(PlayerAvatar player)
    {
        try
        {
            if (player == null || !PlayerState.IsLiving(player))
            {
                return true;
            }

            StageRoleController? controller = StageRolesPlugin.Instance?.Controller;
            return controller?.PlayerHasRole(player, StageRole.Ninja) != true;
        }
        catch (Exception exception)
        {
            StageRolesPlugin.ModLogger?.LogDebug(
                $"Unseen-enemy target compatibility query failed open: {exception.Message}");
            return true;
        }
    }

    public static bool CanActivateValuableEffect(Component effect)
    {
        try
        {
            return effect == null ||
                   StageRolesPlugin.Instance?.Controller?
                       .ShouldSuppressEngineerEffect(effect) != true;
        }
        catch (Exception exception)
        {
            StageRolesPlugin.ModLogger?.LogDebug(
                $"Valuable-effect compatibility query failed open: {exception.Message}");
            return true;
        }
    }

    public static void RegisterNonPlayerEnemyDamage(EnemyHealth enemyHealth)
    {
        try
        {
            StageRolesPlugin.Instance?.Controller?
                .RegisterNonPlayerEnemyDamage(enemyHealth);
        }
        catch (Exception exception)
        {
            StageRolesPlugin.ModLogger?.LogDebug(
                $"Non-player enemy damage registration was skipped: {exception.Message}");
        }
    }

    public static void NotifyExternalRevival(PlayerAvatar player)
    {
        try
        {
            StageRolesPlugin.Instance?.Controller?.NotifyExternalRevival(player);
        }
        catch (Exception exception)
        {
            StageRolesPlugin.ModLogger?.LogDebug(
                $"External revival notification was skipped: {exception.Message}");
        }
    }

    public static bool HasImmediateCorrectiveRevival(PlayerAvatar player)
    {
        try
        {
            return player != null &&
                   StageRolesPlugin.Instance?.Controller?
                       .HasCorrectiveRevivalPending(player) == true;
        }
        catch (Exception exception)
        {
            StageRolesPlugin.ModLogger?.LogDebug(
                $"Corrective-revival compatibility query failed open: {exception.Message}");
            return false;
        }
    }

    public static bool IsRoleAssignmentReady()
    {
        try
        {
            return StageRolesPlugin.Instance?.Controller?.RoleAssignmentsReady == true;
        }
        catch
        {
            return false;
        }
    }

    public static string GetAssignedRoleName(PlayerAvatar player)
    {
        try
        {
            return StageRolesPlugin.Instance?.Controller?.AssignedRoleName(player) ??
                   string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    public static int GetActiveRoleCount(string roleName)
    {
        try
        {
            return string.IsNullOrWhiteSpace(roleName)
                ? 0
                : StageRolesPlugin.Instance?.Controller?
                    .ActiveRoleCount(roleName) ?? 0;
        }
        catch
        {
            return 0;
        }
    }

    public static bool IsTricksterDecoy(Component component)
    {
        try
        {
            return component != null &&
                   StageRolesPlugin.Instance?.Controller?
                       .IsTricksterDecoy(component) == true;
        }
        catch
        {
            return false;
        }
    }
}
