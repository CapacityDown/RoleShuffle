using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using HarmonyLib;
using MenuLib;
using MenuLib.MonoBehaviors;
using MenuLib.Structs;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace REPOJP.StageRoles;

internal sealed partial class RoleMenu : MonoBehaviour
{
    private const float RefreshIntervalSeconds = 0.25f;
    private const float MinimumContentWidth = 260f;
    private const float MaximumContentWidth = 520f;
    private const float AssignmentRowHeight = 32f;
    private const float GuideFontSize = 18f;
    private const float GuideTitleHeight = 30f;
    private const float GuideLineHeight = 25f;
    private const float GuideRoleSpacing = 10f;
    private const float RowPadding = 3f;
    private const float RowSpacing = 1.5f;
    private const float LanguageWrapWidthMultiplier = 1f;
    private const int RoleUiBuildNumber = 435;
    internal static int UiBuildNumber => RoleUiBuildNumber;

    private static bool _registered;
    private static readonly List<RoleMenuRow> _roleRows = new();
    private static StageRolesConfig _config = null!;
    private static REPOPopupPage? _openPage;
    private static MenuPage? _issuesDialog;
    private static int _issuesDialogDismissedFrame = -1;
    private static REPOButton? _assignmentsButton;
    private static REPOButton? _guideButton;
    private static REPOButton? _settingsButton;
    private static REPOButton? _baseUpgradesButton;
    private static REPOButton? _historyButton;
    private static REPOButton? _toolsButton;
    private static string _utilityMessage = string.Empty;
    private static REPOButton? _languageButton;
    private static REPOButton? _backButton;
    private static REPOLabel? _versionLabel;
    private static string _openSignature = string.Empty;
    private static string? _guideSignaturePayload;
    private static string? _guideVisibilitySignature;
    private static int _guideSignatureRevealed;
    private static string _guideSignature = string.Empty;
    private static IReadOnlyList<RoleSnapshot>? _signatureAssignments;
    private static string _assignmentsSignature = string.Empty;
    private static bool _signatureTestingEnabled;
    private static string _signatureGuidePayload = string.Empty;
    private static RoleGuideLanguage _signatureGuideLanguage;
    private static string _selectedAssignmentKey = string.Empty;
    private static RoleMenuView _activeView = RoleMenuView.Assignments;
    private static RoleGuideLanguage _guideLanguage = RoleGuideLanguage.English;
    private static bool _assignmentsEnabled = true;

    private float _nextRefreshAt;

    internal static bool IsOpen { get; private set; }

    internal void Initialize(StageRolesConfig config)
    {
        _config = config;
        if (_registered)
        {
            return;
        }
        _registered = true;
        MenuAPI.AddElementToEscapeMenu(parent =>
        {
            CreateCornerRoleButton(parent, OpenRolePage);
        });
        MenuAPI.AddElementToLobbyMenu(parent =>
        {
            CreateCornerRoleButton(parent, OpenRoleGuidePage);
        });
    }

    private static void CreateCornerRoleButton(
        Transform parent,
        Action onClick)
    {
        REPOButton button = MenuAPI.CreateREPOButton(
            "ROLES",
            onClick,
            parent,
            Vector2.zero);
        RectTransform rect = button.rectTransform;
        Vector2 buttonSize = button.labelTMP.GetPreferredValues();
        button.overrideButtonSize = buttonSize;
        rect.SetInsetAndSizeFromParentEdge(
            RectTransform.Edge.Right,
            36f,
            buttonSize.x);
        rect.SetInsetAndSizeFromParentEdge(
            RectTransform.Edge.Top,
            36f,
            buttonSize.y);
    }

    private void Update()
    {
        if (!IsOpen)
        {
            return;
        }
        if (_openPage == null)
        {
            ClearOpenPageTracking();
            return;
        }
        if (Time.unscaledTime < _nextRefreshAt)
        {
            return;
        }

        _nextRefreshAt = Time.unscaledTime + RefreshIntervalSeconds;
        if (RoleHudEditor.IsOpen || _issuesDialog != null) return;
        RoleGuideLanguage savedLanguage = SavedLanguage;
        if (_guideLanguage != savedLanguage)
        { _guideLanguage = savedLanguage; _utilityMessage = string.Empty; SwitchView(_openPage, _activeView); return; }
        if (_activeView is RoleMenuView.Settings or RoleMenuView.Presets)
        {
            if (_openSignature != RoleSettingsSignature()) RefreshRoleSettingsRows(_openPage);
            return;
        }
        if (_activeView is RoleMenuView.BaseUpgradeSettings or RoleMenuView.BaseUpgradePresets)
        {
            if (_openSignature != BaseUpgradeSettingsSignature()) RefreshBaseUpgradeSettingsRows(_openPage);
            return;
        }
        if (_activeView is RoleMenuView.History or RoleMenuView.Tools or RoleMenuView.Report)
        {
            string utilitySignature = UtilitySignature();
            if (_openSignature != utilitySignature) RefreshUtilityRows(_openPage);
            return;
        }
        if (_activeView == RoleMenuView.Guide)
        {
            string guideSignature = BuildGuideSignature();
            if (!string.Equals(
                    guideSignature,
                    _openSignature,
                    StringComparison.Ordinal))
            {
                RefreshGuideRows(_openPage);
            }
            return;
        }
        if (_activeView == RoleMenuView.BaseUpgrades)
        {
            string baseUpgradeSignature =
                BaseUpgradePageSignature();
            if (!string.Equals(
                    baseUpgradeSignature,
                    _openSignature,
                    StringComparison.Ordinal))
            {
                RefreshBaseUpgradeRows(_openPage);
            }
            return;
        }

        IReadOnlyList<RoleSnapshot> assignments = RoleAssignmentSync.Read();
        string signature = BuildSignature(assignments);
        if (!string.Equals(signature, _openSignature, StringComparison.Ordinal))
        {
            RefreshAssignmentRows(_openPage, assignments, signature);
        }
    }

    private static void OpenRolePage()
    {
        OpenRolePage(assignmentsEnabled: true);
    }

