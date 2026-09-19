using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace REPOJP.StageRoles;

internal sealed class RoleResourceHud : MonoBehaviour
{
    // Serialized vanilla colors; live TMP colors may currently be flashing.
    private static readonly Color Healing = new(0.3373f, 1f, 0.4275f, 1f);
    private static readonly Color Energy = new(0.9893f, 1f, 0f, 1f);
    private readonly List<ResourceRow> _rows = new();
    private StageRolesConfig _config = null!;
    private GameObject? _root;
    private RectTransform? _canvas;
    private RectTransform? _content;
    private TextMeshProUGUI? _native;
    private TextMeshProUGUI? _nativeMax;
    private Image? _nativePlus;
    private Image? _nativeZap;
    private IReadOnlyList<AbilityValue> _values = Array.Empty<AbilityValue>();
    private float _nextRefresh;
    private float _iconInset;
    private float _rowWidth;
    private float _maximumOffset;
    private float _maximumY;
    private float _rowPitch;
    private Vector2 _origin;

    internal void Initialize(StageRolesConfig config) => _config = config;

    private void Update()
    {
        if (_config == null) return;
        bool visible = _config.Enabled.Value && _config.HudResourcesEnabled.Value &&
            !RoleHudEditor.IsOpen && RunManager.instance != null && GameManager.instance != null &&
            PlayerController.instance != null && HealthUI.instance != null && EnergyUI.instance != null &&
            HUD.instance != null && !HUD.instance.hidden && !HealthUI.instance.isHidden &&
            SemiFunc.RunIsLevel() && PlayerState.IsLiving(SemiFunc.PlayerGetLocal());
        if (!visible)
        {
            if (_root != null) _root.SetActive(false);
            _nextRefresh = 0;
            return;
        }
        EnsureCreated();
        if (_root == null || _content == null || _canvas == null || _native == null) return;
        if (Time.unscaledTime >= _nextRefresh)
        {
            _nextRefresh = Time.unscaledTime + 0.25f;
            Refresh();
        }
        _root.SetActive(_values.Count > 0);
        if (_values.Count == 0) return;
        var layout = RoleResourceLayout.Fit(_canvas.rect.width, _canvas.rect.height, _values.Count,
            _rowWidth, _native.rectTransform.rect.height, _rowPitch, _origin.x, _origin.y,
            _config.HudResourceScale.Value, _config.HudResourceOffsetX.Value, _config.HudResourceOffsetY.Value);
        _content.localScale = Vector3.one * layout.Scale;
        _content.anchoredPosition = new Vector2(layout.Left, -layout.Top);
        for (int i = 0; i < _values.Count; i++)
            _rows[i].Rect.anchoredPosition = new Vector2(i / layout.Rows * layout.ColumnPitch,
                -(i % layout.Rows) * _rowPitch);
    }

    private void EnsureCreated()
    {
        var source = HealthUI.instance.GetComponent<TextMeshProUGUI>();
        var sourceMax = HealthUI.instance.transform.Find("HealthMax")?.GetComponent<TextMeshProUGUI>();
        Transform? parent = HealthUI.instance.transform.parent;
        if (source?.font == null || sourceMax == null || parent == null) return;
        if (_root != null && _native == source && _root.transform.parent == parent) return;
        if (_root != null) Destroy(_root);
        _rows.Clear();
        _values = Array.Empty<AbilityValue>();
        _native = source;
        _nativeMax = sourceMax;
        _nativePlus = HealthUI.instance.transform.Find("Plus")?.GetComponent<Image>();
        _nativeZap = EnergyUI.instance.transform.Find("Zap")?.GetComponent<Image>();
        _iconInset = _nativePlus != null ? -_nativePlus.rectTransform.anchoredPosition.x : 28.1f;
        _maximumY = sourceMax.rectTransform.anchoredPosition.y;
        // Undo native digit-dependent movement before deriving the base maximum position.
        _maximumOffset = sourceMax.rectTransform.anchoredPosition.x - Math.Max(0, source.text.Length - 3) * 20;
        Vector2 health = RestingPosition(HealthUI.instance);
        Vector2 energy = RestingPosition(EnergyUI.instance);
        _rowPitch = Mathf.Max(25, health.y - energy.y);
        _origin = new Vector2(health.x - _iconInset, -energy.y + _rowPitch);
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
        group.interactable = group.blocksRaycasts = false;
        GameObject content = new("Resources", typeof(RectTransform));
        content.transform.SetParent(_canvas, false);
        _content = content.GetComponent<RectTransform>();
        TopLeft(_content);
    }

    private static Vector2 RestingPosition(SemiUI source)
    {
        var rect = (RectTransform)source.transform;
        // SemiUI.Start captures showPosition before spring/hide animation changes localPosition.
        return rect.anchoredPosition + source.showPosition - (Vector2)rect.localPosition;
    }

