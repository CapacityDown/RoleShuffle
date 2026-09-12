using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using Photon.Pun;

namespace REPOJP.StageRoles;

internal sealed class RoleSelectionSettings(StageRolesConfig settings, ConfigFile file)
{
    internal static bool CanEdit => GameManager.instance != null &&
        (!SemiFunc.IsMultiplayer() || PhotonNetwork.IsMasterClient);

    internal bool TrySetEnabled(StageRole role, bool enabled)
    {
        if (!CanEdit || settings.RoleEnabledEntry(role) == null) return false;
        Save(new Dictionary<StageRole, bool> { [role] = enabled });
        return true;
    }

    internal bool TryApplyPreset(RolePreset id)
    {
        if (!CanEdit) return false;
        RolePresetDefinition? preset = RolePresets.Find(id);
        if (preset == null) return false;
        Dictionary<StageRole, bool> values = new();
        foreach (StageRole role in Enum.GetValues(typeof(StageRole))) values[role] = preset.Includes(role);
        Save(values);
        return true;
    }

    private void Save(IReadOnlyDictionary<StageRole, bool> values)
    {
        bool autoSave = file.SaveOnConfigSet;
        Dictionary<ConfigEntry<bool>, bool> previous = new();
        file.SaveOnConfigSet = false;
        try
        {
            foreach (var pair in values)
            {
                ConfigEntry<bool> entry = settings.RoleEnabledEntry(pair.Key)!;
                previous[entry] = entry.Value;
                entry.Value = pair.Value;
            }
            // Use the same entries as REPOConfig. SettingChanged handles host
            // publication; saving once avoids rewriting the file for every role.
            file.Save();
        }
        catch
        {
            foreach (var pair in previous) pair.Key.Value = pair.Value;
            throw;
        }
        finally { file.SaveOnConfigSet = autoSave; }
    }
}