    private static void OpenRoleGuidePage()
    {
        OpenRolePage(assignmentsEnabled: false);
    }

    private static void OpenRolePage(bool assignmentsEnabled)
    {
        REPOPopupPage page = MenuAPI.CreateREPOPopupPage(
            "Roles",
            REPOPopupPage.PresetSide.Right,
            shouldCachePage: false,
            pageDimmerVisibility: true,
            spacing: RowSpacing);
        Padding padding = page.maskPadding;
        padding.top = 24f;
        padding.bottom = 0f;
        page.maskPadding = padding;

        _openPage = page;
        _roleRows.Clear();
        _assignmentsEnabled = assignmentsEnabled;
        _activeView = assignmentsEnabled
            ? RoleMenuView.Assignments
            : RoleMenuView.Guide;
        _guideLanguage = SavedLanguage;
        _utilityMessage = string.Empty;
        _openSignature = string.Empty;
        _selectedAssignmentKey = string.Empty;

        page.AddElement(parent =>
        {
            _assignmentsButton = CreateNavigationButton(
                "CURRENT ROLES",
                () => SwitchView(page, RoleMenuView.Assignments),
                parent,
                new Vector2(108f, 272f));
            _guideButton = CreateNavigationButton(
                "ROLE GUIDE",
                () => SwitchView(page, RoleMenuView.Guide),
                parent,
                new Vector2(108f, 240f));
            _settingsButton = CreateNavigationButton("ROLE SETTINGS", () => SwitchView(page, RoleMenuView.Settings), parent, new Vector2(108f, 208f));
            _baseUpgradesButton = CreateNavigationButton(
                "BASE UPGRADES",
                () => SwitchView(page, RoleMenuView.BaseUpgrades),
                parent,
                new Vector2(108f, 176f));
            _baseUpgradesButton.labelTMP.fontSize = 20f;
            _historyButton = CreateNavigationButton("DRAW HISTORY", () => SwitchView(page, RoleMenuView.History), parent, new Vector2(108f, 144f));
            _toolsButton = CreateNavigationButton("TOOLS", () => SwitchView(page, RoleMenuView.Tools), parent, new Vector2(108f, 112f));
            _languageButton = CreateNavigationButton(
                "LANGUAGE: ENGLISH",
                () => ToggleLanguage(page),
                parent,
                new Vector2(108f, 80f));
            _languageButton.labelTMP.fontSize = 18f;
            _versionLabel = MenuAPI.CreateREPOLabel(
                $"RoleShuffle v{StageRolesPlugin.PluginVersion}" +
                $"(build-{RoleUiBuildNumber})",
                parent,
                new Vector2(108f, 54f));
            _versionLabel.rectTransform.sizeDelta = new Vector2(190f, 24f);
            _versionLabel.labelTMP.rectTransform.sizeDelta =
                new Vector2(190f, 24f);
            _versionLabel.labelTMP.alignment = TextAlignmentOptions.Left;
            _versionLabel.labelTMP.fontSize = 16f;
            _versionLabel.labelTMP.color =
                new Color(0.72f, 0.72f, 0.72f, 0.9f);
            if (!assignmentsEnabled)
            {
                DisableNavigationButton(_assignmentsButton);
            }
            UpdateNavigationLabels();
        });

        page.AddElement(parent =>
        {
            _backButton = MenuAPI.CreateREPOButton(
                "Back",
                () =>
                {
                    ClearOpenPageTracking();
                    page.ClosePage(closePagesAddedOnTop: true);
                },
                parent,
                new Vector2(66f, 18f));
        });
        page.onEscapePressed += () =>
        {
            // MenuLib also sends Escape to inactive pages. Consume it in the
            // confirmation dialog without closing the Roles page underneath.
            if (_issuesDialog != null || _issuesDialogDismissedFrame == Time.frameCount) return false;
            if (RoleHudEditor.IsOpen) { RoleHudEditor.Instance!.Close(false); return false; }
            ClearOpenPageTracking();
            return true;
        };
        IsOpen = true;
        SwitchView(page, _activeView);
        page.OpenPage(openOnTop: false);
    }

    private static void DisableNavigationButton(REPOButton button)
    {
        button.onClick = null;
        Button? unityButton = button.GetComponent<Button>();
        if (unityButton != null)
        {
            unityButton.interactable = false;
        }
        button.menuButton.enabled = false;
        button.labelTMP.color = new Color(0.35f, 0.35f, 0.35f, 0.75f);
    }

    private static REPOButton CreateNavigationButton(
        string text,
        Action onClick,
        Transform parent,
        Vector2 position)
    {
        REPOButton button = MenuAPI.CreateREPOButton(
            text,
            onClick,
            parent,
            position);
        button.overrideButtonSize = new Vector2(190f, 32f);
        button.labelTMP.alignment = TextAlignmentOptions.Left;
        button.labelTMP.fontSize = 20f;
        return button;
    }

    private static void SwitchView(REPOPopupPage page, RoleMenuView view)
    {
        if (!ReferenceEquals(_openPage, page))
        {
            return;
        }
        if (view == RoleMenuView.Assignments && !_assignmentsEnabled)
        {
            return;
        }

        _activeView = view;
        _utilityMessage = string.Empty;
        UpdateNavigationLabels();
        page.scrollView.SetScrollPosition(0f);
        if (view is RoleMenuView.Settings or RoleMenuView.Presets)
        { RefreshRoleSettingsRows(page); return; }
        if (view is RoleMenuView.BaseUpgradeSettings or RoleMenuView.BaseUpgradePresets)
        { RefreshBaseUpgradeSettingsRows(page); return; }
        if (view is RoleMenuView.History or RoleMenuView.Tools or RoleMenuView.Report)
        { RefreshUtilityRows(page); return; }
        if (view == RoleMenuView.Assignments)
        {
            IReadOnlyList<RoleSnapshot> assignments = RoleAssignmentSync.Read();
            RefreshAssignmentRows(
                page,
                assignments,
                BuildSignature(assignments));
            return;
        }
        if (view == RoleMenuView.Guide)
        {
            RefreshGuideRows(page);
            return;
        }
        RefreshBaseUpgradeRows(page);
    }

