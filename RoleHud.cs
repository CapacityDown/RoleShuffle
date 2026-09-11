using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace REPOJP.StageRoles;

internal sealed class RoleHud : MonoBehaviour
{
    private const float RefreshIntervalSeconds = 0.25f;
    private const int MaximumNameCharacters = 20;
    private const float MinimumHeadingHeight = 36f;
    private const float HeadingGap = 4f;
    private const float MinimumRowHeight = 36f;
    private const float TextHeightPadding = 4f;
    private const float RowGap = 8f;
    private const float IconTextGap = 12f;
    private static readonly Vector2 LayoutSize = new(620f, 340f);
    private Vector2 CurrentLayoutSize => new(LayoutSize.x,
        Mathf.Max(LayoutSize.y, _headingHeight + HeadingGap + 2f * RowHeight));

    private readonly List<TMP_Text> _fontTargets = new();
    private readonly List<RoleSnapshot> _assignments = new();
    private readonly List<RoleSnapshot> _displayedAssignments = new();
    private readonly List<HudRow> _rows = new();
    private string _lastDisplayMode = string.Empty;
    private bool _rowsDirty;
    private float _headingHeight = MinimumHeadingHeight;
    private float _textRowHeight = MinimumRowHeight;
    private StageRolesConfig _config = null!;
    internal HudLayoutSettings? PreviewSettings;
    private Transform? _previewParent;
    private TMP_FontAsset? _previewFont;
    private int _lastFontSize;
    private readonly HudLayoutSettings _liveLayout = new();
    private HudLayoutSettings Layout => PreviewSettings ?? _liveLayout;
    private static readonly IReadOnlyList<RoleSnapshot> PreviewPlayers = new[]
    {
        new RoleSnapshot("preview-local", "YOU", StageRole.Tank, StageRole.Tank),
        new RoleSnapshot("preview-2", "Player 2", StageRole.Runner, StageRole.Runner),
        new RoleSnapshot("preview-3", "Player 3", StageRole.Medic, StageRole.Medic),
        new RoleSnapshot("preview-4", "Player 4", StageRole.Engineer, StageRole.Engineer),
        new RoleSnapshot("preview-5", "Player 5", StageRole.Ninja, StageRole.Ninja),
        new RoleSnapshot("preview-6", "Player 6", StageRole.Jumper, StageRole.Jumper)
    };
    private GameObject? _root;
    private RectTransform? _canvasRect;
    private RectTransform? _contentRect;
    private CanvasGroup? _canvasGroup;
    private TextMeshProUGUI? _heading;
    private (string Anchor, string Alignment, int X, int Y, int Scale,
        int CanvasWidth, int CanvasHeight, int ScreenWidth, int ScreenHeight)? _lastLayout;
    private (IReadOnlyList<RoleSnapshot>? Data, int PageSize, bool Pin, string? LocalId,
        string DisplayMode, int IconSize) _assignmentState;
    private float _nextRefreshAt;
    private float _nextFontProbeAt;
    private float _nextPageAt;
    private float _transitionStartedAt;
    private int _pageIndex;
    private bool _transitioning;
    private bool _pageSwitched;
    private bool _hasAssignments;

    internal void Initialize(StageRolesConfig config)
    {
        _config = config;
        _liveLayout.Refresh(config);
    }

    internal void InitializePreview(StageRolesConfig config, HudLayoutSettings settings, Transform parent, TMP_FontAsset font)
    {
        _config = config; PreviewSettings = settings; _previewParent = parent; _previewFont = font;
        EnsureCreated(); RefreshPreview();
    }

    internal void RefreshPreview()
    {
        _lastLayout = null; _assignmentState = default; _nextRefreshAt = 0;
        ApplyFontSize(); RefreshAssignments(); ApplyLayout(); UpdateRows();
    }

    internal bool ContainsPreview(Vector2 pointer) => _contentRect != null &&
        RectTransformUtility.RectangleContainsScreenPoint(_contentRect, pointer);

    internal void MovePreview(Vector2 pixels, int startX, int startY)
    {
        if (PreviewSettings == null) return;
        float scale = Screen.height / 540f;
        PreviewSettings.X = startX + Mathf.RoundToInt(pixels.x / scale);
        PreviewSettings.Y = startY + Mathf.RoundToInt(pixels.y / scale);
        ClampPreview();
    }

