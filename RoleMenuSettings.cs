using System;
using System.Collections.Generic;
using System.Text;
using MenuLib.MonoBehaviors;
using TMPro;

namespace REPOJP.StageRoles;

internal sealed partial class RoleMenu
{
    private static string RoleSettingsSignature()
    {
        bool editable = RoleSelectionSettings.CanEdit;
        StringBuilder signature = new(editable ? "host:" : "guest:");
        signature.Append(RoleGuideSync.VisibilitySignature(_config));
        if (editable)
        {
            signature.Append('|').Append(_config.Enabled.Value);
            foreach (StageRole role in RoleCatalog.AllRoles)
                signature.Append('|').Append(_config.RoleIsEnabled(role)).Append(':').Append(_config.RoleWeightValue(role));
        }
        return signature.ToString();
    }

    private static void RefreshRoleSettingsRows(REPOPopupPage page)
    {
        bool editable = RoleSelectionSettings.CanEdit;
        bool available = editable || RoleGuideSync.VisibilitySignature(_config) != "*";
        RoleMenuView view = _activeView;
        List<RoleMenuEntry> entries = new();
        void Text(string value)
        {
            foreach (string line in WrapGuideText(MeasurementText(page), value, ContentWidth(page), _guideLanguage))
                entries.Add(new RoleMenuEntry(line, GuideFontSize, FontStyles.Normal, GuideLineHeight, false, UseLanguageFont));
        }
        void Button(string label, Action action, bool enabled = true, StageRole? emblem = null, bool emblemGrayedOut = false)
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
                if (_openPage == page && _activeView == view) RefreshRoleSettingsRows(page);
            } : null;
            entries.Add(new RoleMenuEntry(label, 20, FontStyles.Bold, emblem.HasValue ? 56 : 40,
                false, UseLanguageFont, click, emblem, emblem.HasValue && RoleCatalog.IsSecretRole(emblem.Value),
                isControl: true, emblemGrayedOut: emblemGrayedOut));
        }
        bool Enabled(StageRole role) => editable ? _config.RoleIsEnabled(role) : RoleGuideSync.IsVisible(role, _config);

        Text(Localized("Changes are saved for the next role assignment. Existing roles stay active."));
        if (!editable) Text(Localized("Only the host can change roles or apply presets."));
        if (_utilityMessage.Length > 0) Text(_utilityMessage);

        if (view == RoleMenuView.Presets)
        {
            Button(Localized("BACK TO ROLE SETTINGS"), () => SwitchView(page, RoleMenuView.Settings));
            Text(Localized("Presets replace all role ON/OFF settings. Weights, abilities and balance settings are kept."));
            foreach (RolePresetDefinition preset in RolePresets.All)
            {
                Button(RoleText.Format("Apply: {0}", _guideLanguage, Localized(preset.Name)), () =>
                {
                    if (StageRolesPlugin.Instance.RoleSettings.TryApplyPreset(preset.Id))
                        SwitchView(page, RoleMenuView.Settings);
                }, editable);
                Text(Localized(preset.Description));
                Text(RoleText.Format("Enabled roles: {0}/{1}", _guideLanguage, preset.Count, RoleCatalog.AllRoles.Count));
            }
        }
        else
        {
            Button(Localized("PRESETS"), () => SwitchView(page, RoleMenuView.Presets));
            if (!available) Text(Localized("Host role settings are not available yet."));
            else
            {
                int enabledCount = 0;
                bool hasCandidate = false;
                foreach (StageRole role in RoleCatalog.AllRoles)
                {
                    if (Enabled(role)) enabledCount++;
                    if (_config.RoleIsEnabled(role) && !RoleCatalog.IsSecretRole(role) && _config.RoleWeightValue(role) > 0)
                        hasCandidate = true;
                }
                Text(RoleText.Format("Enabled roles: {0}/{1}", _guideLanguage, enabledCount, RoleCatalog.AllRoles.Count));
                Text(RoleText.Format("Selection: {0}", _guideLanguage, Localized(RolePresets.MatchingName(Enabled))));
                if (editable && !_config.Enabled.Value) Text(Localized("Role assignment is disabled in MOD settings."));
                if (editable && !hasCandidate) Text(Localized("No regular roles with a positive weight are enabled. Random assignment has no candidates."));
                foreach (StageRole role in RoleCatalog.AllRoles)
                {
                    bool roleEnabled = Enabled(role);
                    string state = Localized(roleEnabled ? "ON" : "OFF");
                    string label = $"[{state}] {RoleCatalog.DisplayName(role)}";
                    if (editable && _config.RoleWeightValue(role) == 0) label += " — " + Localized("Weight 0");
                    Button(label, () => StageRolesPlugin.Instance.RoleSettings.TrySetEnabled(role, !_config.RoleIsEnabled(role)),
                        editable, role, emblemGrayedOut: !roleEnabled);
                }
            }
        }
        ApplyEntries(page, entries);
        _openSignature = RoleSettingsSignature();
    }
}
