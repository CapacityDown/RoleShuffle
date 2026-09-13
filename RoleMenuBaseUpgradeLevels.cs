using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using MenuLib.MonoBehaviors;
using TMPro;

namespace REPOJP.StageRoles;

internal sealed class RoleMenuAdjustment(Action? decrease, Action? increase)
{
    internal Action? Decrease { get; } = decrease;
    internal Action? Increase { get; } = increase;
}

internal sealed partial class RoleMenu
{
    private static string BaseUpgradePageSignature()
    {
        bool editable = RoleSelectionSettings.CanEdit;
        StringBuilder signature = new(editable ? "host:" : "guest:");
        signature.Append(BaseUpgradeSync.CurrentSignature(_config));
        if (editable)
        {
            signature.Append('|').Append(BaseUpgradeSelectionSettings.CurrentRunLevel);
            foreach (var definition in RoleUpgradeScaling.Definitions)
                signature.Append('|').Append(_config.BaseUpgradeLevelsEntry(definition.Name)!.Value);
        }
        return signature.ToString();
    }

    private static void RefreshBaseUpgradeRows(REPOPopupPage page)
    {
        IReadOnlyList<BaseUpgradeSnapshot> upgrades = BaseUpgradeSync.Read(_config);
        List<RoleMenuEntry> entries = new();
        void Text(string value)
        {
            foreach (string line in WrapGuideText(MeasurementText(page), value, ContentWidth(page), _guideLanguage))
                entries.Add(new RoleMenuEntry(line, GuideFontSize, FontStyles.Normal, GuideLineHeight, false, UseLanguageFont));
        }
        entries.Add(new RoleMenuEntry(Localized("BASE UPGRADE SETTINGS"), 20, FontStyles.Bold, 40, false, UseLanguageFont,
            () => { if (_openPage == page && _activeView == RoleMenuView.BaseUpgrades) SwitchView(page, RoleMenuView.BaseUpgradeSettings); }));
        if (upgrades.Count == 0) Text(Localized("Base Upgrade data is not available yet."));
        else
        {
            Text(Localized("Shared targets used when a role does not replace an upgrade."));
            Text(Localized("The +/- buttons change the configured base level from this run level until the next level rule. Truck draw bonuses are kept."));
            if (!RoleSelectionSettings.CanEdit) Text(Localized("Only the host can change Base Upgrade settings."));
            if (_utilityMessage.Length > 0) Text(_utilityMessage);
            int runLevel = BaseUpgradeSelectionSettings.CurrentRunLevel;
            foreach (BaseUpgradeSnapshot upgrade in upgrades)
            {
                Action? Adjust(int delta)
                {
                    if (!StageRolesPlugin.Instance.BaseUpgradeSettings.CanAdjustLevel(upgrade.Name, delta, runLevel)) return null;
                    return () =>
                    {
                        if (_openPage != page || _activeView != RoleMenuView.BaseUpgrades) return;
                        try { StageRolesPlugin.Instance.BaseUpgradeSettings.TryAdjustLevel(upgrade.Name, delta, runLevel); }
                        catch (Exception exception)
                        {
                            _utilityMessage = Localized("Operation failed. Check the RoleShuffle log.");
                            StageRolesPlugin.ModLogger.LogError(exception);
                        }
                        RefreshBaseUpgradeRows(page);
                    };
                }
                entries.Add(new RoleMenuEntry(
                    $"{DisplayUpgradeName(upgrade.Name)}: {upgrade.CurrentLevel.ToString(CultureInfo.InvariantCulture)}",
                    21, FontStyles.Bold, 30, false, UseLanguageFont));
                entries.Add(new RoleMenuEntry($"{Localized("Configured")}: {upgrade.ConfiguredLevel}",
                    18, FontStyles.Normal, 36, false, UseLanguageFont,
                    adjustment: new RoleMenuAdjustment(Adjust(-1), Adjust(1))));
                Text($"{Localized("Truck Draw")}: {SignedValue(upgrade.TruckDrawBonus)}");
                entries.Add(new RoleMenuEntry(string.Empty, 18, FontStyles.Normal, 5, false));
            }
        }
        ApplyEntries(page, entries);
        _openSignature = BaseUpgradePageSignature();
    }
}