    internal void ClampPreview()
    {
        if (PreviewSettings == null || _contentRect == null || _canvasRect == null) return;
        ApplyLayout();
        Vector2 anchor = ResolveAnchor(PreviewSettings.Anchor);
        float scale = ResolveResolutionScale();
        Vector2 size = CurrentLayoutSize * _contentRect.localScale.x;
        PreviewSettings.X = HudLayoutMath.ClampOffset(PreviewSettings.X, anchor.x, _canvasRect.rect.width, size.x, scale, 3840);
        PreviewSettings.Y = HudLayoutMath.ClampOffset(PreviewSettings.Y, anchor.y, _canvasRect.rect.height, size.y, scale, 2160);
        ApplyLayout();
    }

    private void ApplyFontSize()
    {
        int size = Layout.FontSize;
        if (_lastFontSize == size) return;
        _lastFontSize = size;
        foreach (TMP_Text label in _fontTargets) label.fontSize = size;
        _assignmentState = default;
        _nextRefreshAt = 0;
    }

    private void Update()
    {
        _liveLayout.Refresh(_config);
        EnsureCreated();
        if (_root == null)
        {
            return;
        }

        ApplyFontSize();
        if (Time.unscaledTime >= _nextRefreshAt)
        {
            _nextRefreshAt = Time.unscaledTime + RefreshIntervalSeconds;
            RefreshAssignments();
        }

        bool visible = PreviewSettings != null || (!RoleHudEditor.IsOpen && _config.HudEnabled.Value &&
                       _config.Enabled.Value &&
                       _hasAssignments &&
                       IsPlayableStageContext());
        if (_root.activeSelf != visible)
        {
            _root.SetActive(visible);
        }
        if (!visible)
        {
            return;
        }

        UpdatePageTransition();
        ApplyLayout();
        TryApplyRepoUiFont();
        UpdateRows();
    }

    private void EnsureCreated()
    {
        if (_root != null)
        {
            return;
        }

        Transform? layerParent = _previewParent ?? (HealthUI.instance != null
            ? HealthUI.instance.transform.parent
            : HUDCanvas.instance?.transform);
        if (layerParent == null)
        {
            return;
        }

        _root = new GameObject("RoleShuffle_HUD", typeof(RectTransform));
        _root.hideFlags = HideFlags.HideAndDontSave;
        _root.transform.SetParent(layerParent, false);
        RectTransform rootRect = _root.GetComponent<RectTransform>();
        _canvasRect = rootRect;
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        if (HealthUI.instance != null && HealthUI.instance.transform.parent == layerParent)
        {
            _root.transform.SetSiblingIndex(
                HealthUI.instance.transform.GetSiblingIndex() + 1);
        }

        GameObject content = new(
            "RotatingRoles",
            typeof(RectTransform),
            typeof(CanvasGroup));
        content.transform.SetParent(_root.transform, false);
        _contentRect = content.GetComponent<RectTransform>();
        _contentRect.sizeDelta = LayoutSize;
        _canvasGroup = content.GetComponent<CanvasGroup>();
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;

        _rows.Clear();
        _fontTargets.Clear();
        _heading = CreateLabel("RoleHeading", content.transform);
        _lastDisplayMode = string.Empty;
        _lastLayout = null;
        _assignmentState = default;
        _root.SetActive(false);
    }

    private TextMeshProUGUI CreateLabel(string name, Transform parent)
    {
        GameObject obj = new(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        obj.transform.SetParent(parent, false);
        TextMeshProUGUI label = obj.GetComponent<TextMeshProUGUI>();
        label.fontSize = Layout.FontSize;
        if (_previewFont != null) label.font = _previewFont;
        label.color = new Color(0.9f, 0.91f, 0.87f, 1f);
        label.outlineColor = new Color(0f, 0f, 0f, 0.95f);
        label.outlineWidth = 0.15f;
        label.raycastTarget = false;
        label.enableWordWrapping = false;
        label.richText = false;
        label.overflowMode = TextOverflowModes.Truncate;
        label.alignment = TextAlignmentOptions.Left;
        if (_heading != null && _heading.font != null) label.font = _heading.font;
        _fontTargets.Add(label);
        return label;
    }

    private void RefreshAssignments()
    {
        IReadOnlyList<RoleSnapshot> current = PreviewSettings != null ? PreviewPlayers : RoleAssignmentSync.Read();
        _hasAssignments = current.Count > 0;
        var state = (current, _config.HudPlayersPerPage.Value,
            _config.HudPinLocalPlayer.Value, PlayerIdentity.SteamId(SemiFunc.PlayerGetLocal()),
            Layout.Display, IconSize);
        if (_assignmentState == state)
        {
            return;
        }

        _assignmentState = state;
        _assignments.Clear();
        foreach (RoleSnapshot snapshot in current)
        {
            _assignments.Add(snapshot);
        }
        UpdateTextMetrics();
        _pageIndex = 0;
        _transitioning = false;
        _pageSwitched = false;
        _nextPageAt = Time.unscaledTime + ClampedPageInterval;
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 1f;
        }
        UpdateDisplayedText();
    }

