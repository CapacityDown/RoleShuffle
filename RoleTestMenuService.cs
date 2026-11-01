using System;
using System.Collections.Generic;

namespace REPOJP.StageRoles;

internal readonly struct RoleTestPlayer(string id, string name, StageRole role, bool alive, int number)
{
    internal int Number { get; } = number;
    internal string Id { get; } = id;
    internal string Name { get; } = name;
    internal StageRole Role { get; } = role;
    internal bool Alive { get; } = alive;
}

internal sealed class RoleTestMenuService(StageRolesConfig config, StageRoleController controller)
{
    internal const string AllPlayers = "*";
    internal bool Visible => config.SetRoleCommandEnabled.Value && RoleSelectionSettings.CanEdit;
    internal bool Ready => Visible && controller.RoleAssignmentsReady;
    internal string UnavailableReason => !config.SetRoleCommandEnabled.Value ? "Testing is disabled."
        : !RoleSelectionSettings.CanEdit ? "Only the host can change player roles."
        : "Roles can only be changed during a stage.";

    internal IReadOnlyList<RoleTestPlayer> Players()
    {
        List<RoleTestPlayer> result = new();
        if (!Ready) return result;
        // Authoritative assignments, never the HUD's synthetic player preview.
        int number = 0;
        foreach (RoleAssignment assignment in controller.Assignments)
        {
            number++;
            if (assignment.Player != null && !string.IsNullOrWhiteSpace(assignment.SteamId))
                result.Add(new RoleTestPlayer(assignment.SteamId, PlayerIdentity.Name(assignment.Player),
                    assignment.AssignedRole, PlayerState.IsLiving(assignment.Player), number));
        }
        return result;
    }

    internal bool TryChange(string target, StageRole? role, string partner, out string message)
    {
        message = UnavailableReason;
        if (!Ready) return false; // Recheck privileges, testing flag and stage at click time.
        message = "Could not change roles.";
        if (role.HasValue && !Enum.IsDefined(typeof(StageRole), role.Value)) return false;
        IReadOnlyList<RoleTestPlayer> players = Players();
        RoleTestPlayer? Find(string id)
        {
            foreach (var player in players) if (string.Equals(player.Id, id, StringComparison.Ordinal)) return player;
            return null;
        }
        bool all = target == AllPlayers;
        RoleTestPlayer? selected = Find(target);
        if (players.Count == 0 || (!all && selected == null)) { message = "Player is no longer available."; return false; }
        if (role == StageRole.Twins)
        {
            RoleTestPlayer? second = Find(partner);
            if (all || second == null || selected!.Value.Id == partner || !selected.Value.Alive || !second.Value.Alive)
            { message = "Select two different living players."; return false; }
            foreach (var player in players)
                if (player.Role == StageRole.Twins && player.Id != target && player.Id != partner)
                { message = "A Twins pair already exists."; return false; }
        }
        else if (!all && selected!.Value.Role == StageRole.Twins && role != StageRole.Influenza)
        { message = "Use All players to replace an existing Twins pair."; return false; }

        // The existing command implementation owns cleanup, upgrades, HP,
        // Twins pairing, host checks and assignment publication.
        bool success;
        string response;
        if (role == StageRole.Twins) success = controller.TrySetTwins(target, partner, out response);
        else if (!role.HasValue) success = all ? controller.TryRandomizeAllRoles(out response) : controller.TryRandomizeRole(target, out response);
        else success = all ? controller.TrySetRoleForAll(role.Value, out response) : controller.TrySetRole(target, role.Value, out response);
        message = success ? "Roles updated." : "Could not change roles.";
        if (!success) StageRolesPlugin.ModLogger.LogWarning("Role menu: " + response);
        return success;
    }
}
