using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace REPOJP.StageRoles;

// Own UI objects only: cloning HealthUI would also run vanilla health logic.
internal sealed class RoleResourceHud : MonoBehaviour
{
    private static readonly Color Ink = new(0.94f, 0.95f, 0.9f, 1f);
    private static readonly Color Accent = new(1f, 0.75f, 0.3f, 1f);
    private static readonly Color Empty = new(1f, 0.32f, 0.28f, 1f);
    private readonly List<ResourceRow> _rows = new();
    private StageRolesConfig _config = null!;
    private GameObject? _root;
    private RectTransform? _canvas;
    private RectTransform? _content;
    private TMP_FontAsset? _numberFont;
    private IReadOnlyList<AbilityValue> _values = Array.Empty<AbilityValue>();
    private float _nextRefresh;

    internal void Initialize(StageRolesConfig config) => _config = config;

    private void Update()
    {
        if (_config == null) return;
        // Do not touch Photon or local-player helpers while a scene is loading.
        bool visible = _config.Enabled.Value && _config.HudResourcesEnabled.Value &&
            !RoleHudEditor.IsOpen && RunManager.instance != null && GameManager.instance != null &&
            PlayerController.instance != null && HealthUI.instance != null && HUD.instance != null &&
            !HUD.instance.hidden && !HealthUI.instance.isHidden && SemiFunc.RunIsLevel() &&
            PlayerState.IsLiving(SemiFunc.PlayerGetLocal());
        if (!visible)
        {
            if (_root != null) _root.SetActive(false);
            _nextRefresh = 0;
            return;
        }

        EnsureCreated();
        if (_root == null || _content == null || _canvas == null) return;
        if (Time.unscaledTime >= _nextRefresh)
        {
            _nextRefresh = Time.unscaledTime + 0.25f;
            Refresh();
        }
        _root.SetActive(_values.Count > 0);
        if (_values.Count == 0) return;
        var layout = RoleResourceLayout.Fit(_canvas.rect.width, _canvas.rect.height, _values.Count,
            _config.HudResourceScale.Value, _config.HudResourceOffsetX.Value, _config.HudResourceOffsetY.Value);
        _content.sizeDelta = new Vector2(RoleResourceLayout.Width, _values.Count * RoleResourceLayout.RowHeight);
        _content.localScale = Vector3.one * layout.Scale;
        _content.anchoredPosition = new Vector2(layout.Left, -layout.Top);
    }

    private void EnsureCreated()
    {
        Transform? parent = HealthUI.instance.transform.parent;
        TMP_FontAsset? font = HealthUI.instance.GetComponent<TextMeshProUGUI>()?.font;
        if (parent == null || font == null) return;
        if (_root != null && _root.transform.parent == parent && _numberFont == font) return;
        if (_root != null) Destroy(_root);
        _rows.Clear();
        _values = Array.Empty<AbilityValue>();
        _numberFont = font;
        _nextRefresh = 0;
        _root = new GameObject("RoleShuffle_AbilityResources", typeof(RectTransform), typeof(CanvasGroup));
        _root.hideFlags = HideFlags.HideAndDontSave;
        _root.SetActive(false);
        _root.transform.SetParent(parent, false);
        _canvas = _root.GetComponent<RectTransform>();
        _canvas.anchorMin = Vector2.zero;
        _canvas.anchorMax = Vector2.one;
        _canvas.offsetMin = _canvas.offsetMax = Vector2.zero;
        CanvasGroup group = _root.GetComponent<CanvasGroup>();
        group.interactable = false;
        group.blocksRaycasts = false;
        GameObject content = new("Resources", typeof(RectTransform));
        content.transform.SetParent(_canvas, false);
        _content = content.GetComponent<RectTransform>();
        TopLeft(_content);
    }

