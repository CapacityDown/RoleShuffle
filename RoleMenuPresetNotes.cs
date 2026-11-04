using System;
using MenuLib;
using MenuLib.MonoBehaviors;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace REPOJP.StageRoles;

internal sealed partial class RoleMenu
{
    private static void EditCustomPresetNote(REPOPopupPage parentPage, int slot)
    {
        if (_issuesDialog != null || _openPage != parentPage || _activeView != RoleMenuView.Presets ||
            !RoleSelectionSettings.CanEdit) return;
        var service = StageRolesPlugin.Instance.RoleSettings;
        var preset = service.CustomPresets.Get(slot);
        if (preset == null) return;
        _presetOverwriteSlot = -1;
        string name = RoleText.Format("Custom {0}", _guideLanguage, slot + 1);
        REPOPopupPage editor = MenuAPI.CreateREPOPopupPage(
            RoleText.Format("Edit note: {0}", _guideLanguage, name),
            REPOPopupPage.PresetSide.Right, shouldCachePage: false, pageDimmerVisibility: true, spacing: 12f);
        var padding = editor.maskPadding;
        padding.top = 24f;
        padding.bottom = 60f;
        editor.maskPadding = padding;
        REPOInputField? input = null;
        REPOLabel? hint = null;
        bool closed = false;
        void Style(TMP_Text text)
        {
            text.font = RoleGuideFont.ForLanguage(text.font, _guideLanguage) ?? text.font;
            RoleTextLayout.Apply(text, _guideLanguage);
            text.richText = false;
            text.enableAutoSizing = true;
            text.fontSizeMin = 12;
            text.fontSizeMax = 18;
            text.fontSize = 18;
        }
        void Close(bool save)
        {
            if (closed) return;
            bool current = _openPage == parentPage && _activeView == RoleMenuView.Presets &&
                ReferenceEquals(service, StageRolesPlugin.Instance.RoleSettings);
            if (save && current)
            {
                try
                {
                    if (!service.TrySetCustomPresetNote(slot, input!.inputStringSystem.currentValue))
                        throw new InvalidOperationException("Preset note is no longer editable.");
                    _utilityMessage = Localized("Saved.");
                }
                catch (Exception exception)
                {
                    hint!.labelTMP.text = Localized("Operation failed. Check the RoleShuffle log.");
                    StageRolesPlugin.ModLogger.LogError(exception);
                    return; // Retain the draft when saving fails.
                }
            }
            closed = true;
            if (input != null) input.inputStringSystem.isFocused = false;
            _issuesDialogDismissedFrame = Time.frameCount;
            editor.ClosePage(closePagesAddedOnTop: true);
            if (current) RefreshRoleSettingsRows(parentPage);
        }
        try
        {
            Style(editor.headerTMP);
            editor.headerTMP.fontSizeMax = 24;
            editor.AddElementToScrollView(parent =>
            {
                hint = MenuAPI.CreateREPOLabel(RoleText.Format(
                    "Up to {0} characters. Leave blank to remove the note.", _guideLanguage,
                    CustomRolePresets.MaximumNoteLength), parent, Vector2.zero);
                Style(hint.labelTMP);
                hint.labelTMP.textWrappingMode = TextWrappingModes.Normal;
                hint.rectTransform.sizeDelta = new Vector2(ContentWidth(editor), 60f);
                return hint.rectTransform;
            });
            editor.AddElementToScrollView(parent =>
            {
                input = MenuAPI.CreateREPOInputField(string.Empty, value =>
                {
                    string limited = CustomRolePresets.NormalizeNote(value, trim: false);
                    if (input != null && limited != value) input.inputStringSystem.SetValue(limited, false);
                }, parent, Vector2.zero, onlyNotifyOnSubmit: false, placeholder: string.Empty, defaultValue: preset.Note);
                float width = ContentWidth(editor);
                input.rectTransform.sizeDelta = new Vector2(width, 40f);
                input.labelTMP.gameObject.SetActive(false);
                // MenuLib's stock field reserves space for a label. Use the whole
                // row for the note, keeping its native focus and clipboard handling.
                foreach (string childName in new[] { "SliderBG", "RawImage", "RawImage (1) Left" })
                {
                    var decoration = input.transform.Find(childName);
                    if (decoration != null) decoration.gameObject.SetActive(false);
                }
                var system = input.inputStringSystem;
                var mask = system.maskRectTransform;
                mask.anchorMin = mask.anchorMax = mask.pivot = Vector2.zero;
                mask.anchoredPosition = new Vector2(12f, 4f);
                mask.sizeDelta = new Vector2(width - 24f, 32f);
                var background = new GameObject("Note background", typeof(RectTransform), typeof(Image), typeof(Outline));
                var backgroundRect = (RectTransform)background.transform;
                backgroundRect.SetParent(input.transform, false);
                backgroundRect.SetAsFirstSibling();
                backgroundRect.anchorMin = backgroundRect.anchorMax = backgroundRect.pivot = Vector2.zero;
                backgroundRect.anchoredPosition = new Vector2(10f, 2f);
                backgroundRect.sizeDelta = new Vector2(width - 20f, 36f);
                var fill = background.GetComponent<Image>();
                fill.color = new Color(0.08f, 0.08f, 0.08f, 0.95f);
                fill.raycastTarget = false;
                background.GetComponent<Outline>().effectColor = new Color(0.8f, 0.8f, 0.8f, 1f);
                Style(system.inputTMP);
                system.inputTMP.enableAutoSizing = false;
                system.inputTMP.textWrappingMode = TextWrappingModes.NoWrap;
                system.inputTMP.textPreprocessor = new PresetNoteInputText(() => system.currentValue);
                input.gameObject.AddComponent<PresetNoteInputIme>().InputField = system;
                return input.rectTransform;
            });
            editor.AddElement(parent =>
            {
                var save = MenuAPI.CreateREPOButton(Localized("SAVE"), () => Close(true), parent, new Vector2(40f, 18f));
                var cancel = MenuAPI.CreateREPOButton(Localized("CANCEL"), () => Close(false), parent, new Vector2(200f, 18f));
                Style(save.labelTMP);
                Style(cancel.labelTMP);
            });
            editor.onEscapePressed += () => { Close(false); return false; };
            // Share the existing modal/escape guard with other Roles dialogs.
            _issuesDialog = editor.menuPage;
            editor.OpenPage(openOnTop: true);
        }
        catch
        {
            editor.ClosePage(closePagesAddedOnTop: true);
            _issuesDialogDismissedFrame = Time.frameCount;
            throw;
        }
    }
}

// The game's chat turns IME off while idle. Enable it only while this field
// owns focus, then restore the prior state when focus/the modal is lost.
internal sealed class PresetNoteInputIme : MonoBehaviour
{
    internal REPOInputStringSystem? InputField;
    private bool _ownsIme;
    private IMECompositionMode _previousMode;
    private Keyboard? _keyboard;

    private void LateUpdate()
    {
        if (InputField == null || !InputField.isFocused) { Restore(); return; }
        if (!_ownsIme)
        {
            _previousMode = Input.imeCompositionMode;
            _keyboard = Keyboard.current;
            _ownsIme = true;
            _keyboard?.SetIMEEnabled(true);
        }
        Input.imeCompositionMode = IMECompositionMode.On;
    }

    private void OnDisable() => Restore();
    private void OnDestroy() => Restore();
    private void Restore()
    {
        if (!_ownsIme) return;
        _ownsIme = false;
        Input.imeCompositionMode = _previousMode;
        _keyboard?.SetIMEEnabled(_previousMode != IMECompositionMode.Off);
        _keyboard = null;
    }
}
