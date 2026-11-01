using System;
using System.Collections.Generic;

namespace REPOJP.StageRoles;

internal static class NegotiatorRules
{
    private static readonly string[] Sizes = { "Tiny", "Small", "Medium", "Big", "Wide", "Tall", "VeryTall" };
    internal static bool AllowsSize(string configured, string size)
    {
        if (Array.IndexOf(Sizes, size) < 0) return false;
        foreach (string token in (configured ?? string.Empty).Split(','))
            if (string.Equals(token.Trim(), size, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }
    internal static string DisplaySizes(string configured)
    {
        List<string> enabled = new();
        foreach (string size in Sizes) if (AllowsSize(configured, size)) enabled.Add(size);
        return enabled.Count == 0 ? "—" : string.Join(", ", enabled);
    }
}
