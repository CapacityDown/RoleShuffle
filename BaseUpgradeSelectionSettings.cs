using System;
using System.Collections.Generic;
using BepInEx.Configuration;

namespace REPOJP.StageRoles;

internal sealed class BaseUpgradeSelectionSettings(StageRolesConfig settings, ConfigFile file)
{
    internal static int CurrentRunLevel => (int)Math.Max(1L, Math.Min(999999L,
        RunManager.instance != null ? (long)RunManager.instance.levelsCompleted + 1 : 1));

    internal static bool CanAdjustInCurrentScene => RunManager.instance != null &&
        RunManager.instance.levelCurrent != null &&
        (SemiFunc.RunIsLobbyMenu() || SemiFunc.RunIsLobby() || SemiFunc.RunIsShop());

    internal bool CanAdjustLevel(string name, int delta, string saveIdentity) =>
        RoleSelectionSettings.CanEdit && TryBuildLevelAdjustment(name, delta, saveIdentity, out _, out _);

    internal bool TryAdjustLevel(string name, int delta, string saveIdentity)
    {
        if (!RoleSelectionSettings.CanEdit ||
            !TryBuildLevelAdjustment(name, delta, saveIdentity, out string dictionaryName, out int adjustment)) return false;
        return BaseUpgradeManualStore.TrySave(dictionaryName, adjustment, saveIdentity);
    }

    private bool TryBuildLevelAdjustment(string name, int delta, string saveIdentity,
        out string dictionaryName, out int adjustment)
    {
        dictionaryName = string.Empty;
        adjustment = 0;
        var entry = settings.BaseUpgradeLevelsEntry(name);
        if (!settings.BaseUpgradeManualAdjustmentEnabled.Value || !CanAdjustInCurrentScene || entry == null ||
            delta is not (-1 or 1) || string.IsNullOrEmpty(saveIdentity) ||
            saveIdentity != BaseUpgradeManualStore.SaveIdentity) return false;
        int maximum = name == "MapPlayerCount" ? 1 : RoleUpgradeScaling.MaximumUpgradeLevel;
        // Refuse malformed expressions instead of discarding user-authored rules.
        if (!RoleUpgradeScaling.TryParse(entry.Value, 1, 999999, maximum, true, out var rules, out _)) return false;
        int configured = 0;
        foreach (UpgradeScalingRule rule in rules)
        {
            if (rule.Condition <= CurrentRunLevel) configured = rule.Level;
        }
        dictionaryName = "playerUpgrade" + name;
        int next = BaseUpgradeManualStore.EffectiveLevel(dictionaryName, configured, maximum,
            settings.BaseUpgradeManualAdjustmentEnabled.Value) + delta;
        if (next < 0 || next > maximum) return false;
        // Keep configuration and truck results intact. If a changed rule has
        // clipped the total, one click still moves the displayed total by one.
        long value = (long)next - configured - BaseUpgradeBonusStore.Get(dictionaryName);
        if (value < int.MinValue || value > int.MaxValue) return false;
        adjustment = (int)value;
        return true;
    }

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
}