    private void UpdatePageTransition()
    {
        if (_canvasGroup == null)
        {
            return;
        }

        int pageCount = GetPageCount();
        if (pageCount <= 1)
        {
            _pageIndex = 0;
            _transitioning = false;
            _canvasGroup.alpha = 1f;
            return;
        }

        float now = Time.unscaledTime;
        if (!_transitioning)
        {
            if (now < _nextPageAt || RoleMenu.IsOpen)
            {
                return;
            }
            _transitioning = true;
            _pageSwitched = false;
            _transitionStartedAt = now;
        }

        float duration = Mathf.Clamp(
            _config.HudTransitionDurationSeconds.Value,
            0f,
            1f);
        if (duration <= 0.001f)
        {
            AdvancePage(pageCount);
            FinishTransition(now);
            return;
        }

        float half = duration * 0.5f;
        float elapsed = now - _transitionStartedAt;
        if (elapsed < half)
        {
            _canvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / half);
            return;
        }
        if (!_pageSwitched)
        {
            AdvancePage(pageCount);
            _pageSwitched = true;
        }
        if (elapsed < duration)
        {
            _canvasGroup.alpha = Mathf.Clamp01((elapsed - half) / half);
            return;
        }
        FinishTransition(now);
    }

    private void AdvancePage(int pageCount)
    {
        _pageIndex = (_pageIndex + 1) % pageCount;
        UpdateDisplayedText();
    }

    private void FinishTransition(float now)
    {
        _transitioning = false;
        _pageSwitched = false;
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 1f;
        }
        _nextPageAt = now + ClampedPageInterval;
    }

    private void UpdateDisplayedText()
    {
        _lastDisplayMode = Layout.Display;
        _rowsDirty = true;
        _displayedAssignments.Clear();
        if (_assignments.Count == 0)
        {
            if (_heading != null) _heading.SetText(string.Empty);
            return;
        }

        RoleSnapshot? local = FindLocalSnapshot();
        List<RoleSnapshot> others = new(_assignments.Count);
        foreach (RoleSnapshot snapshot in _assignments)
        {
            if (!local.HasValue ||
                !string.Equals(
                    snapshot.SteamId,
                    local.Value.SteamId,
                    StringComparison.Ordinal))
            {
                others.Add(snapshot);
            }
        }

        int pageSize = EffectivePageSize;
        int otherCapacity = Mathf.Max(1, pageSize - (local.HasValue ? 1 : 0));
        int pageCount = Mathf.Max(1, Mathf.CeilToInt(others.Count / (float)otherCapacity));
        _pageIndex = Mathf.Clamp(_pageIndex, 0, pageCount - 1);

        if (_heading != null)
        {
            // Restore TMP's text buffers after measuring arbitrary player labels.
            _heading.SetText(pageCount > 1
                ? $"ROLES  {_pageIndex + 1} / {pageCount}"
                : "ROLES");
        }
        if (local.HasValue)
        {
            _displayedAssignments.Add(local.Value);
        }

        int first = _pageIndex * otherCapacity;
        int last = Mathf.Min(first + otherCapacity, others.Count);
        for (int index = first; index < last; index++)
        {
            _displayedAssignments.Add(others[index]);
        }
    }

    private string FormatPlayerRole(RoleSnapshot snapshot, int index, bool hasIcon)
    {
        RoleSnapshot? local = FindLocalSnapshot();
        bool isLocal = local.HasValue && string.Equals(
            snapshot.SteamId, local.Value.SteamId, StringComparison.Ordinal);
        int otherCapacity = Mathf.Max(1, EffectivePageSize - (local.HasValue ? 1 : 0));
        int playerNumber = _pageIndex * otherCapacity + index + 1 - (local.HasValue ? 1 : 0);
        string playerName = isLocal ? "YOU" : Ellipsize(
            string.IsNullOrWhiteSpace(snapshot.PlayerName)
                ? $"Player {playerNumber}"
                : snapshot.PlayerName.Trim(), MaximumNameCharacters);
        // Never allow a player name to add extra HUD lines or rich-text markup.
        playerName = playerName.Replace('\n', ' ').Replace('\r', ' ');
        bool iconOnly = _lastDisplayMode == "IconOnly" && hasIcon;
        return iconOnly ? playerName
            : $"{playerName}: {RoleCatalog.AssignmentName(snapshot.Role, snapshot.EffectiveRole)}";
    }

    private int IconSize => Mathf.Clamp(Layout.IconSize, 32, 128);

    private float RowHeight => Layout.Display == "NameOnly"
        ? _textRowHeight : Mathf.Max(_textRowHeight, IconSize + RowGap);

    private void UpdateTextMetrics()
    {
        if (_heading == null || _heading.font == null) return;

        // TMP's Truncate mode discards even the first character when the font's
        // ascender-to-descender height exceeds the rectangle. Font size 28 does
        // not imply a 28-pixel line (Teko needs about 40, for example).
        _headingHeight = Mathf.Max(MinimumHeadingHeight,
            Mathf.Ceil(_heading.GetPreferredValues("ROLES  30 / 30").y) + TextHeightPadding);
        _textRowHeight = Mathf.Max(MinimumRowHeight,
            Mathf.Ceil(_heading.GetPreferredValues("YOU: Player").y) + TextHeightPadding);
        foreach (RoleSnapshot snapshot in _assignments)
        {
            string playerName = Ellipsize(
                string.IsNullOrWhiteSpace(snapshot.PlayerName) ? "Player" : snapshot.PlayerName.Trim(),
                MaximumNameCharacters).Replace('\n', ' ').Replace('\r', ' ');
            string sample = $"{playerName}: {RoleCatalog.AssignmentName(snapshot.Role, snapshot.EffectiveRole)}";
            _textRowHeight = Mathf.Max(_textRowHeight,
                Mathf.Ceil(_heading.GetPreferredValues(sample).y) + TextHeightPadding);
        }
        // Measure all assignments, including fallback glyphs, so page capacity
        // stays consistent when switching pages or hiding the icons.
        _rowsDirty = true;
        _lastLayout = null;
    }

    // Page before shrinking: detailed emblems must keep their requested size.
    private int EffectivePageSize => Mathf.Min(
        Mathf.Clamp(_config.HudPlayersPerPage.Value, 2, 20),
        Mathf.Max(2, Mathf.FloorToInt((CurrentLayoutSize.y - _headingHeight - HeadingGap) / RowHeight)));

    private void UpdateRows()
    {
        if (!_rowsDirty || _heading == null || _contentRect == null) return;
        _rowsDirty = false;
        bool show = _lastDisplayMode != "NameOnly";
        float rowHeight = RowHeight;
        // Keep the heading and pinned player in place on a partially filled last page.
        int rowCount = Mathf.Min(_assignments.Count, EffectivePageSize);
        float height = _headingHeight + HeadingGap + rowCount * rowHeight;
        float top = (CurrentLayoutSize.y - height) * 0.5f;
        RectTransform headingRect = _heading.rectTransform;
        headingRect.anchorMin = headingRect.anchorMax = new Vector2(0f, 1f);
        headingRect.pivot = new Vector2(0f, 1f);
        headingRect.anchoredPosition = new Vector2(0f, -top);
        headingRect.sizeDelta = new Vector2(LayoutSize.x, _headingHeight);

        for (int index = 0; index < _displayedAssignments.Count; index++)
        {
            if (index >= _rows.Count)
            {
                GameObject rowObject = new("PlayerRole", typeof(RectTransform));
                rowObject.transform.SetParent(_contentRect, false);
                RectTransform rowRect = rowObject.GetComponent<RectTransform>();
                rowRect.anchorMin = rowRect.anchorMax = new Vector2(0f, 1f);
                rowRect.pivot = new Vector2(0f, 1f);
                TextMeshProUGUI label = CreateLabel("RoleText", rowRect);
                label.rectTransform.anchorMin = label.rectTransform.anchorMax = new Vector2(0f, 0.5f);
                label.rectTransform.pivot = new Vector2(0f, 0.5f);
                GameObject obj = new("RoleEmblem", typeof(RectTransform), typeof(Image));
                obj.transform.SetParent(rowRect, false);
                Image icon = obj.GetComponent<Image>();
                icon.raycastTarget = false;
                icon.preserveAspect = true;
                icon.rectTransform.anchorMin = icon.rectTransform.anchorMax = new Vector2(0f, 0.5f);
                icon.rectTransform.pivot = new Vector2(0f, 0.5f);
                _rows.Add(new HudRow(rowRect, label, icon));
            }

            HudRow row = _rows[index];
            row.Rect.gameObject.SetActive(true);
            row.Rect.anchoredPosition = new Vector2(0f, -top - _headingHeight - HeadingGap - index * rowHeight);
            row.Rect.sizeDelta = new Vector2(LayoutSize.x, rowHeight);
            RoleSnapshot snapshot = _displayedAssignments[index];
            StageRole emblemRole = snapshot.Role == StageRole.Imitator ? snapshot.EffectiveRole : snapshot.Role;
            row.Icon.sprite = show ? RoleEmblems.Get(emblemRole) : null;
            bool hasIcon = row.Icon.sprite != null;
            row.Icon.gameObject.SetActive(hasIcon);
            row.Label.text = FormatPlayerRole(snapshot, index, hasIcon);

            float iconSpace = hasIcon ? IconSize + IconTextGap : 0f;
            float textWidth = Mathf.Clamp(Mathf.Ceil(row.Label.preferredWidth) + 2f, 1f, LayoutSize.x - iconSpace);
            float groupWidth = iconSpace + textWidth;
            float left = Layout.Alignment switch
            {
                "Center" => (LayoutSize.x - groupWidth) * 0.5f,
                "Right" => LayoutSize.x - groupWidth,
                _ => 0f
            };
            row.Icon.rectTransform.sizeDelta = new Vector2(IconSize, IconSize);
            row.Icon.rectTransform.anchoredPosition = new Vector2(left, 0f);
            row.Label.rectTransform.sizeDelta = new Vector2(textWidth, rowHeight);
            row.Label.rectTransform.anchoredPosition = new Vector2(left + iconSpace, 0f);
        }
        for (int index = _displayedAssignments.Count; index < _rows.Count; index++)
        {
            _rows[index].Rect.gameObject.SetActive(false);
        }
    }

    private sealed class HudRow(RectTransform rect, TextMeshProUGUI label, Image icon)
    {
        internal RectTransform Rect { get; } = rect;
        internal TextMeshProUGUI Label { get; } = label;
        internal Image Icon { get; } = icon;
    }

    private RoleSnapshot? FindLocalSnapshot()
    {
        if (PreviewSettings != null) return PreviewPlayers[0];
        if (!_config.HudPinLocalPlayer.Value)
        {
            return null;
        }
        string localSteamId = PlayerIdentity.SteamId(SemiFunc.PlayerGetLocal());
        if (string.IsNullOrEmpty(localSteamId))
        {
            return null;
        }
        foreach (RoleSnapshot snapshot in _assignments)
        {
            if (string.Equals(snapshot.SteamId, localSteamId, StringComparison.Ordinal))
            {
                return snapshot;
            }
        }
        return null;
    }

    private int GetPageCount()
    {
        if (_assignments.Count == 0)
        {
            return 1;
        }
        bool hasLocal = FindLocalSnapshot().HasValue;
        int pageSize = EffectivePageSize;
        int otherCapacity = Mathf.Max(1, pageSize - (hasLocal ? 1 : 0));
        int otherCount = Mathf.Max(0, _assignments.Count - (hasLocal ? 1 : 0));
        return Mathf.Max(1, Mathf.CeilToInt(otherCount / (float)otherCapacity));
    }

    private float ClampedPageInterval =>
        Mathf.Clamp(_config.HudPageIntervalSeconds.Value, 1f, 30f);

    private static string Ellipsize(string value, int maximumCharacters)
    {
        if (value.Length <= maximumCharacters)
        {
            return value;
        }
        return value.Substring(0, Mathf.Max(1, maximumCharacters - 1)) + "…";
    }

    private void ApplyLayout()
    {
        if (_contentRect == null || _heading == null)
        {
            return;
        }

        int canvasWidth = _canvasRect != null
            ? Mathf.RoundToInt(_canvasRect.rect.width)
            : Screen.width;
        int canvasHeight = _canvasRect != null
            ? Mathf.RoundToInt(_canvasRect.rect.height)
            : Screen.height;
        HudLayoutSettings layout = Layout;
        var signature = (layout.Anchor, layout.Alignment,
            layout.X, layout.Y, layout.Scale, canvasWidth, canvasHeight, Screen.width, Screen.height);
        if (_lastLayout == signature)
        {
            return;
        }
        _lastLayout = signature;
        _rowsDirty = true;

        Vector2 anchor = ResolveAnchor(layout.Anchor);
        float resolutionScale = ResolveResolutionScale();
        float userScale = layout.Scale / 100f;
        float scale = CalculateResponsiveScale(CurrentLayoutSize, userScale, resolutionScale);
        _contentRect.sizeDelta = CurrentLayoutSize;
        _contentRect.anchorMin = anchor;
        _contentRect.anchorMax = anchor;
        _contentRect.pivot = anchor;
        _contentRect.anchoredPosition = new Vector2(
            layout.X * resolutionScale,
            layout.Y * resolutionScale);
        _contentRect.localScale = Vector3.one * scale;
        _heading.alignment = layout.Alignment switch
        {
            "Center" => TextAlignmentOptions.Center,
            "Right" => TextAlignmentOptions.Right,
            _ => TextAlignmentOptions.Left
        };
    }

    private float ResolveResolutionScale()
    {
        float canvasHeight = _canvasRect != null && _canvasRect.rect.height > 1f
            ? _canvasRect.rect.height
            : Screen.height;
        return Mathf.Max(0.1f, canvasHeight / 540f);
    }

    private float CalculateResponsiveScale(
        Vector2 layoutSize,
        float userScale,
        float resolutionScale)
    {
        return HudLayoutMath.Scale(_canvasRect?.rect.width ?? 0, _canvasRect?.rect.height ?? 0,
            layoutSize.x, layoutSize.y, userScale, resolutionScale);
    }

    private void TryApplyRepoUiFont()
    {
        if (Time.unscaledTime < _nextFontProbeAt)
        {
            return;
        }
        _nextFontProbeAt = Time.unscaledTime + 2f;

        TMP_Text? source = HealthUI.instance != null
            ? HealthUI.instance.GetComponent<TextMeshProUGUI>()
            : null;
        source ??= ChatUI.instance?.chatText;
        if (source == null && HUD.instance != null)
        {
            foreach (TMP_Text candidate in HUD.instance.GetComponentsInChildren<TMP_Text>(true))
            {
                if (candidate != null &&
                    !_fontTargets.Contains(candidate) &&
                    candidate.font != null)
                {
                    source = candidate;
                    break;
                }
            }
        }
        if (source?.font == null)
        {
            return;
        }
        bool fontChanged = false;
        foreach (TMP_Text target in _fontTargets)
        {
            if (target != null && target.font != source.font)
            {
                target.font = source.font;
                fontChanged = true;
            }
        }
        if (fontChanged)
        {
            UpdateTextMetrics();
            UpdateDisplayedText();
        }
    }

    private static bool IsPlayableStageContext()
    {
        try
        {
            return SemiFunc.RunIsLevel() &&
                   !SemiFunc.RunIsLobby() &&
                   !SemiFunc.RunIsShop() &&
                   !SemiFunc.RunIsArena() &&
                   !SemiFunc.RunIsTutorial();
        }
        catch
        {
            return false;
        }
    }

    private static Vector2 ResolveAnchor(string anchor) => anchor switch
    {
        "TopLeft" => new Vector2(0f, 1f),
        "TopCenter" => new Vector2(0.5f, 1f),
        "TopRight" => new Vector2(1f, 1f),
        "MiddleLeft" => new Vector2(0f, 0.5f),
        "MiddleCenter" => new Vector2(0.5f, 0.5f),
        "MiddleRight" => new Vector2(1f, 0.5f),
        "BottomCenter" => new Vector2(0.5f, 0f),
        "BottomRight" => new Vector2(1f, 0f),
        _ => new Vector2(0f, 0f)
    };

    private void OnDestroy()
    {
        if (_root != null)
        {
            Destroy(_root);
        }
    }
}