    private static void ToggleLanguage(REPOPopupPage page)
    {
        if (!ReferenceEquals(_openPage, page))
        {
            return;
        }

        _guideLanguage = RoleLanguage.Next(_guideLanguage);
        _utilityMessage = string.Empty;
        _config.GuideLanguage.Value = RoleLanguage.NativeName(_guideLanguage);
        StageRolesPlugin.Instance.SaveLocalSettings();
        SwitchView(page, _activeView);
    }

    private static RoleGuideLanguage SavedLanguage => RoleLanguage.Parse(_config.GuideLanguage.Value);
    private static bool UseLanguageFont => _guideLanguage != RoleGuideLanguage.English;
    private static string Localized(string english, string? japanese = null) => RoleText.Get(english, _guideLanguage, japanese);

    private static void UpdateNavigationLabels()
    {
        void Button(REPOButton? button, string key, bool active)
        {
            if (button == null) return;
            button.labelTMP.text = (active ? "> " : "  ") + Localized(key);
            button.labelTMP.font = RoleGuideFont.ForLanguage(button.labelTMP.font, _guideLanguage);
            button.labelTMP.enableAutoSizing = true;
            button.labelTMP.fontSizeMin = 12;
            button.labelTMP.fontSizeMax = button == _languageButton ? 18 : 20;
        }
        Button(_assignmentsButton, "CURRENT ROLES", _activeView == RoleMenuView.Assignments);
        Button(_guideButton, "ROLE GUIDE", _activeView == RoleMenuView.Guide);
        Button(_settingsButton, "ROLE SETTINGS", _activeView is RoleMenuView.Settings or RoleMenuView.Presets);
        Button(_baseUpgradesButton, "BASE UPGRADES", _activeView is RoleMenuView.BaseUpgrades or RoleMenuView.BaseUpgradeSettings or RoleMenuView.BaseUpgradePresets);
        Button(_historyButton, "DRAW HISTORY", _activeView == RoleMenuView.History);
        Button(_toolsButton, "TOOLS", _activeView is RoleMenuView.Tools or RoleMenuView.Report);
        Button(_backButton, "Back", false);
        Button(_languageButton, "", false);
        if (_languageButton != null)
        {
            _languageButton.labelTMP.text = "LANG: " + RoleLanguage.NativeName(_guideLanguage);
            _languageButton.rectTransform.gameObject.SetActive(true);
        }
        if (_openPage != null)
        {
            _openPage.headerTMP.text = Localized(_activeView switch
            {
                RoleMenuView.Assignments => "Current Roles",
                RoleMenuView.Guide => "Role Guide",
                RoleMenuView.Settings => "Role Settings",
                RoleMenuView.Presets => "Role Presets",
                RoleMenuView.BaseUpgradeSettings => "Base Upgrade Settings",
                RoleMenuView.BaseUpgradePresets => "Base Upgrade Presets",
                RoleMenuView.History => "Draw History",
                RoleMenuView.Tools => "Tools",
                RoleMenuView.Report => "Bug Report",
                _ => "Base Upgrades"
            });
            _openPage.headerTMP.font = RoleGuideFont.ForLanguage(_openPage.headerTMP.font, _guideLanguage);
            _openPage.headerTMP.enableAutoSizing = true;
            _openPage.headerTMP.fontSizeMin = 18;
            _openPage.headerTMP.fontSizeMax = 32;
        }
    }

    private static void RefreshAssignmentRows(
        REPOPopupPage page,
        IReadOnlyList<RoleSnapshot> assignments,
        string signature)
    {
        List<RoleMenuEntry> entries = new();
        string localSteamId = PlayerIdentity.SteamId(SemiFunc.PlayerGetLocal());
        if (assignments.Count == 0)
        {
            entries.Add(new RoleMenuEntry(
                Localized("No roles are currently assigned."),
                24f,
                FontStyles.Bold,
                AssignmentRowHeight,
                wrap: false));
        }
        else
        {
            bool selectedAssignmentFound = false;
            int fallbackIndex = 1;
            foreach (RoleSnapshot snapshot in assignments)
            {
                string playerName = string.IsNullOrWhiteSpace(snapshot.PlayerName)
                    ? $"{Localized("Player")} {fallbackIndex}"
                    : snapshot.PlayerName.Trim();
                bool isLocal = !string.IsNullOrEmpty(localSteamId) &&
                               string.Equals(
                                   snapshot.SteamId,
                                   localSteamId,
                                   StringComparison.Ordinal);
                string assignmentKey = AssignmentKey(snapshot, fallbackIndex);
                bool isSelected = string.Equals(
                    assignmentKey,
                    _selectedAssignmentKey,
                    StringComparison.Ordinal);
                selectedAssignmentFound |= isSelected;
                entries.Add(new RoleMenuEntry(
                    (isSelected ? "> " : "  ") +
                    (_config.SetRoleCommandEnabled.Value && snapshot.PlayerNumber > 0
                        ? $"[{snapshot.PlayerNumber}] " : string.Empty) +
                    (isLocal
                        ? $"{Localized("YOU")} - {RoleCatalog.AssignmentName(snapshot.Role, snapshot.EffectiveRole)}"
                        : $"{playerName} - {RoleCatalog.AssignmentName(snapshot.Role, snapshot.EffectiveRole)}"),
                    isLocal ? 24f : 21f,
                    isLocal ? FontStyles.Bold : FontStyles.Normal,
                    48f,
                    wrap: false,
                    onClick: () => ToggleAssignmentDescription(
                        page,
                        assignmentKey),
                    emblemRole: snapshot.Role == StageRole.Imitator
                        ? snapshot.EffectiveRole : snapshot.Role));

                if (isSelected)
                {
                    IReadOnlyDictionary<StageRole, string> descriptions =
                        RoleGuideSync.Read(_config, _guideLanguage);
                    string description = descriptions.TryGetValue(
                        snapshot.Role,
                        out string value)
                            ? value
                            : RoleGuideCatalog.GenericDescription(
                                snapshot.Role,
                                _guideLanguage);
                    if (RoleCatalog.IsSecretRole(snapshot.Role))
                    {
                        description = RoleGuideCatalog
                            .RevealedSecretDescription(snapshot.Role, _guideLanguage);
                    }
                    TMP_Text measurementText = MeasurementText(page);
                    foreach (string line in WrapGuideText(
                                 measurementText,
                                 description,
                                 ContentWidth(page),
                                 _guideLanguage))
                    {
                        entries.Add(new RoleMenuEntry(
                            line,
                            GuideFontSize,
                            FontStyles.Normal,
                            GuideLineHeight,
                            wrap: false,
                            useLanguageFont:
                                UseLanguageFont));
                    }
                    entries.Add(new RoleMenuEntry(
                        string.Empty,
                        GuideFontSize,
                        FontStyles.Normal,
                        GuideRoleSpacing,
                        wrap: false));
                }
                fallbackIndex++;
            }
            if (!selectedAssignmentFound)
            {
                _selectedAssignmentKey = string.Empty;
            }
        }
        ApplyEntries(page, entries);
        UpdateNavigationLabels();
        _openSignature = signature;
    }

