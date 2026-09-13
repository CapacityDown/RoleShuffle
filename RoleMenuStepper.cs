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
    private readonly REPOButton _minus;
    private readonly REPOButton _plus;
    private readonly RoleMenuButtonVisual _minusVisual;
    private readonly RoleMenuButtonVisual _plusVisual;

    internal RoleMenuStepper(Transform parent, Transform creationParent, TMP_FontAsset font)
    {
        _minus = CreateButton("-", parent, creationParent, font, out _minusVisual);
        _plus = CreateButton("+", parent, creationParent, font, out _plusVisual);
    }

    internal void Hide()
    {
        _minus.gameObject.SetActive(false);
        _plus.gameObject.SetActive(false);
    }

    internal void Configure(RoleMenuAdjustment adjustment, float height)
    {
        ConfigureButton(_minus, _minusVisual, adjustment.Decrease, 0f, height);
        ConfigureButton(_plus, _plusVisual, adjustment.Increase, ButtonSize + Gap, height);
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
