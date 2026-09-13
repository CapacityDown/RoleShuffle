using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using MenuLib.MonoBehaviors;
using TMPro;

namespace REPOJP.StageRoles;

internal sealed class RoleMenuAdjustment(int currentLevel, Action? decrease, Action? increase, bool showButtons = true)
{
    internal bool ShowButtons { get; } = showButtons;
    internal int CurrentLevel { get; } = currentLevel;
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
        signature.Append('|').Append(BaseUpgradeSync.ManualAdjustmentEnabled(_config));
        if (editable)
        {
            signature.Append('|').Append(BaseUpgradeSelectionSettings.CurrentRunLevel);
            signature.Append('|').Append(BaseUpgradeSelectionSettings.CanAdjustInCurrentScene);
            signature.Append('|').Append(BaseUpgradeManualStore.SaveIdentity);
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
            bool manualDisabled = !BaseUpgradeSync.ManualAdjustmentEnabled(_config);
            if (!manualDisabled)
            {
                Text(Localized("+/- adjustments are saved with this game data."));
                if (!RoleSelectionSettings.CanEdit) Text(Localized("Only the host can change Base Upgrade settings."));
                else if (!BaseUpgradeSelectionSettings.CanAdjustInCurrentScene)
                    Text(Localized("+/- is available only in the lobby, truck, or shop."));
                else if (BaseUpgradeManualStore.SaveIdentity.Length == 0)
                    Text(Localized("Load a saved game to use +/- adjustments."));
                if (_utilityMessage.Length > 0) Text(_utilityMessage);
            }
            string saveIdentity = BaseUpgradeManualStore.SaveIdentity;
            foreach (BaseUpgradeSnapshot upgrade in upgrades)
            {
                Action? Adjust(int delta)
                {
                    if (!StageRolesPlugin.Instance.BaseUpgradeSettings.CanAdjustLevel(upgrade.Name, delta, saveIdentity)) return null;
                    return () =>
                    {
                        if (_openPage != page || _activeView != RoleMenuView.BaseUpgrades) return;
                        try { StageRolesPlugin.Instance.BaseUpgradeSettings.TryAdjustLevel(upgrade.Name, delta, saveIdentity); }
                        catch (Exception exception)
                        {
                            _utilityMessage = Localized("Operation failed. Check the RoleShuffle log.");
                            StageRolesPlugin.ModLogger.LogError(exception);
                        }
                        RefreshBaseUpgradeRows(page);
                    };
                }
                entries.Add(new RoleMenuEntry(
                    DisplayUpgradeName(upgrade.Name),
                    21, FontStyles.Bold, 30, false, UseLanguageFont,
                    adjustment: new RoleMenuAdjustment(upgrade.CurrentLevel, Adjust(-1), Adjust(1), showButtons: !manualDisabled)));
                entries.Add(new RoleMenuEntry(
                    manualDisabled
                        ? string.Format(CultureInfo.InvariantCulture, Localized("Config {0}  Draw {1}"),
                            upgrade.ConfiguredLevel, SignedValue(upgrade.TruckDrawBonus))
                        : string.Format(CultureInfo.InvariantCulture, Localized("Config {0}  Manual {1}  Draw {2}"),
                            upgrade.ConfiguredLevel, SignedValue(upgrade.ManualAdjustment), SignedValue(upgrade.TruckDrawBonus)),
                    18, FontStyles.Normal, GuideLineHeight, false, UseLanguageFont));
            }
        }
        ApplyEntries(page, entries);
        _openSignature = BaseUpgradePageSignature();
    }
}