    private static void ToggleAssignmentDescription(
        REPOPopupPage page,
        string assignmentKey)
    {
        if (!ReferenceEquals(_openPage, page) ||
            _activeView != RoleMenuView.Assignments)
        {
            return;
        }

        _selectedAssignmentKey = string.Equals(
            _selectedAssignmentKey,
            assignmentKey,
            StringComparison.Ordinal)
                ? string.Empty
                : assignmentKey;
        IReadOnlyList<RoleSnapshot> assignments = RoleAssignmentSync.Read();
        RefreshAssignmentRows(
            page,
            assignments,
            BuildSignature(assignments));
    }

    private static string AssignmentKey(
        RoleSnapshot snapshot,
        int fallbackIndex) =>
        !string.IsNullOrEmpty(snapshot.SteamId)
            ? $"steam:{snapshot.SteamId}"
            : $"fallback:{fallbackIndex}:{snapshot.PlayerName}:{(int)snapshot.Role}";

    private static void RefreshGuideRows(REPOPopupPage page)
    {
        float contentWidth = ContentWidth(page);
        IReadOnlyDictionary<StageRole, string> descriptions =
            RoleGuideSync.Read(_config, _guideLanguage);
        TMP_Text measurementText = MeasurementText(page);
        List<StageRole> visibleRoles = new();
        foreach (StageRole role in RoleCatalog.AllRoles)
        {
            if (RoleGuideSync.IsVisible(role, _config)) visibleRoles.Add(role);
        }

        List<RoleMenuEntry> entries = new(visibleRoles.Count * 4);
        for (int roleIndex = 0; roleIndex < visibleRoles.Count; roleIndex++)
        {
            StageRole role = visibleRoles[roleIndex];
            string description = descriptions.TryGetValue(role, out string value)
                ? value
                : RoleGuideCatalog.GenericDescription(role, _guideLanguage);
            bool secretAssigned = RoleCatalog.IsSecretRole(role) && IsRoleAssigned(role);
            if (RoleCatalog.IsSecretRole(role))
            {
                description = secretAssigned
                    ? RoleGuideCatalog.RevealedSecretDescription(role, _guideLanguage)
                    : "???";
            }
            entries.Add(new RoleMenuEntry(
                secretAssigned
                    ? RoleCatalog.AssignmentName(role)
                    : RoleCatalog.DisplayName(role),
                20f,
                FontStyles.Bold,
                64f,
                wrap: false,
                emblemRole: role,
                unrevealedEmblem: RoleCatalog.IsSecretRole(role) && !secretAssigned));
            foreach (string line in WrapGuideText(
                         measurementText,
                         description,
                         contentWidth,
                         _guideLanguage))
            {
                entries.Add(new RoleMenuEntry(
                    line,
                    GuideFontSize,
                    FontStyles.Normal,
                    GuideLineHeight,
                    wrap: false,
                    useLanguageFont:
                        UseLanguageFont));
            }
            if (roleIndex + 1 < visibleRoles.Count)
            {
                entries.Add(new RoleMenuEntry(
                    string.Empty,
                    GuideFontSize,
                    FontStyles.Normal,
                    GuideRoleSpacing,
                    wrap: false));
            }
        }
        ApplyEntries(page, entries);
        _openSignature = BuildGuideSignature();
    }

    private static string BuildGuideSignature()
    {
        string payload = RoleGuideSync.CurrentSignature(_config);
        string visibility = RoleGuideSync.VisibilitySignature(_config);
        int revealed = (IsRoleAssigned(StageRole.Superbot) ? 1 : 0) |
                       (IsRoleAssigned(StageRole.Disaster) ? 2 : 0);
        if (_guideSignaturePayload != payload || _guideSignatureRevealed != revealed ||
            _guideVisibilitySignature != visibility)
        {
            _guideSignaturePayload = payload;
            _guideVisibilitySignature = visibility;
            _guideSignatureRevealed = revealed;
            _guideSignature = payload + "|enabled:" + visibility +
                "|secrets:" + revealed;
        }
        return _guideSignature;
    }

    private static bool IsRoleAssigned(StageRole role)
    {
        foreach (RoleSnapshot snapshot in RoleAssignmentSync.Read())
        {
            if (snapshot.Role == role)
            {
                return true;
            }
        }
        return false;
    }

    private static string UtilitySignature() => _activeView switch
    {
        RoleMenuView.History => (BaseUpgradeHistory.IsDisplayPreview ? "preview:" : "") + (BaseUpgradeHistory.DisplayPayload ?? "unavailable"),
        RoleMenuView.Tools => RoleSyncStatus.Instance?.Describe(_guideLanguage) ?? "",
        _ => StageRolesPlugin.Instance.BugReport.LatestPath + _utilityMessage
    };

