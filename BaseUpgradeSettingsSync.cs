using System.Text;
using Photon.Pun;

namespace REPOJP.StageRoles;

internal sealed class BaseUpgradeSettingsSnapshot(bool modEnabled, bool drawEnabled, string flags)
{
    internal bool ModEnabled { get; } = modEnabled;
    internal bool DrawEnabled { get; } = drawEnabled;
    internal bool IsEnabled(string name)
    {
        for (int i = 0; i < BaseUpgradeDrawSelection.UpgradeNames.Count; i++)
            if (BaseUpgradeDrawSelection.UpgradeNames[i] == name) return flags[i] == '1';
        return false;
    }
}

internal static class BaseUpgradeSettingsSync
{
    internal const string PropertyKey = "RS.BaseUpgradeSettingsV1";
    internal static string LocalSignature(StageRolesConfig config)
    {
        StringBuilder result = new();
        result.Append(config.Enabled.Value ? '1' : '0').Append(config.TruckUpgradeDrawEnabled.Value ? '1' : '0');
        foreach (string name in BaseUpgradeDrawSelection.UpgradeNames) result.Append(config.BaseUpgradeDrawIsEnabled(name) ? '1' : '0');
        return result.ToString();
    }
    internal static string CurrentSignature(StageRolesConfig config)
    {
        if (!SemiFunc.IsMultiplayer() || PhotonNetwork.IsMasterClient) return LocalSignature(config);
        return PhotonNetwork.CurrentRoom != null &&
            PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(PropertyKey, out object value) && value is string text
            ? text : "unavailable";
    }
    internal static BaseUpgradeSettingsSnapshot? Read(StageRolesConfig config)
    {
        string text = CurrentSignature(config);
        if (text.Length != BaseUpgradeDrawSelection.UpgradeNames.Count + 2) return null;
        foreach (char flag in text) if (flag is not ('0' or '1')) return null;
        return new BaseUpgradeSettingsSnapshot(text[0] == '1', text[1] == '1', text.Substring(2));
    }
}