    private void Refresh()
    {
        _values = Array.Empty<AbilityValue>();
        string id = PlayerIdentity.SteamId(SemiFunc.PlayerGetLocal());
        foreach (RoleSnapshot role in RoleAssignmentSync.Read())
        {
            if (string.IsNullOrEmpty(id) || !string.Equals(id, role.SteamId, StringComparison.Ordinal)) continue;
            AbilitySnapshot? state = RoleAbilitySync.Read(role);
            if (state != null) _values = RoleAbilityResources.Select(state.Values, resources: true);
            break;
        }
        _rowWidth = _iconInset + _native!.rectTransform.rect.width;
        for (int i = 0; i < _values.Count; i++)
        {
            if (i == _rows.Count) _rows.Add(CreateRow());
            ResourceRow row = _rows[i];
            AbilityValue value = _values[i];
            row.Rect.gameObject.SetActive(true);
            bool healing = value.Metric is AbilityMetric.Medic or AbilityMetric.MageRecovery or AbilityMetric.Rescuer or AbilityMetric.Phoenix;
            Color ink = value.Remaining == 0 ? Color.red : healing ? Healing : Energy;
            SetText(row.Remaining, value.Remaining.ToString(CultureInfo.InvariantCulture));
            string slash = value.Remaining == 0 ? "red" : healing ? "#008b20" : "orange";
            string suffix = value.Metric is AbilityMetric.Repair or AbilityMetric.Charge ? "%" : string.Empty;
            SetText(row.Maximum, "<b><color=" + slash + ">/</color></b>" + value.Limit.ToString(CultureInfo.InvariantCulture) + suffix);
            row.Remaining.color = row.Maximum.color = row.Symbol.color = row.NativeIcon.color = ink;
            Sprite? sprite = value.Metric == AbilityMetric.Medic ? _nativePlus?.sprite :
                value.Metric == AbilityMetric.Charge ? _nativeZap?.sprite : null;
            row.NativeIcon.sprite = sprite;
            row.NativeIcon.enabled = sprite != null;
            row.Symbol.enabled = sprite == null;
            row.Symbol.SetMetric(value.Metric);
            float maximumX = RoleResourceLayout.MaximumOffset(value.Remaining, _maximumOffset);
            row.Maximum.rectTransform.anchoredPosition = new Vector2(_iconInset + maximumX, _maximumY);
            _rowWidth = Mathf.Max(_rowWidth, _iconInset + maximumX + row.Maximum.GetPreferredValues(row.Maximum.text).x + 4);
        }
        for (int i = _values.Count; i < _rows.Count; i++) _rows[i].Rect.gameObject.SetActive(false);
    }

    private ResourceRow CreateRow()
    {
        GameObject obj = new("Resource", typeof(RectTransform));
        obj.transform.SetParent(_content, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        TopLeft(rect);
        rect.sizeDelta = _native!.rectTransform.sizeDelta;
        TextMeshProUGUI remaining = CopyTextStyle("Remaining", rect, _native);
        remaining.rectTransform.anchoredPosition = new Vector2(_iconInset, 0);
        TextMeshProUGUI maximum = CopyTextStyle("Maximum", rect, _nativeMax!);
        GameObject vector = new("AbilitySymbol", typeof(RectTransform), typeof(RoleResourceIcon));
        vector.transform.SetParent(rect, false);
        RoleResourceIcon symbol = vector.GetComponent<RoleResourceIcon>();
        GameObject image = new("NativeSymbol", typeof(RectTransform), typeof(Image));
        image.transform.SetParent(rect, false);
        Image nativeIcon = image.GetComponent<Image>();
        foreach (Graphic icon in new Graphic[] { symbol, nativeIcon })
        {
            TopLeft(icon.rectTransform);
            icon.rectTransform.sizeDelta = _nativePlus != null ? _nativePlus.rectTransform.sizeDelta : new Vector2(25, 25);
            icon.rectTransform.anchoredPosition = Vector2.zero;
            icon.raycastTarget = false;
        }
        nativeIcon.preserveAspect = true;
        return new ResourceRow(rect, remaining, maximum, symbol, nativeIcon);
    }

    private static TextMeshProUGUI CopyTextStyle(string name, Transform parent, TextMeshProUGUI source)
    {
        // Copy TMP presentation only, never the HealthUI GameObject or its scripts.
        GameObject obj = new(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        obj.transform.SetParent(parent, false);
        var text = obj.GetComponent<TextMeshProUGUI>();
        TopLeft(text.rectTransform);
        text.rectTransform.sizeDelta = source.rectTransform.sizeDelta;
        text.font = source.font;
        // SemiUI flashes an instanced material; use the unmodified native font material.
        text.fontSharedMaterial = source.font.material;
        text.fontSize = source.fontSize;
        text.fontStyle = source.fontStyle;
        text.fontWeight = source.fontWeight;
        text.alignment = source.alignment;
        text.characterSpacing = source.characterSpacing;
        text.wordSpacing = source.wordSpacing;
        text.margin = source.margin;
        text.enableKerning = source.enableKerning;
        text.extraPadding = source.extraPadding;
        text.enableAutoSizing = false;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Overflow;
        text.richText = true;
        text.raycastTarget = false;
        return text;
    }

    private static void TopLeft(RectTransform rect) => rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
    private static void SetText(TMP_Text text, string value) { if (text.text != value) text.SetText(value); }
    private void OnDestroy() { if (_root != null) Destroy(_root); }

    private sealed class ResourceRow(RectTransform rect, TextMeshProUGUI remaining,
        TextMeshProUGUI maximum, RoleResourceIcon symbol, Image nativeIcon)
    {
        internal RectTransform Rect { get; } = rect;
        internal TextMeshProUGUI Remaining { get; } = remaining;
        internal TextMeshProUGUI Maximum { get; } = maximum;
        internal RoleResourceIcon Symbol { get; } = symbol;
        internal Image NativeIcon { get; } = nativeIcon;
    }
}
