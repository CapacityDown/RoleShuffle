using System;
using System.Collections.Generic;
using MenuLib.MonoBehaviors;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace REPOJP.StageRoles;

internal sealed class HudLayoutSettings
{
    internal string Anchor = "BottomLeft", Alignment = "Left", Display = "NameOnly";
    internal int X, Y = 80, Scale = 70, IconSize = 64, FontSize = 28;
    internal bool Enabled = true;
    internal void Refresh(StageRolesConfig config)
    {
        Anchor = config.HudAnchor.Value; Alignment = config.HudAlignment.Value; Display = config.HudRoleDisplay.Value;
        X = config.HudOffsetX.Value; Y = config.HudOffsetY.Value; Scale = config.HudScalePercent.Value;
        IconSize = config.HudIconSize.Value; FontSize = config.HudFontSize.Value; Enabled = config.HudEnabled.Value;
    }
    internal static HudLayoutSettings Read(StageRolesConfig config) => new()
    {
        Anchor = config.HudAnchor.Value, Alignment = config.HudAlignment.Value,
        Display = config.HudRoleDisplay.Value, X = config.HudOffsetX.Value, Y = config.HudOffsetY.Value,
        Scale = config.HudScalePercent.Value, IconSize = config.HudIconSize.Value,
        FontSize = config.HudFontSize.Value, Enabled = config.HudEnabled.Value
    };
    internal void Save(StageRolesConfig config)
    {
        config.HudAnchor.Value = Anchor; config.HudAlignment.Value = Alignment; config.HudRoleDisplay.Value = Display;
        config.HudOffsetX.Value = X; config.HudOffsetY.Value = Y; config.HudScalePercent.Value = Scale;
        config.HudIconSize.Value = IconSize; config.HudFontSize.Value = FontSize; config.HudEnabled.Value = Enabled;
    }
}

// A screen-space canvas keeps the actual HUD renderer usable in both lobby and Escape menus.
// Input is handled here because the game's menu navigation does not use a normal EventSystem.
internal sealed class RoleHudEditor : MonoBehaviour
{
    internal static RoleHudEditor? Instance { get; private set; }
    internal static bool IsOpen => Instance != null;
    private StageRolesConfig _config = null!;
    private REPOPopupPage _page = null!;
    private GameObject _overlay = null!;
    private RectTransform _canvas = null!;
    private RectTransform _toolbar = null!;
    private RectTransform _pointer = null!;
    private RoleHud _preview = null!;
    private HudLayoutSettings _draft = null!;
    private TMP_FontAsset _font = null!;
    private RoleGuideLanguage _language;
    private TextMeshProUGUI _status = null!;
    private readonly List<(RectTransform Rect, Image Background, TextMeshProUGUI Label, Func<string> Text, Action Click)> _buttons = new();
    private readonly List<(Behaviour Component, bool Enabled)> _disabled = new();
    private CanvasGroup? _pageGroup;
    private bool _addedGroup;
    private float _pageAlpha;
    private bool _pageInteractable, _pageRaycasts;
    private bool _dragging;
    private Vector2 _lastPointer;
    private int _dragX, _dragY;
    private int _openedFrame;
    private bool _closed;

    internal static void Open(StageRolesConfig config, REPOPopupPage page, TMP_FontAsset font, RoleGuideLanguage language)
    {
        if (IsOpen) return;
        GameObject obj = new("RoleShuffle_HudEditor");
        RoleHudEditor editor = obj.AddComponent<RoleHudEditor>();
        Instance = editor;
        try { editor.Initialize(config, page, font, language); }
        catch (Exception exception)
        {
            editor.Close(false);
            StageRolesPlugin.ModLogger.LogError($"Could not open HUD editor: {exception}");
        }
    }

