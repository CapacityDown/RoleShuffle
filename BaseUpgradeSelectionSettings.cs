using System.Collections.Generic;
using BepInEx.Configuration;

namespace REPOJP.StageRoles;

internal sealed class BaseUpgradeSelectionSettings(StageRolesConfig settings, ConfigFile file)
{
    internal bool TrySetEnabled(string name, bool enabled)
    {
        if (!RoleSelectionSettings.CanEdit || settings.BaseUpgradeDrawEnabledEntry(name) is not { } entry) return false;
        RoleSelectionSettings.SaveEntries(file, new Dictionary<ConfigEntry<bool>, bool> { [entry] = enabled });
        return true;
    }
    internal bool TrySetDrawEnabled(bool enabled)
    {
        if (!RoleSelectionSettings.CanEdit) return false;
        RoleSelectionSettings.SaveEntries(file, new Dictionary<ConfigEntry<bool>, bool> { [settings.TruckUpgradeDrawEnabled] = enabled });
        return true;
    }
    internal bool TryApplyPreset(BaseUpgradePreset id)
    {
        if (!RoleSelectionSettings.CanEdit || BaseUpgradePresets.Find(id) is not { } preset) return false;
        Dictionary<ConfigEntry<bool>, bool> entries = new();
        foreach (string name in BaseUpgradePresets.UpgradeNames)
            entries[settings.BaseUpgradeDrawEnabledEntry(name)!] = preset.Includes(name);
        RoleSelectionSettings.SaveEntries(file, entries);
        return true;
    }
}
