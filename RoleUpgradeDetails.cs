using System;
using System.Collections.Generic;
namespace REPOJP.StageRoles;
internal sealed partial class StageRoleController
{
    internal UpgradeBreakdownSnapshot? BuildUpgradeBreakdownForActor(int actor)
    {
        foreach (var player in CollectPlayers())
            if (PlayerIdentity.ActorNumber(player) == actor) return BuildUpgradeBreakdown(player);
        return null;
    }
    internal UpgradeBreakdownSnapshot? BuildUpgradeBreakdown(PlayerAvatar? player)
    {
        if (!SemiFunc.IsMasterClientOrSingleplayer() || player == null) return null;
        string id = PlayerIdentity.SteamId(player); // Used only to read host-local ledgers, never serialized.
        if (id.Length == 0 || !UpgradeService.TryGetLevels(id, out var levels)) return null;
        StageRole? role = _stageReady && _assignmentsBySteamId.TryGetValue(id, out var assignment) ? assignment.Role : null;
        var targets = role.HasValue ? RoleCatalog.TargetUpgrades(role.Value, _config, id) : RoleCatalog.BaseUpgrades(_config, id);
        var rows = new List<UpgradeBreakdownRow>();
        foreach (var basis in RoleCatalog.BaseUpgrades(_config))
        {
            string key = basis.DictionaryName;
            int actual = levels.GetValueOrDefault(key, 0), target = basis.Level;
            foreach (var value in targets) if (value.DictionaryName == key) { target = value.Level; break; }
            rows.Add(new UpgradeBreakdownRow(basis.CommandName, basis.Level, UpgradeItemRetention.Total(_config, id, key), target,
                role.HasValue ? _eventRoles?.AppliedUpgradeGrant(id, key) ?? 0 : 0,
                KingUpgradeAura.Bonus(id, key), Math.Max(0, PorterSpeed.WithoutPenalty(id, key, actual) - actual), actual));
        }
        rows.Add(new UpgradeBreakdownRow("Throw", 0, UpgradeItemRetention.Total(_config, id, "playerUpgradeThrow"), 0, 0, 0, 0,
            levels.GetValueOrDefault("playerUpgradeThrow", 0)));
        return new UpgradeBreakdownSnapshot(role, rows);
    }
}