    private void Initialize(StageRolesConfig config, REPOPopupPage page, TMP_FontAsset font, RoleGuideLanguage language)
    {
        _config = config; _page = page; _draft = HudLayoutSettings.Read(config); _language = language;
        _font = RoleGuideFont.ForLanguage(font, language) ?? font;
        _openedFrame = Time.frameCount;
        _pageGroup = page.GetComponent<CanvasGroup>();
        _addedGroup = _pageGroup == null;
        if (_addedGroup) _pageGroup = page.gameObject.AddComponent<CanvasGroup>();
        _pageAlpha = _pageGroup!.alpha; _pageInteractable = _pageGroup.interactable; _pageRaycasts = _pageGroup.blocksRaycasts;
        _pageGroup.alpha = 0; _pageGroup.interactable = false; _pageGroup.blocksRaycasts = false;
        foreach (Behaviour component in page.GetComponentsInChildren<Behaviour>(true))
            if (component is MenuButton || component is Button || component is MenuScrollBox)
            { _disabled.Add((component, component.enabled)); component.enabled = false; }

        _overlay = new GameObject("HUD Editor Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        _overlay.transform.SetParent(transform, false);
        Canvas canvas = _overlay.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 30000;
        CanvasScaler scaler = _overlay.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(960f, 540f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        _canvas = (RectTransform)_overlay.transform;
        RectTransform background = Box("Background", _canvas, new Color(0.025f, 0.035f, 0.055f, 0.94f));
        background.anchorMin = Vector2.zero; background.anchorMax = Vector2.one; background.sizeDelta = Vector2.zero;
        _preview = _overlay.AddComponent<RoleHud>();
        _preview.InitializePreview(config, _draft, _canvas, font);
        _toolbar = Box("Toolbar", _canvas, new Color(0.06f, 0.09f, 0.13f, 0.97f));
        _toolbar.anchorMin = new Vector2(0, 1); _toolbar.anchorMax = Vector2.one;
        _toolbar.pivot = new Vector2(0.5f, 1); _toolbar.sizeDelta = new Vector2(0, 154);
        Label(_toolbar, Pick("HUD EDITOR — drag to move; Save to apply", "HUD編集 — ドラッグで移動／保存で反映"),
            new Vector2(16, -7), new Vector2(900, 34), 18);
        Add(16, 44, 244, () => Pick("Display: ", "表示: ") + Pick(_draft.Display),
            () => { _draft.Display = Next(_draft.Display, "NameOnly", "IconAndName", "IconOnly"); Changed(); });
        Add(272, 44, 230, () => Pick("Anchor: ") + Pick(_draft.Anchor), () =>
        {
            _draft.Anchor = Next(_draft.Anchor, "BottomLeft", "BottomCenter", "BottomRight", "MiddleLeft", "MiddleCenter", "MiddleRight", "TopLeft", "TopCenter", "TopRight");
            _draft.X = 0; _draft.Y = 0; Changed();
        });
        Add(514, 44, 194, () => Pick("Align: ", "整列: ") + Pick(_draft.Alignment),
            () => { _draft.Alignment = Next(_draft.Alignment, "Left", "Center", "Right"); Changed(); });
        Add(720, 44, 220, () => "HUD: " + Pick(_draft.Enabled ? "ON" : "OFF"), () => _draft.Enabled = !_draft.Enabled);
        Pair(16, 80, Pick("Scale"), () => _draft.Scale, delta => { _draft.Scale = Mathf.Clamp(_draft.Scale + delta * 5, 50, 200); Changed(); });
        Pair(272, 80, Pick("Text", "文字"), () => _draft.FontSize, delta => { _draft.FontSize = Mathf.Clamp(_draft.FontSize + delta * 2, 16, 48); Changed(); });
        Pair(514, 80, Pick("Icon", "アイコン"), () => _draft.IconSize, delta => { _draft.IconSize = Mathf.Clamp(_draft.IconSize + delta * 8, 32, 128); Changed(); });
        Add(720, 80, 68, () => Pick("SAVE", "保存"), () => Close(true));
        Add(796, 80, 68, () => Pick("CANCEL", "取消"), () => Close(false));
        Add(872, 80, 68, () => Pick("RESET", "初期値"), () =>
        { _draft = new HudLayoutSettings(); _preview.PreviewSettings = _draft; Changed(); });
        _status = Label(_toolbar, "", new Vector2(16, -119), new Vector2(914, 28), 16);
        // The game's cursor is a mesh rendered behind ScreenSpaceOverlay.
        // Draw a pointer on our canvas instead of changing global cursor state.
        GameObject pointer = new("Editor Pointer", typeof(RectTransform), typeof(HudEditorPointer), typeof(Outline));
        pointer.transform.SetParent(_canvas, false);
        _pointer = (RectTransform)pointer.transform;
        _pointer.anchorMin = _pointer.anchorMax = new Vector2(0.5f, 0.5f);
        _pointer.pivot = new Vector2(0, 1);
        _pointer.sizeDelta = new Vector2(20, 28);
        pointer.GetComponent<HudEditorPointer>().raycastTarget = false;
        pointer.GetComponent<HudEditorPointer>().color = new Color(1f, 0.65f, 0f);
        pointer.GetComponent<Outline>().effectColor = new Color(0.02f, 0.02f, 0.02f, 1f);
        pointer.GetComponent<Outline>().effectDistance = new Vector2(1, -1);
        Canvas.ForceUpdateCanvases();
        Changed();
        UpdatePointer();
    }

    private string Pick(string english, string? japanese = null) => RoleText.Get(english, _language, japanese);
    private static string Next(string current, params string[] values) => values[(Array.IndexOf(values, current) + 1) % values.Length];
    private void Pair(float x, float y, string name, Func<int> value, Action<int> adjust)
    {
        Add(x, y, 44, () => "−", () => adjust(-1));
        // The center label also shows the live value without rebuilding the toolbar.
        Add(x + 48, y, 136, () => $"{name}: {value()}", () => { });
        Add(x + 188, y, 44, () => "+", () => adjust(1));
    }
    private void Add(float x, float y, float width, Func<string> text, Action click)
    {
        RectTransform rect = Box("Editor Button", _toolbar, new Color(0.15f, 0.20f, 0.27f));
        rect.anchorMin = rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(width, 28);
        var label = Label(rect, text(), new Vector2(5, -1), new Vector2(width - 10, 26), width < 75 ? 14 : 16);
        label.alignment = TextAlignmentOptions.Center;
        label.enableAutoSizing = true; label.fontSizeMin = 10; label.fontSizeMax = label.fontSize;
        _buttons.Add((rect, rect.GetComponent<Image>(), label, text, click));
    }
    private static RectTransform Box(string name, Transform parent, Color color)
    {
        GameObject obj = new(name, typeof(RectTransform), typeof(Image)); obj.transform.SetParent(parent, false);
        Image image = obj.GetComponent<Image>(); image.color = color; image.raycastTarget = false;
        return (RectTransform)obj.transform;
    }
    private TextMeshProUGUI Label(Transform parent, string text, Vector2 position, Vector2 size, int fontSize)
    {
        GameObject obj = new("Editor Label", typeof(RectTransform), typeof(TextMeshProUGUI)); obj.transform.SetParent(parent, false);
        var label = obj.GetComponent<TextMeshProUGUI>(); label.font = _font; label.fontSize = fontSize;
        label.text = text; label.color = Color.white; label.richText = false; label.raycastTarget = false;
        label.enableWordWrapping = false; label.overflowMode = TextOverflowModes.Ellipsis;
        label.rectTransform.anchorMin = label.rectTransform.anchorMax = new Vector2(0, 1);
        label.rectTransform.pivot = new Vector2(0, 1); label.rectTransform.anchoredPosition = position; label.rectTransform.sizeDelta = size;
        return label;
    }
    private void Changed() { _preview.RefreshPreview(); _preview.ClampPreview(); }
    private void Update()
    {
        if (_closed) return;
        if (_page == null || !RoleMenu.IsOpen) { Close(false); return; }
        if (!Application.isFocused) { _dragging = false; return; }
        SemiFunc.CursorUnlock(0.1f);
        if (Time.frameCount <= _openedFrame) return;
        Vector2 pointer = Input.mousePosition;
        foreach (var button in _buttons)
        {
            bool hover = RectTransformUtility.RectangleContainsScreenPoint(button.Rect, pointer);
            button.Background.color = hover ? new Color(0.25f, 0.38f, 0.48f) : new Color(0.15f, 0.20f, 0.27f);
            button.Label.text = button.Text();
            if (hover && Input.GetMouseButtonDown(0)) { button.Click(); return; }
        }
        bool toolbar = RectTransformUtility.RectangleContainsScreenPoint(_toolbar, pointer);
        if (Input.GetMouseButtonDown(0) && !toolbar && _preview.ContainsPreview(pointer))
        { _dragging = true; _lastPointer = pointer; _dragX = _draft.X; _dragY = _draft.Y; }
        if (!Input.GetMouseButton(0)) _dragging = false;
        if (_dragging)
        {
            _preview.MovePreview(pointer - _lastPointer, _dragX, _dragY);
        }
        _status.text = $"X: {_draft.X}   Y: {_draft.Y}   " + Pick("Sample HUD. Esc: cancel. RESET: defaults.", "サンプルHUD。Esc: 取消／初期値: リセット。");
    }
    private void LateUpdate()
    {
        if (!_closed) UpdatePointer();
    }
    private void UpdatePointer()
    {
        if (_pointer == null) return;
        Vector2 position = Input.mousePosition;
        bool visible = Application.isFocused && position.x >= 0 && position.x < Screen.width && position.y >= 0 && position.y < Screen.height;
        _pointer.gameObject.SetActive(visible);
        if (!visible) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvas, position, null, out Vector2 local);
        _pointer.anchoredPosition = local;
        _pointer.SetAsLastSibling();
    }
    internal void Close(bool save)
    {
        if (_closed) return;
        if (save)
        {
            try { _preview.ClampPreview(); _draft.Save(_config); StageRolesPlugin.Instance.SaveLocalSettings(); }
            catch (Exception exception)
            { _status.text = Pick("Could not save. Check the log and retry.", "保存できませんでした。ログを確認してください。"); StageRolesPlugin.ModLogger.LogError(exception); return; }
        }
        _closed = true;
        if (Instance == this) Instance = null;
        RestorePage();
        if (_overlay != null) _overlay.SetActive(false);
        Destroy(gameObject);
    }
    private void RestorePage()
    {
        foreach (var item in _disabled) if (item.Component != null) item.Component.enabled = item.Enabled;
        _disabled.Clear();
        if (_pageGroup != null)
        {
            if (_addedGroup) Destroy(_pageGroup);
            else { _pageGroup.alpha = _pageAlpha; _pageGroup.interactable = _pageInteractable; _pageGroup.blocksRaycasts = _pageRaycasts; }
        }
        _pageGroup = null;
    }
    private void OnDestroy() { RestorePage(); if (Instance == this) Instance = null; }
}

// A small outlined arrow whose tip coincides with the editor's mouse hit tests.
internal sealed class HudEditorPointer : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        mesh.AddVert(new Vector3(0, 0), color, Vector2.zero);
        mesh.AddVert(new Vector3(0, -24), color, Vector2.zero);
        mesh.AddVert(new Vector3(6, -18), color, Vector2.zero);
        mesh.AddVert(new Vector3(11, -28), color, Vector2.zero);
        mesh.AddVert(new Vector3(15, -26), color, Vector2.zero);
        mesh.AddVert(new Vector3(10, -16), color, Vector2.zero);
        mesh.AddVert(new Vector3(20, -16), color, Vector2.zero);
        mesh.AddTriangle(0, 1, 2);
        mesh.AddTriangle(0, 2, 5);
        mesh.AddTriangle(0, 5, 6);
        mesh.AddTriangle(2, 3, 4);
        mesh.AddTriangle(2, 4, 5);
    }
}
