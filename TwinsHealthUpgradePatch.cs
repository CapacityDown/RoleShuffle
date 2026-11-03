using HarmonyLib;

namespace REPOJP.StageRoles;

// This runs after the level has changed and before the native maximum/owner heal.
// It also covers vanilla guests and works independently of upgrade-item retention.
[HarmonyPatch(typeof(PunManager), "UpdateHealthRightAway")]
internal static class TwinsHealthUpgradePatch
{
    [HarmonyPrefix]
    internal static void Prefix(string _steamID, int value) =>
        StageRolesPlugin.Instance?.Controller?.ApplyTwinsHealthUpgrade(_steamID, value);
}