    private static void RefreshUtilityRows(REPOPopupPage page)
    {
        List<RoleMenuEntry> entries = new();
        void Text(string text)
        {
            foreach (string paragraph in text.Replace("\r", "").Split('\n'))
            {
                if (paragraph.Length == 0)
                { entries.Add(new RoleMenuEntry("", 18, FontStyles.Normal, 10, false)); continue; }
                foreach (string line in WrapGuideText(MeasurementText(page), paragraph, ContentWidth(page), _guideLanguage))
                    entries.Add(new RoleMenuEntry(line, GuideFontSize, FontStyles.Normal, GuideLineHeight, false, UseLanguageFont));
            }
        }
        void Button(string label, Action action)
        {
            entries.Add(new RoleMenuEntry("> " + label, 18, FontStyles.Bold, 40, false, UseLanguageFont, () =>
            {
                try { action(); }
                catch (Exception exception)
                {
                    _utilityMessage = Localized("Operation failed. Check the RoleShuffle log.", "操作に失敗しました。RoleShuffleのログを確認してください。");
                    StageRolesPlugin.ModLogger.LogError(exception);
                }
                if (_openPage == page && !RoleHudEditor.IsOpen && _issuesDialog == null) RefreshUtilityRows(page);
            }));
        }

        if (_activeView == RoleMenuView.History)
        {
            if (BaseUpgradeHistory.IsDisplayPreview)
                Text(Localized("TEST PREVIEW — sample draw history (local only).", "テスト表示 — サンプルの抽選履歴（自分にだけ表示）。"));
            else Text(Localized("This run's latest 50 draws, newest first. Values are shared levels.", "このセーブの直近50回を新しい順に表示。数値は共有レベルです。"));
            if (BaseUpgradeHistory.DisplayPayload == null)
                Text(Localized("History has not been received. Older hosts do not provide it.", "履歴を受信していません。旧バージョンのホストは履歴を配信しません。"));
            else if (!DrawHistoryStore.TryParse(BaseUpgradeHistory.DisplayPayload, out _))
                Text(Localized("History data is invalid or unsupported. Request a refresh from TOOLS.", "履歴データが不正、または未対応です。TOOLSから表示データを再取得してください。"));
            else if (BaseUpgradeHistory.ReadForDisplay().Count == 0)
                Text(Localized("No recorded draws in this run. Draws before this update cannot be recovered.", "このセーブに抽選履歴はありません。更新前の抽選結果は復元できません。"));
            foreach (UpgradeDrawRecord record in BaseUpgradeHistory.ReadForDisplay())
            { Text("\n" + BaseUpgradeHistory.Describe(record, _guideLanguage)); }
        }
        else if (_activeView == RoleMenuView.Tools)
        {
            Text(Localized("SYNC STATUS", "同期状態") + "\n" + (RoleSyncStatus.Instance?.Describe(_guideLanguage) ?? ""));
            Text(Localized("Display-data sync status.", "表示データの同期状態です。"));
            Button(Localized("REFRESH DISPLAY DATA", "表示データを再取得"), () => RoleSyncStatus.Instance?.RequestRefresh());
            Button(Localized("HUD EDITOR", "HUD編集モード"), () => RoleHudEditor.Open(_config, page, _roleRows[0].DefaultFont, _guideLanguage));
            Text(Localized("Adjust HUD position and size.", "HUDの位置とサイズを調整します。"));
            Button(Localized("REPORT A PROBLEM", "不具合レポート"), () => SwitchView(page, RoleMenuView.Report));
        }
        else
        {
            Text(Localized("Copy or open to create a report. Personal data is partly masked. Review before sharing; no automatic upload.", "コピー・開く操作でレポートを作成。個人情報は一部マスクされます。共有前に確認してください。自動送信はしません。"));
            Button(Localized("COPY REPORT", "レポートをコピー"), () =>
            {
                StageRolesPlugin.Instance.CreateBugReport();
                GUIUtility.systemCopyBuffer = StageRolesPlugin.Instance.BugReport.LatestText;
                _utilityMessage = Localized("Copied to clipboard.", "クリップボードにコピーしました。");
            });
            Button(Localized("OPEN SAVED REPORT", "保存したレポートを開く"), () =>
            {
                StageRolesPlugin.Instance.CreateBugReport();
                Application.OpenURL(new Uri(StageRolesPlugin.Instance.BugReport.LatestPath).AbsoluteUri);
                _utilityMessage = Localized("Saved to BepInEx/RoleShuffleReports. Add steps before sharing.", "BepInEx/RoleShuffleReportsに保存。共有前に再現手順を追記してください。");
            });
            Button(Localized("OPEN GITHUB ISSUES", "GitHub Issuesを開く"), () => ConfirmOpenIssues(page));
        }
        if (_utilityMessage.Length > 0)
            entries.Insert(0, new RoleMenuEntry(_utilityMessage, 18, FontStyles.Normal, 80, true, UseLanguageFont));
        ApplyEntries(page, entries);
        _openSignature = UtilitySignature();
    }

