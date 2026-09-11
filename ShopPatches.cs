using HarmonyLib;

namespace REPOJP.StageRoles;

[HarmonyAfter(StageRolesPlugin.StageFluxGuid)]
internal static class ShopPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ShopManager), "GetAllItemsFromStatsManager")]
    private static void ShopManagerGetAllItemsFromStatsManagerPostfix(
        ShopManager __instance)
    {
        if (StageRolesPlugin.Instance?.Settings.Enabled.Value != true ||
            !SemiFunc.IsMasterClientOrSingleplayer())
        {
            return;
        }
        int requestedCount = UnityEngine.Mathf.Clamp(
            StageRolesPlugin.Instance.Settings.ShopUpgradeItemCount.Value,
            0,
            30);
        __instance.itemUpgradesAmount = requestedCount;
        if (requestedCount == 0)
        {
            __instance.potentialItemUpgrades?.Clear();
        }
        StageRolesPlugin.ModLogger.LogDebug(
            $"Shop upgrade item count set to {requestedCount}.");
    }
}
