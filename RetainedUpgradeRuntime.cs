using System.Collections.Generic;

namespace REPOJP.StageRoles;

internal sealed partial class StageRoleController
{
    internal void RetainedUpgradeApplied(string steamId, IReadOnlyList<UpgradeGrant> changes)
    {
        if (!_assignmentsBySteamId.TryGetValue(steamId, out var assignment)) return;
        _eventRoles?.RetainedUpgradeApplied(steamId, assignment.Role, changes);
        List<UpgradeGrant> fixedTargets = new();
        foreach (UpgradeGrant target in RoleCatalog.TargetUpgrades(assignment.Role, _config, steamId))
            if (UpgradeItemRetention.IsFixed(assignment.Role, target.CommandName))
                foreach (UpgradeGrant change in changes)
                    if (change.DictionaryName == target.DictionaryName) fixedTargets.Add(target);
        if (fixedTargets.Count > 0) UpgradeService.SetLevels(steamId, fixedTargets);
    }
}
