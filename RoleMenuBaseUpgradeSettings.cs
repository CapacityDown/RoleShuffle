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
            foreach (string name in BaseUpgradePresets.UpgradeNames) signature.Append('|').Append(_config.TruckUpgradeDrawWeight(name));
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
        void Button(string label, Action action, bool enabled = true)
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
            entries.Add(new RoleMenuEntry(label, 20, FontStyles.Bold, 40, false, UseLanguageFont, click, isControl: true));
        }

        Text(Localized("Changes apply to future truck draws. Existing levels and configured base targets are kept."));
        if (!editable) Text(Localized("Only the host can change Base Upgrade settings."));
        if (_utilityMessage.Length > 0) Text(_utilityMessage);
        if (view == RoleMenuView.BaseUpgradePresets)
        {
            Button(Localized("BACK TO BASE UPGRADE SETTINGS"), () => SwitchView(page, RoleMenuView.BaseUpgradeSettings));
            Text(Localized("Presets replace all draw selection switches. Draw activation, weights, change amounts and level limits are kept."));
            foreach (var preset in BaseUpgradePresets.All)
            {
                Button(RoleText.Format("Apply: {0}", _guideLanguage, Localized(preset.Name)), () =>
                {
                    if (StageRolesPlugin.Instance.BaseUpgradeSettings.TryApplyPreset(preset.Id))
                        SwitchView(page, RoleMenuView.BaseUpgradeSettings);
                }, editable);
                Text(Localized(preset.Description));
                List<string> names = new();
                foreach (string name in BaseUpgradePresets.UpgradeNames)
                    if (preset.Includes(name)) names.Add(name == "AllUpgrades" ? Localized("All Upgrades") : DisplayUpgradeName(name));
                Text(string.Join(", ", names));
            }
        }
        else
        {
            Button(Localized("BACK TO BASE UPGRADES"), () => SwitchView(page, RoleMenuView.BaseUpgrades));
            Button(Localized("PRESETS"), () => SwitchView(page, RoleMenuView.BaseUpgradePresets));
            if (state == null) Text(Localized("Host Base Upgrade settings are not available yet."));
            else
            {
                Button(RoleText.Format("Truck draw: {0}", _guideLanguage, Localized(state.DrawEnabled ? "ON" : "OFF")),
                    () => StageRolesPlugin.Instance.BaseUpgradeSettings.TrySetDrawEnabled(!_config.TruckUpgradeDrawEnabled.Value), editable);
                Text(RoleText.Format("Selection: {0}", _guideLanguage, Localized(BaseUpgradePresets.MatchingName(state.IsEnabled))));
                int count = 0;
                bool hasCandidate = false;
                foreach (string name in BaseUpgradePresets.UpgradeNames)
                {
                    if (!state.IsEnabled(name)) continue;
                    count++;
                    if (name != "AllUpgrades" && (_config.TruckUpgradeDrawWeight(name) > 0 ||
                        (state.IsEnabled("AllUpgrades") && _config.TruckUpgradeDrawWeight("AllUpgrades") > 0))) hasCandidate = true;
                }
                Text(RoleText.Format("Enabled draw entries: {0}/{1}", _guideLanguage, count, BaseUpgradePresets.UpgradeNames.Count));
                Text(Localized("OFF excludes an upgrade from individual and All Upgrades draws. All Upgrades affects enabled types only."));
                Text(Localized("Weight 0 excludes direct selection only; enabled types can still receive All Upgrades."));
                if (!state.ModEnabled) Text(Localized("RoleShuffle is disabled in MOD settings."));
                if (!state.DrawEnabled) Text(Localized("Truck draws are disabled."));
                if (editable && !hasCandidate) Text(Localized("No enabled upgrade types can enter the draw with these weights."));
                foreach (string name in BaseUpgradePresets.UpgradeNames)
                {
                    string displayName = name == "AllUpgrades" ? Localized("All Upgrades") : DisplayUpgradeName(name);
                    string label = $"[{Localized(state.IsEnabled(name) ? "ON" : "OFF")}] {displayName}";
                    if (editable && _config.TruckUpgradeDrawWeight(name) == 0) label += " — " + Localized("Weight 0");
                    Button(label, () => StageRolesPlugin.Instance.BaseUpgradeSettings.TrySetEnabled(name, !_config.BaseUpgradeDrawIsEnabled(name)), editable);
                }
            }
        }
        ApplyEntries(page, entries);
        _openSignature = BaseUpgradeSettingsSignature();
    }
}
