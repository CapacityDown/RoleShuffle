using System;
using System.Collections.Generic;
using MenuLib.MonoBehaviors;
using TMPro;

namespace REPOJP.StageRoles;
internal sealed partial class RoleMenu
{
    private static string _roleQuery = string.Empty;
    private static RoleCategory _roleCategory;
    private static RoleListScope _roleScope;
    private static string RoleFilterSignature => _roleQuery + ":" + _roleCategory + ":" + _roleScope +
        (_roleScope == RoleListScope.Eligible ? RoleDrawEligibilitySync.Signature : string.Empty);
    private static bool MatchesRoleFilter(StageRole role)
    {
        var snapshot = RoleDrawEligibilitySync.Read();
        string id = PlayerIdentity.SteamId(SemiFunc.PlayerGetLocal());
        DrawStatus status = snapshot != null && snapshot.Players.TryGetValue(id, out var roles) && roles.TryGetValue(role, out var reason)
            ? RoleDrawEligibility.Status(reason) : DrawStatus.Unknown;
        return RoleListFilter.Matches(role, _roleQuery, _roleCategory, _roleScope,
            RoleGuideSync.IsVisible(role, _config), status, RoleCatalog.IsSecretRole(role) && IsRoleAssigned(role));
    }
    private static void AddRoleFilters(REPOPopupPage page, List<RoleMenuEntry> entries)
    {
        var view = _activeView;
        void Button(string label, Action action) => entries.Add(new RoleMenuEntry(label, 18, FontStyles.Normal, 36,
            false, UseLanguageFont, () =>
            {
                if (_openPage != page || _activeView != view) return;
                action(); page.scrollView.SetScrollPosition(0f); RefreshCurrentRoleList(page);
            }, isControl: true));
        Button(RoleText.Format("Search: {0}", _guideLanguage, _roleQuery.Length == 0 ? Localized("All") : _roleQuery), () =>
            OpenRoleTextEditor(page, Localized("Search roles"), _roleQuery, 40,
                value => { _roleQuery = CustomRolePresets.NormalizeNote(value, maximum: 40); return true; }));
        Button(RoleText.Format("Category: {0}", _guideLanguage, Localized(_roleCategory.ToString())), () =>
            _roleCategory = (RoleCategory)(((int)_roleCategory + 1) % Enum.GetValues(typeof(RoleCategory)).Length));
        string scope = _roleScope == RoleListScope.Enabled ? "Enabled roles" : _roleScope == RoleListScope.Eligible ? "Eligible or conditional" : "All";
        Button(RoleText.Format("Show: {0}", _guideLanguage, Localized(scope)), () =>
            _roleScope = (RoleListScope)(((int)_roleScope + 1) % 3));
        if (_roleScope == RoleListScope.Eligible)
            entries.Add(new RoleMenuEntry(Localized("Conditional roles are included; unknown roles are hidden."),
                16, FontStyles.Normal, 52, true, UseLanguageFont));
        if (_roleQuery.Length > 0 || _roleCategory != RoleCategory.All || _roleScope != RoleListScope.All)
            Button(Localized("Clear filters"), () => { _roleQuery = ""; _roleCategory = RoleCategory.All; _roleScope = RoleListScope.All; });
    }
    private static void AddTeamCompositionControls(REPOPopupPage page, List<RoleMenuEntry> entries)
    {
        void Save(Action action)
        {
            try { action(); }
            catch (Exception error)
            { _utilityMessage = Localized("Operation failed. Check the RoleShuffle log."); StageRolesPlugin.ModLogger.LogError(error); }
            RefreshRoleSettingsRows(page);
        }
        bool editable = RoleSelectionSettings.CanEdit;
        if (!editable) return;
        void Text(string value) { foreach (var line in WrapGuideText(MeasurementText(page), value, ContentWidth(page), _guideLanguage))
            entries.Add(new RoleMenuEntry(line, 18, FontStyles.Normal, GuideLineHeight, false, UseLanguageFont)); }
        entries.Add(new RoleMenuEntry(RoleText.Format("Team composition: {0}", _guideLanguage,
            Localized(_config.TeamCompositionEnabled.Value ? "ON" : "OFF")), 20, FontStyles.Bold, 40, false, UseLanguageFont,
            () => { if (_openPage != page || _activeView != RoleMenuView.Settings) return;
                Save(() => StageRolesPlugin.Instance.RoleSettings.TrySetTeamComposition(!_config.TeamCompositionEnabled.Value)); }, isControl: true));
        if (!_config.TeamCompositionEnabled.Value) return;
        foreach (bool support in new[] { false, true })
        {
            int value = support ? _config.TeamSupportMinimum.Value : _config.TeamDangerMaximum.Value;
            Action? Adjust(int delta) => value + delta < (support ? 0 : 1) || value + delta > 30 ? null : () =>
            {
                if (_openPage != page || _activeView != RoleMenuView.Settings) return;
                Save(() => StageRolesPlugin.Instance.RoleSettings.TryAdjustTeamTarget(support, delta));
            };
            entries.Add(new RoleMenuEntry(Localized(support ? "Minimum support roles" : "Maximum danger roles"),
                18, FontStyles.Normal, 36, false, UseLanguageFont,
                adjustment: new RoleMenuAdjustment(value, Adjust(-1), Adjust(1))));
        }
        Text(Localized("Applies to future draws. Support is prioritized; team limits relax only when candidates run out. Disabled roles and player/item requirements stay excluded."));
    }
}