    private void Refresh()
    {
        _values = Array.Empty<AbilityValue>();
        string id = PlayerIdentity.SteamId(SemiFunc.PlayerGetLocal());
        if (!string.IsNullOrEmpty(id))
        {
            // Independent of role-list pagination, visibility and local-player pinning.
            foreach (RoleSnapshot role in RoleAssignmentSync.Read())
            {
                if (!string.Equals(id, role.SteamId, StringComparison.Ordinal)) continue;
                AbilitySnapshot? state = RoleAbilitySync.Read(role);
                if (state != null) _values = RoleAbilityResources.Select(state.Values, resources: true);
                break;
            }
        }
        RoleGuideLanguage language = RoleLanguage.Parse(_config.GuideLanguage.Value);
        TMP_FontAsset? labelFont = _values.Count > 0 ? RoleGuideFont.ForLanguage(_numberFont, language) : null;
        for (int i = 0; i < _values.Count; i++)
        {
            if (i == _rows.Count) _rows.Add(CreateRow());
            ResourceRow row = _rows[i];
            AbilityValue value = _values[i];
            row.Rect.gameObject.SetActive(true);
            row.Rect.anchoredPosition = new Vector2(0, -i * RoleResourceLayout.RowHeight);
            if (labelFont != null && row.Label.font != labelFont) row.Label.font = labelFont;
            SetText(row.Label, RoleAbilityText.Label(value.Metric, language));
            SetText(row.Remaining, value.Remaining.ToString(CultureInfo.InvariantCulture));
            SetText(row.Maximum, "/" + value.Limit.ToString(CultureInfo.InvariantCulture) + RoleAbilityResources.Unit(value.Metric));
            row.Remaining.color = value.Remaining == 0 ? Empty : Ink;
            row.Maximum.color = value.Remaining == 0 ? Empty : Accent;
            row.Icon.sprite = RoleEmblems.Get(RoleAbilityResources.Emblem(value.Metric));
            row.Icon.color = value.Remaining == 0 ? new Color(0.65f, 0.65f, 0.65f, 1f) : Color.white;
            // Keep the smaller maximum directly after the actual number, including five-digit limits.
            float numberWidth = Mathf.Min(106f, row.Remaining.GetPreferredValues(row.Remaining.text).x + 3f);
            row.Remaining.rectTransform.sizeDelta = new Vector2(numberWidth, 34);
            row.Maximum.rectTransform.anchoredPosition = new Vector2(38 + numberWidth, -23);
            row.Maximum.rectTransform.sizeDelta = new Vector2(RoleResourceLayout.Width - 38 - numberWidth, 24);
        }
        for (int i = _values.Count; i < _rows.Count; i++) _rows[i].Rect.gameObject.SetActive(false);
    }

    private ResourceRow CreateRow()
    {
        GameObject obj = new("Resource", typeof(RectTransform));
        obj.transform.SetParent(_content, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        TopLeft(rect);
        rect.sizeDelta = new Vector2(RoleResourceLayout.Width, RoleResourceLayout.RowHeight);
        GameObject iconObject = new("RoleEmblem", typeof(RectTransform), typeof(Image));
        iconObject.transform.SetParent(rect, false);
        Image icon = iconObject.GetComponent<Image>();
        TopLeft(icon.rectTransform);
        icon.rectTransform.anchoredPosition = new Vector2(0, -12);
        icon.rectTransform.sizeDelta = new Vector2(32, 32);
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        TextMeshProUGUI label = CreateText("ResourceLabel", rect, 13, 38, 0, RoleResourceLayout.Width - 38, 17);
        label.color = Ink;
        TextMeshProUGUI remaining = CreateText("Remaining", rect, 32, 38, -14, 106, 34);
        TextMeshProUGUI maximum = CreateText("Maximum", rect, 18, 70, -23, 150, 24);
        return new ResourceRow(rect, icon, label, remaining, maximum);
    }

    private TextMeshProUGUI CreateText(string name, Transform parent, float size, float x, float y, float width, float height)
    {
        GameObject obj = new(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        obj.transform.SetParent(parent, false);
        TextMeshProUGUI text = obj.GetComponent<TextMeshProUGUI>();
        TopLeft(text.rectTransform);
        text.rectTransform.anchoredPosition = new Vector2(x, y);
        text.rectTransform.sizeDelta = new Vector2(width, height);
        text.font = _numberFont;
        text.fontSize = text.fontSizeMax = size;
        text.fontSizeMin = size * 0.7f;
        text.enableAutoSizing = true;
        text.alignment = TextAlignmentOptions.BottomLeft;
        text.enableWordWrapping = false;
        text.richText = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
        text.outlineColor = new Color(0, 0, 0, 0.9f);
        text.outlineWidth = 0.12f;
        return text;
    }

    private static void TopLeft(RectTransform rect) =>
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);

    private static void SetText(TMP_Text text, string value)
    {
        if (text.text != value) text.SetText(value);
    }

    private void OnDestroy()
    {
        if (_root != null) Destroy(_root);
    }

    private sealed class ResourceRow(RectTransform rect, Image icon, TextMeshProUGUI label,
        TextMeshProUGUI remaining, TextMeshProUGUI maximum)
    {
        internal RectTransform Rect { get; } = rect;
        internal Image Icon { get; } = icon;
        internal TextMeshProUGUI Label { get; } = label;
        internal TextMeshProUGUI Remaining { get; } = remaining;
        internal TextMeshProUGUI Maximum { get; } = maximum;
    }
}
