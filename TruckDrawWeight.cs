using System;

namespace REPOJP.StageRoles;

internal static class TruckDrawWeight
{
    // Original Item.maxAmountInShop values from the shipped vanilla Item assets.
    // Do not read mutable runtime Items: other mods may change their shop limits.
    internal static int VanillaShopMaximum(string upgradeName) => upgradeName switch
    {
        "Stamina" => 4,
        "Speed" or "Range" or "CrouchRest" => 3,
        "Health" or "Strength" => 2,
        _ => 1
    };

    internal static int DefaultWeight(string upgradeName) =>
        upgradeName == "AllUpgrades" ? 1 : 10 * VanillaShopMaximum(upgradeName);

    internal static float Resolve(
        int weight, int level, int maximum, float cappedMultiplier, float exponent = 2f)
    {
        if (weight <= 0)
        {
            return 0f;
        }
        float multiplier = float.IsNaN(cappedMultiplier) || float.IsInfinity(cappedMultiplier)
            ? 0.25f
            : Math.Max(0f, Math.Min(1f, cappedMultiplier));
        float curve = float.IsNaN(exponent) || float.IsInfinity(exponent)
            ? 2f
            : Math.Max(0.1f, Math.Min(10f, exponent));
        double progress = maximum <= 0
            ? 1d
            : Math.Max(0d, Math.Min(1d, (double)level / maximum));
        return (float)(weight * (1d - (1d - multiplier) * Math.Pow(progress, curve)));
    }
}
