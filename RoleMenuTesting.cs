using System;
using System.Collections.Generic;
using System.Text;
using MenuLib.MonoBehaviors;
using TMPro;

namespace REPOJP.StageRoles;

internal sealed partial class RoleMenu
{
    private static string _testTarget = string.Empty;
    private static bool _testChoosePartner;
    private static RoleTestMenuService TestRoles => StageRolesPlugin.Instance.RoleTests;

    private static string RoleTestingSignature()
    {
        StringBuilder signature = new();
        signature.Append(TestRoles.Visible).Append('|').Append(TestRoles.Ready).Append('|').Append(TestRoles.UnavailableReason);
        foreach (var player in TestRoles.Players())
            signature.Append('|').Append(player.Id).Append(':').Append(player.Name).Append(':').Append(player.Role).Append(':').Append(player.Alive);
        return signature.ToString();
    }

    private static void RefreshRoleTestingRows(REPOPopupPage page)
    {
        var players = TestRoles.Players();
        RoleTestPlayer? selected = null;
        foreach (var player in players) if (player.Id == _testTarget) selected = player;
        if (!TestRoles.Ready || (_testTarget != RoleTestMenuService.AllPlayers && selected == null))
        { _testTarget = string.Empty; _testChoosePartner = false; }
        List<RoleMenuEntry> entries = new();
        void Text(string value)
        {
            foreach (string line in WrapGuideText(MeasurementText(page), value, ContentWidth(page), _guideLanguage))
                entries.Add(new RoleMenuEntry(line, GuideFontSize, FontStyles.Normal, GuideLineHeight, false, UseLanguageFont));
        }
        void Button(string label, Action action, bool enabled = true, StageRole? emblem = null)
        {
            string targetAtRender = _testTarget;
            bool partnerAtRender = _testChoosePartner;
            Action? click = enabled ? () =>
            {
                if (_openPage != page || _activeView != RoleMenuView.RoleTesting ||
                    _testTarget != targetAtRender || _testChoosePartner != partnerAtRender) return;
                try { action(); }
                catch (Exception exception)
                {
                    _utilityMessage = Localized("Operation failed. Check the RoleShuffle log.");
                    StageRolesPlugin.ModLogger.LogError(exception);
                }
                if (_openPage == page && _activeView == RoleMenuView.RoleTesting)
                {
                    RefreshRoleTestingRows(page);
                    if (_testTarget != targetAtRender || _testChoosePartner != partnerAtRender || _utilityMessage.Length > 0)
                        page.scrollView.SetScrollPosition(0f);
                }
            } : null;
            entries.Add(new RoleMenuEntry(label, 20, FontStyles.Bold, emblem.HasValue ? 56 : 40, false, UseLanguageFont,
                click, emblem, emblem.HasValue && RoleCatalog.IsSecretRole(emblem.Value), isControl: true));
        }
        void Change(StageRole? role, string partner = "")
        {
            bool success = TestRoles.TryChange(_testTarget, role, partner, out string message);
            _utilityMessage = Localized(message);
            if (success) _testChoosePartner = false;
        }
        Button(Localized("Back"), () =>
        {
            if (_testChoosePartner) _testChoosePartner = false;
            else if (_testTarget.Length > 0) _testTarget = string.Empty;
            else SwitchView(page, RoleMenuView.Tools);
            _utilityMessage = string.Empty;
        });
        if (_utilityMessage.Length > 0) Text(_utilityMessage);
        if (!TestRoles.Ready)
        {
            _testTarget = string.Empty;
            _testChoosePartner = false;
            Text(Localized(TestRoles.UnavailableReason));
        }
        else
        {
            if (_testTarget.Length == 0)
            {
                Text(Localized("Select a player."));
                if (players.Count > 0)
                    Button(Localized("All players"), () => { _testTarget = RoleTestMenuService.AllPlayers; _utilityMessage = string.Empty; });
                foreach (var player in players)
                    Button($"{player.Number}. {player.Name} — {RoleCatalog.DisplayName(player.Role)}", () =>
                    { _testTarget = player.Id; _utilityMessage = string.Empty; }, emblem: player.Role);
                if (players.Count == 0) Text(Localized("No roles are currently assigned."));
            }
            else
            {
                Text(RoleText.Format("Selection: {0}", _guideLanguage, selected.HasValue ? $"{selected.Value.Number}. {selected.Value.Name} — {RoleCatalog.DisplayName(selected.Value.Role)}" : Localized("All players")));
                if (_testChoosePartner)
                {
                    Text(Localized("Select a second player for Twins."));
                    foreach (var player in players)
                        if (player.Id != _testTarget)
                            Button($"{player.Number}. {player.Name}", () => Change(StageRole.Twins, player.Id), player.Alive && selected?.Alive == true);
                }
                else
                {
                    Button(Localized("Random role"), () => Change(null));
                    foreach (StageRole role in RoleCatalog.AllRoles)
                        Button(RoleText.Format("Apply: {0}", _guideLanguage, RoleCatalog.DisplayName(role)), () =>
                        {
                            if (role == StageRole.Twins) { _testChoosePartner = true; _utilityMessage = string.Empty; }
                            else Change(role);
                        }, role != StageRole.Twins || (selected?.Alive == true && players.Count >= 2), role);
                }
            }
        }
        ApplyEntries(page, entries);
        _openSignature = RoleTestingSignature();
    }
}
