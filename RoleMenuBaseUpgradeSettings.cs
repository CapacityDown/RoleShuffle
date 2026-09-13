using System;
using System.Collections.Generic;
using System.Text;
using MenuLib.MonoBehaviors;
using TMPro;

namespace REPOJP.StageRoles;

internal sealed partial class RoleMenu
{
    private static string BaseUpgradeSettingsSignature()
    {
        StringBuilder signature = new(RoleSelectionSettings.CanEdit ? "host:" : "guest:");
        signature.Append(BaseUpgradeSettingsSync.CurrentSignature(_config));
        if (RoleSelectionSettings.CanEdit)
            foreach (string name in BaseUpgradeDrawSelection.UpgradeNames) signature.Append('|').Append(_config.TruckUpgradeDrawWeight(name));
        return signature.ToString();
    }

    private static void RefreshBaseUpgradeSettingsRows(REPOPopupPage page)
    {
        bool editable = RoleSelectionSettings.CanEdit;
        BaseUpgradeSettingsSnapshot? state = BaseUpgradeSettingsSync.Read(_config);
        RoleMenuView view = _activeView;
        List<RoleMenuEntry> entries = new();
        void Text(string value)
        {
            foreach (string line in WrapGuideText(MeasurementText(page), value, ContentWidth(page), _guideLanguage))
                entries.Add(new RoleMenuEntry(line, GuideFontSize, FontStyles.Normal, GuideLineHeight, false, UseLanguageFont));
        }
        void Button(string label, Action action, bool enabled = true, bool off = false)
        {
            Action? click = enabled ? () =>
            {
                if (_openPage != page || _activeView != view) return;
                try { action(); }
                catch (Exception exception)
                {
                    _utilityMessage = Localized("Operation failed. Check the RoleShuffle log.");
                    StageRolesPlugin.ModLogger.LogError(exception);
                }
                if (_openPage == page && _activeView == view) RefreshBaseUpgradeSettingsRows(page);
            } : null;
            entries.Add(new RoleMenuEntry(label, 20, FontStyles.Bold, 40, false, UseLanguageFont, click, isControl: true, isOff: off));
        }

        Text(Localized("ON/OFF applies to future draws."));
        if (!editable) Text(Localized("Only the host can change Base Upgrade settings."));
        if (_utilityMessage.Length > 0) Text(_utilityMessage);
        Button(Localized("BACK TO BASE UPGRADES"), () => SwitchView(page, RoleMenuView.BaseUpgrades));
        if (state == null) Text(Localized("Host Base Upgrade settings are not available yet."));
        else
        {
            Button(RoleText.Format("Truck draw: {0}", _guideLanguage, Localized(state.DrawEnabled ? "ON" : "OFF")),
                () => StageRolesPlugin.Instance.BaseUpgradeSettings.TrySetDrawEnabled(!_config.TruckUpgradeDrawEnabled.Value), editable, off: !state.DrawEnabled);
            int count = 0;
            bool hasCandidate = false;
            foreach (string name in BaseUpgradeDrawSelection.UpgradeNames)
            {
                if (!state.IsEnabled(name)) continue;
                count++;
                if (name != "AllUpgrades" && (_config.TruckUpgradeDrawWeight(name) > 0 ||
                    (state.IsEnabled("AllUpgrades") && _config.TruckUpgradeDrawWeight("AllUpgrades") > 0))) hasCandidate = true;
            }
            Text(RoleText.Format("Enabled draw entries: {0}/{1}", _guideLanguage, count, BaseUpgradeDrawSelection.UpgradeNames.Count));
            Text(Localized("OFF excludes a type from all draws."));
            Text(Localized("Types marked 'No individual draw' can still be boosted by All Upgrades when ON."));
            if (!state.ModEnabled) Text(Localized("RoleShuffle is disabled in MOD settings."));
            if (!state.DrawEnabled) Text(Localized("Truck draws are disabled."));
            if (editable && !hasCandidate) Text(Localized("No enabled upgrade types can enter the draw with these weights."));
            foreach (string name in BaseUpgradeDrawSelection.UpgradeNames)
            {
                string displayName = name == "AllUpgrades" ? Localized("All Upgrades") : DisplayUpgradeName(name);
                bool upgradeEnabled = state.IsEnabled(name);
                string label = $"[{Localized(upgradeEnabled ? "ON" : "OFF")}] {displayName}";
                if (editable && _config.TruckUpgradeDrawWeight(name) == 0)
                    label += " — " + (name == "AllUpgrades" ? Localized("No draw") : Localized("No individual draw"));
                Button(label, () => StageRolesPlugin.Instance.BaseUpgradeSettings.TrySetEnabled(name, !_config.BaseUpgradeDrawIsEnabled(name)), editable, off: !upgradeEnabled);
            }
        }
        ApplyEntries(page, entries);
        _openSignature = BaseUpgradeSettingsSignature();
    }
}
