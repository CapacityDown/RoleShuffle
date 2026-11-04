using System;
using MenuLib.MonoBehaviors;

namespace REPOJP.StageRoles;

internal sealed partial class RoleMenu
{
    private static int _presetTransferSlot = -1;
    private static CustomRolePreset? _presetTransfer, _presetTransferExpected;
    private static string PresetLabel(int slot, CustomRolePreset? preset)
    {
        string label = RoleText.Format("Custom {0}", _guideLanguage, slot + 1);
        return preset == null || preset.Name.Length == 0 ? label : label + " — " + preset.Name;
    }
    private static void PreviewPresetCopy(int source, int destination)
    {
        var preset = StageRolesPlugin.Instance.RoleSettings.CustomPresets.Get(source);
        if (!RoleSelectionSettings.CanEdit || preset == null || source == destination) return;
        PreviewPresetTransfer(destination, preset);
    }
    private static void PreviewPresetTransfer(int slot, CustomRolePreset preset)
    {
        _presetTransferSlot = slot;
        _presetTransfer = preset;
        _presetTransferExpected = StageRolesPlugin.Instance.RoleSettings.CustomPresets.Get(slot);
        _presetOverwriteSlot = -1;
        _utilityMessage = Localized("Review the destination slot below, then confirm.");
    }
    private static void ConfirmPresetTransfer(int slot, CustomRolePreset expectedDraft)
    {
        var service = StageRolesPlugin.Instance.RoleSettings;
        if (!RoleSelectionSettings.CanEdit || slot != _presetTransferSlot || !ReferenceEquals(expectedDraft, _presetTransfer)) return;
        if (!ReferenceEquals(_presetTransferExpected, service.CustomPresets.Get(slot)))
        { ClearPresetTransfer(); _utilityMessage = Localized("Preset changed. Try again."); return; }
        if (service.TryStoreCustomPreset(slot, expectedDraft, true))
        { ClearPresetTransfer(); _utilityMessage = Localized("Saved."); }
    }
    private static void ClearPresetTransfer()
    { _presetTransferSlot = -1; _presetTransfer = _presetTransferExpected = null; }
    private static void RefreshCurrentRoleList(REPOPopupPage page)
    {
        if (_activeView == RoleMenuView.Guide) RefreshGuideRows(page);
        else RefreshRoleSettingsRows(page);
    }
}