    private static void ConfirmOpenIssues(REPOPopupPage page)
    {
        if (_issuesDialog != null) return;
        bool decided = false;
        void Decide(bool open)
        {
            if (decided) return;
            decided = true;
            _issuesDialogDismissedFrame = Time.frameCount;
            if (!open || _openPage != page) return;
            try { Application.OpenURL(RoleBugReport.IssuesUrl); }
            catch (Exception exception)
            {
                _utilityMessage = Localized("Operation failed. Check the RoleShuffle log.", "操作に失敗しました。RoleShuffleのログを確認してください。");
                StageRolesPlugin.ModLogger.LogError(exception);
            }
        }
        MenuAPI.OpenPopup(Localized("OPEN EXTERNAL SITE", "外部サイトを開く"), new Color(1f, 0.65f, 0f),
            Localized("Open GitHub Issues in your browser? Nothing is sent automatically.", "ブラウザーでGitHub Issuesを開きます。自動送信はしません。") + "\n\n" + RoleBugReport.IssuesUrl,
            () => Decide(true), () => Decide(false));
        // OpenPopup creates the stock two-option dialog synchronously; its
        // singleton is only assigned in Start, so read the newly current page.
        _issuesDialog = AccessTools.Field(typeof(MenuManager), "currentMenuPage").GetValue(MenuManager.instance) as MenuPage;
        if (_issuesDialog == null) return;
        var dialog = _issuesDialog.GetComponent<MenuPageTwoOptions>();
        if (dialog == null) { _issuesDialog = null; return; }
        foreach (TextMeshProUGUI label in _issuesDialog.GetComponentsInChildren<TextMeshProUGUI>(true))
            label.font = RoleGuideFont.ForLanguage(label.font, _guideLanguage) ?? label.font;
        dialog.option1Button.buttonTextString = Localized("OPEN IN BROWSER", "ブラウザーで開く");
        dialog.option2Button.buttonTextString = Localized("CANCEL", "取消");
        dialog.bodyTextMesh.enableWordWrapping = true;
        dialog.bodyTextMesh.enableAutoSizing = true;
        dialog.bodyTextMesh.fontSizeMin = 12;
        dialog.bodyTextMesh.fontSizeMax = 18;
    }

    private static string DisplayUpgradeName(string name) => name switch
    {
        "ExtraJump" => "Extra Jump",
        "TumbleClimb" => "Tumble Climb",
        "TumbleWings" => "Tumble Wings",
        "CrouchRest" => "Crouch Rest",
        "MapPlayerCount" => "Map Player Count",
        "DeathHeadBattery" => "Death Head Battery",
        _ => name
    };

    private static string SignedValue(int value) =>
        value > 0 ? $"+{value}" : value.ToString(CultureInfo.InvariantCulture);

    private static TMP_Text MeasurementText(REPOPopupPage page)
    {
        if (_roleRows.Count == 0)
        {
            _roleRows.Add(CreateRoleRow(page));
        }
        return _roleRows[0].Label.labelTMP;
    }

    private static IReadOnlyList<string> WrapGuideText(
        TMP_Text measurementText,
        string text,
        float contentWidth,
        RoleGuideLanguage language)
    {
        List<string> lines = new();
        IReadOnlyList<string> tokens = WrapTokens(text);
        if (tokens.Count == 0)
        {
            return lines;
        }

        float previousFontSize = measurementText.fontSize;
        FontStyles previousFontStyle = measurementText.fontStyle;
        bool previousWrapping = measurementText.enableWordWrapping;
        bool previousAutoSizing = measurementText.enableAutoSizing;
        TMP_FontAsset previousFont = measurementText.font;
        float wrapWidth = contentWidth;
        if (language != RoleGuideLanguage.English)
        {
            TMP_FontAsset primaryFont = _roleRows.Count > 0
                ? _roleRows[0].DefaultFont
                : previousFont;
            TMP_FontAsset? mixedFont = RoleGuideFont.ForLanguage(primaryFont, language);
            if (mixedFont != null)
            {
                measurementText.font = mixedFont;
            }
            wrapWidth *= LanguageWrapWidthMultiplier;
        }
        measurementText.fontSize = GuideFontSize;
        measurementText.fontStyle = FontStyles.Normal;
        measurementText.enableWordWrapping = false;
        measurementText.enableAutoSizing = false;
        StringBuilder line = new();
        foreach (string token in tokens)
        {
            if (token == "\n")
            {
                if (line.Length > 0)
                {
                    lines.Add(line.ToString().TrimEnd());
                    line.Clear();
                }
                continue;
            }
            if (line.Length == 0 && string.IsNullOrWhiteSpace(token))
            {
                continue;
            }
            string candidate = line + token;
            if (line.Length == 0 ||
                measurementText.GetPreferredValues(candidate).x <= wrapWidth)
            {
                line.Clear();
                line.Append(candidate);
                continue;
            }

            lines.Add(line.ToString().TrimEnd());
            line.Clear();
            if (!string.IsNullOrWhiteSpace(token))
            {
                line.Append(token);
            }
        }
        if (line.Length > 0)
        {
            lines.Add(line.ToString().TrimEnd());
        }
        measurementText.fontSize = previousFontSize;
        measurementText.fontStyle = previousFontStyle;
        measurementText.enableWordWrapping = previousWrapping;
        measurementText.enableAutoSizing = previousAutoSizing;
        measurementText.font = previousFont;
        return lines;
    }

    private static IReadOnlyList<string> WrapTokens(string text)
    {
        List<string> tokens = new();
        StringBuilder westernToken = new();
        TextElementEnumerator elements =
            StringInfo.GetTextElementEnumerator(text);
        while (elements.MoveNext())
        {
            string element = elements.GetTextElement();
            char first = element[0];
            if (first == '\n')
            {
                if (westernToken.Length > 0)
                {
                    tokens.Add(westernToken.ToString());
                    westernToken.Clear();
                }
                tokens.Add("\n");
                continue;
            }
            if (first == '\r')
            {
                continue;
            }
            if (char.IsWhiteSpace(first) || IsCjk(first))
            {
                if (westernToken.Length > 0)
                {
                    tokens.Add(westernToken.ToString());
                    westernToken.Clear();
                }
                tokens.Add(char.IsWhiteSpace(first) ? " " : element);
                continue;
            }
            westernToken.Append(element);
        }
        if (westernToken.Length > 0)
        {
            tokens.Add(westernToken.ToString());
        }
        return tokens;
    }

    private static bool IsCjk(char value) =>
        value is >= '\u3040' and <= '\u30ff' or
        >= '\u3400' and <= '\u4dbf' or
        >= '\u4e00' and <= '\u9fff' or
        >= '\uf900' and <= '\ufaff' or
        >= '\uff00' and <= '\uffef';

