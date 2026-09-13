using System.Globalization;
using MenuLib;
using MenuLib.MonoBehaviors;
using TMPro;
using UnityEngine;

namespace REPOJP.StageRoles;

// Two independent native buttons, inside a single scroll row and its mask.
internal sealed class RoleMenuStepper
{
    private const float ButtonSize = 28f;
    private const float Gap = 6f;
    private const float ValueWidth = 34f;
    private const float TextGap = 8f;
    internal const float ReservedWidth = ButtonSize * 2f + Gap + ValueWidth + TextGap * 2f;
    private readonly REPOLabel _value;
    private readonly REPOButton _minus;
    private readonly REPOButton _plus;
    private readonly RoleMenuButtonVisual _minusVisual;
    private readonly RoleMenuButtonVisual _plusVisual;

    internal RoleMenuStepper(Transform parent, Transform creationParent, TMP_FontAsset font)
    {
        _minus = CreateButton("-", parent, creationParent, font, out _minusVisual);
        _plus = CreateButton("+", parent, creationParent, font, out _plusVisual);
        _value = MenuAPI.CreateREPOLabel(string.Empty, creationParent, Vector2.zero);
        _value.rectTransform.SetParent(parent, false);
        _value.rectTransform.anchorMin = _value.rectTransform.anchorMax = _value.rectTransform.pivot = Vector2.zero;
        TMP_Text valueText = _value.labelTMP;
        valueText.font = font;
        valueText.fontSize = 21f;
        valueText.fontStyle = FontStyles.Bold;
        valueText.enableAutoSizing = true;
        valueText.fontSizeMin = 18f;
        valueText.fontSizeMax = 21f;
        valueText.enableWordWrapping = false;
        valueText.alignment = TextAlignmentOptions.Right;
        valueText.raycastTarget = false;
        valueText.rectTransform.anchorMin = valueText.rectTransform.anchorMax = valueText.rectTransform.pivot = Vector2.zero;
        valueText.rectTransform.anchoredPosition = Vector2.zero;
    }

    internal void Hide()
    {
        _minus.gameObject.SetActive(false);
        _plus.gameObject.SetActive(false);
        _value.gameObject.SetActive(false);
    }

    internal void Configure(RoleMenuAdjustment adjustment, float width, float height)
    {
        float buttonsLeft = width - ButtonSize * 2f - Gap;
        ConfigureButton(_minus, _minusVisual, adjustment.Decrease, buttonsLeft, height);
        ConfigureButton(_plus, _plusVisual, adjustment.Increase, width - ButtonSize, height);
        _value.gameObject.SetActive(true);
        _value.labelTMP.text = adjustment.CurrentLevel.ToString(CultureInfo.InvariantCulture);
        _value.rectTransform.anchoredPosition = new Vector2(buttonsLeft - TextGap - ValueWidth, 0f);
        Vector2 valueSize = new(ValueWidth, height);
        _value.rectTransform.sizeDelta = valueSize;
        _value.labelTMP.rectTransform.sizeDelta = valueSize;
    }

    private static void ConfigureButton(REPOButton button, RoleMenuButtonVisual visual,
        System.Action? click, float x, float height)
    {
        button.gameObject.SetActive(true);
        button.rectTransform.anchoredPosition = new Vector2(x, (height - ButtonSize) * 0.5f);
        button.onClick = click;
        button.menuButton.enabled = click != null;
        visual.Configure(true, click != null, false);
    }

    private static REPOButton CreateButton(string text, Transform parent, Transform creationParent, TMP_FontAsset font,
        out RoleMenuButtonVisual visual)
    {
        // MenuLib deactivates offscreen rows even while their visibility is true.
        // Its factory reads fields initialized by Awake, so create under the active
        // scroller first. An inactive parent leaves a failed "Quit game" clone.
        REPOButton button = MenuAPI.CreateREPOButton(text, () => { }, creationParent, Vector2.zero);
        button.menuButton.customHoverArea = true;
        button.menuButton.resizeButton = false;
        button.labelTMP.gameObject.SetActive(false);
        button.labelTMP.raycastTarget = false;
        Vector2 size = new(ButtonSize, ButtonSize);
        button.overrideButtonSize = size;
        RectTransform rect = button.rectTransform;
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
        rect.sizeDelta = size;
        // Native MenuButton animates its own label position/color every frame.
        // Use a separate visible label, as the other Roles UI buttons do.
        REPOLabel visibleLabel = MenuAPI.CreateREPOLabel(text, rect, Vector2.zero);
        visibleLabel.rectTransform.anchorMin = visibleLabel.rectTransform.anchorMax = visibleLabel.rectTransform.pivot = Vector2.zero;
        visibleLabel.rectTransform.anchoredPosition = Vector2.zero;
        visibleLabel.rectTransform.sizeDelta = size;
        TMP_Text label = visibleLabel.labelTMP;
        label.font = font;
        label.fontSize = 20f;
        label.enableAutoSizing = false;
        label.enableWordWrapping = false;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        label.rectTransform.anchorMin = label.rectTransform.anchorMax = label.rectTransform.pivot = Vector2.zero;
        label.rectTransform.anchoredPosition = Vector2.zero;
        label.rectTransform.sizeDelta = size;
        GameObject focus = new("Adjustment Focus Target", typeof(RectTransform), typeof(MenuSelectableElement));
        RectTransform focusRect = (RectTransform)focus.transform;
        focusRect.SetParent(rect, false);
        focusRect.anchorMin = focusRect.anchorMax = focusRect.pivot = Vector2.zero;
        focusRect.sizeDelta = size;
        button.menuButton.rectTransformSelection = focusRect;
        visual = button.gameObject.AddComponent<RoleMenuButtonVisual>();
        visual.Initialize(button, label);
        rect.SetParent(parent, false);
        return button;
    }
}