    private static void ApplyEntries(
        REPOPopupPage page,
        IReadOnlyList<RoleMenuEntry> entries)
    {
        while (_roleRows.Count < entries.Count)
        {
            _roleRows.Add(CreateRoleRow(page));
        }

        float contentWidth = ContentWidth(page);
        for (int index = 0; index < _roleRows.Count; index++)
        {
            RoleMenuRow row = _roleRows[index];
            bool visible = index < entries.Count;
            row.Element.visibility = visible;
            if (!visible)
            {
                row.Emblem.gameObject.SetActive(false);
                row.Stepper?.Hide();
                continue;
            }

            RoleMenuEntry entry = entries[index];
            bool clickable = entry.OnClick != null;
            bool control = clickable || entry.IsControl;
            row.Label.rectTransform.gameObject.SetActive(true);
            row.Button.labelTMP.gameObject.SetActive(false);
            TMP_Text labelText = row.Label.labelTMP;
            labelText.richText = false;
            labelText.text = entry.Text;
            labelText.alignment = entry.Wrap
                ? TextAlignmentOptions.TopLeft
                : TextAlignmentOptions.Left;
            labelText.fontSize = entry.FontSize;
            labelText.fontStyle = entry.FontStyle;
            labelText.enableWordWrapping = entry.Wrap;
            labelText.enableAutoSizing = control || _activeView == RoleMenuView.BaseUpgrades;
            labelText.fontSizeMin = 12f;
            labelText.fontSizeMax = entry.FontSize;
            labelText.overflowMode = TextOverflowModes.Overflow;
            labelText.font = (UseLanguageFont || entry.UseLanguageFont)
                ? RoleGuideFont.ForLanguage(row.DefaultFont, _guideLanguage)
                : row.DefaultFont;
            row.Button.onClick = entry.OnClick;
            row.Button.menuButton.enabled = clickable;
            row.Visual.Configure(control, clickable, entry.IsOff);
            labelText.raycastTarget = false;
            Sprite? emblem = entry.EmblemRole.HasValue
                ? RoleEmblems.Get(entry.EmblemRole.Value, entry.UnrevealedEmblem, entry.EmblemGrayedOut) : null;
            row.Emblem.sprite = emblem;
            row.Emblem.gameObject.SetActive(emblem != null);
            float emblemSize = Mathf.Min(56f, entry.Height - 8f);
            float padding = control ? 12f : 0f;
            float textInset = padding + (emblem != null ? emblemSize + 10f : 0f);
            row.Emblem.rectTransform.sizeDelta = new Vector2(emblemSize, emblemSize);
            row.Emblem.rectTransform.anchoredPosition =
                new Vector2(padding, (entry.Height - emblemSize) * 0.5f);
            Vector2 size = new(contentWidth, entry.Height);
            row.Button.overrideButtonSize = size;
            row.Button.rectTransform.sizeDelta = size;
            row.Label.rectTransform.anchoredPosition = new Vector2(textInset, 0f);
            Vector2 textSize = new(contentWidth - textInset - padding, entry.Height);
            row.Label.rectTransform.sizeDelta = textSize;
            labelText.rectTransform.sizeDelta = textSize;
            if (emblem != null) labelText.overflowMode = TextOverflowModes.Ellipsis;
            float focusHeight = control ? entry.Height : Mathf.Max(12f, entry.Height * 0.5f);
            row.FocusRect.sizeDelta = new Vector2(contentWidth, focusHeight);
            row.FocusRect.anchoredPosition = new Vector2(
                0f,
                (entry.Height - focusHeight) * 0.5f);
            if (entry.Adjustment != null)
            {
                row.Stepper ??= new RoleMenuStepper(row.Button.rectTransform, page.menuScrollBox.scroller, row.DefaultFont);
                row.Stepper.Configure(entry.Adjustment, entry.Height);
            }
            else row.Stepper?.Hide();
        }
        page.scrollView.UpdateElements();
    }

    private static float ContentWidth(REPOPopupPage page)
    {
        float maskWidth = page.maskRectTransform != null
            ? page.maskRectTransform.rect.width
            : MaximumContentWidth;
        return Mathf.Clamp(
            maskWidth - 20f,
            MinimumContentWidth,
            MaximumContentWidth);
    }

    private static RoleMenuRow CreateRoleRow(REPOPopupPage page)
    {
        REPOButton? createdButton = null;
        REPOLabel? createdLabel = null;
        float topPadding = _roleRows.Count == 0 ? 20f : RowPadding;
        page.AddElementToScrollView(
            parent =>
            {
                createdButton = MenuAPI.CreateREPOButton(
                    string.Empty,
                    () => { },
                    parent,
                    Vector2.zero);
                createdLabel = MenuAPI.CreateREPOLabel(
                    string.Empty,
                    createdButton.rectTransform,
                    Vector2.zero);
                return createdButton.rectTransform;
            },
            topPadding: topPadding,
            bottomPadding: RowPadding);

        if (createdButton == null || createdLabel == null)
        {
            throw new InvalidOperationException("Failed to create a role menu row.");
        }
        REPOScrollViewElement element =
            createdButton.rectTransform.GetComponent<REPOScrollViewElement>();
        createdButton.menuButton.enabled = false;
        createdButton.labelTMP.gameObject.SetActive(false);
        createdButton.labelTMP.raycastTarget = false;
        createdButton.labelTMP.alignment = TextAlignmentOptions.Left;
        GameObject focusObject = new(
            "Role Focus Target",
            typeof(RectTransform),
            typeof(MenuSelectableElement));
        RectTransform focusRect = (RectTransform)focusObject.transform;
        focusRect.SetParent(createdButton.rectTransform, false);
        createdButton.menuButton.rectTransformSelection = focusRect;
        focusRect.anchorMin = Vector2.zero;
        focusRect.anchorMax = Vector2.zero;
        focusRect.pivot = Vector2.zero;
        createdLabel.labelTMP.raycastTarget = false;
        createdLabel.rectTransform.anchorMin = Vector2.zero;
        createdLabel.rectTransform.anchorMax = Vector2.zero;
        createdLabel.rectTransform.pivot = Vector2.zero;
        createdLabel.rectTransform.anchoredPosition = Vector2.zero;
        createdLabel.labelTMP.rectTransform.anchorMin = Vector2.zero;
        createdLabel.labelTMP.rectTransform.anchorMax = Vector2.zero;
        createdLabel.labelTMP.rectTransform.pivot = Vector2.zero;
        createdLabel.labelTMP.rectTransform.anchoredPosition = Vector2.zero;
        GameObject emblemObject = new("Role Emblem", typeof(RectTransform), typeof(Image));
        emblemObject.transform.SetParent(createdButton.rectTransform, false);
        Image emblem = emblemObject.GetComponent<Image>();
        emblem.raycastTarget = false;
        emblem.preserveAspect = true;
        emblem.color = Color.white;
        emblem.rectTransform.anchorMin = Vector2.zero;
        emblem.rectTransform.anchorMax = Vector2.zero;
        emblem.rectTransform.pivot = Vector2.zero;
        emblemObject.SetActive(false);
        RoleMenuButtonVisual visual = createdButton.gameObject.AddComponent<RoleMenuButtonVisual>();
        visual.Initialize(createdButton, createdLabel.labelTMP);
        return new RoleMenuRow(
            createdButton,
            createdLabel,
            element,
            focusRect,
            emblem,
            visual);
    }

    private static string BuildSignature(IReadOnlyList<RoleSnapshot> assignments)
    {
        bool testingEnabled = _config.SetRoleCommandEnabled.Value;
        string guidePayload = RoleGuideSync.CurrentSignature(_config);
        if (ReferenceEquals(_signatureAssignments, assignments) &&
            _signatureTestingEnabled == testingEnabled &&
            _signatureGuidePayload == guidePayload &&
            _signatureGuideLanguage == _guideLanguage)
        {
            return _assignmentsSignature;
        }
        StringBuilder builder = new();
        builder.Append(testingEnabled ? "numbers:on|" : "numbers:off|");
        builder.Append((int)_guideLanguage).Append('|').Append(guidePayload).Append('|');
        foreach (RoleSnapshot assignment in assignments)
        {
            builder.Append(assignment.SteamId)
                .Append('\u001f')
                .Append(assignment.PlayerName)
                .Append('\u001f')
                .Append((int)assignment.Role)
                .Append('\u001f')
                .Append((int)assignment.EffectiveRole)
                .Append('\u001f')
                .Append(assignment.PlayerNumber)
                .Append('\u001e');
        }
        _signatureAssignments = assignments;
        _signatureTestingEnabled = testingEnabled;
        _signatureGuidePayload = guidePayload;
        _signatureGuideLanguage = _guideLanguage;
        _assignmentsSignature = builder.ToString();
        return _assignmentsSignature;
    }

    private static void ClearOpenPageTracking()
    {
        RoleHudEditor.Instance?.Close(false);
        if (_issuesDialog != null) _issuesDialog.PageStateSet(MenuPage.PageState.Closing);
        _issuesDialog = null;
        _guideSignaturePayload = null;
        _guideSignature = string.Empty;
        _signatureAssignments = null;
        _assignmentsSignature = string.Empty;
        _signatureGuidePayload = string.Empty;
        IsOpen = false;
        _openPage = null;
        _assignmentsButton = null;
        _guideButton = null;
        _settingsButton = null;
        _baseUpgradesButton = null;
        _historyButton = null;
        _toolsButton = null;
        _languageButton = null;
        _backButton = null;
        _versionLabel = null;
        _roleRows.Clear();
        _openSignature = string.Empty;
        _selectedAssignmentKey = string.Empty;
        _activeView = RoleMenuView.Assignments;
        _guideLanguage = RoleGuideLanguage.English;
        _assignmentsEnabled = true;
    }

    internal static bool TryGetScrollSettings(
        MenuScrollBox scrollBox,
        out float lineHeight)
    {
        lineHeight = GuideLineHeight + RowPadding * 2f + RowSpacing;
        if (!IsOpen ||
            _openPage == null ||
            !ReferenceEquals(_openPage.menuScrollBox, scrollBox))
        {
            return false;
        }

        return true;
    }

    private readonly struct RoleMenuEntry(
        string text,
        float fontSize,
        FontStyles fontStyle,
        float height,
        bool wrap,
        bool useLanguageFont = false,
        Action? onClick = null,
        StageRole? emblemRole = null,
        bool unrevealedEmblem = false,
        bool isControl = false,
        bool emblemGrayedOut = false,
        RoleMenuAdjustment? adjustment = null,
        bool isOff = false)
    {
        internal string Text { get; } = text;
        internal float FontSize { get; } = fontSize;
        internal FontStyles FontStyle { get; } = fontStyle;
        internal float Height { get; } = height;
        internal bool Wrap { get; } = wrap;
        internal bool UseLanguageFont { get; } = useLanguageFont;
        internal Action? OnClick { get; } = onClick;
        internal StageRole? EmblemRole { get; } = emblemRole;
        internal bool UnrevealedEmblem { get; } = unrevealedEmblem;
        internal bool IsControl { get; } = isControl;
        internal bool EmblemGrayedOut { get; } = emblemGrayedOut;
        internal RoleMenuAdjustment? Adjustment { get; } = adjustment;
        internal bool IsOff { get; } = isOff;
    }

    private sealed class RoleMenuRow(
        REPOButton button,
        REPOLabel label,
        REPOScrollViewElement element,
        RectTransform focusRect,
        Image emblem,
        RoleMenuButtonVisual visual)
    {
        internal REPOButton Button { get; } = button;
        internal REPOLabel Label { get; } = label;
        internal REPOScrollViewElement Element { get; } = element;
        internal RectTransform FocusRect { get; } = focusRect;
        internal Image Emblem { get; } = emblem;
        internal RoleMenuButtonVisual Visual { get; } = visual;
        internal RoleMenuStepper? Stepper { get; set; }
        internal TMP_FontAsset DefaultFont { get; } = label.labelTMP.font;
    }

    private enum RoleMenuView
    {
        Assignments,
        Guide,
        BaseUpgrades,
        History,
        Tools,
        Report,
        Settings,
        Presets,
        BaseUpgradeSettings,
        BaseUpgradePresets
    }
}
