using System;

namespace REPOJP.StageRoles;

internal static class MechanicRepairBudget
{
    // Vanilla healing floors displayed dollars. Round up only the final
    // accumulated budget so its fractional tail actually repairs the item.
    // Ordinary ticks keep accumulating fractions without rounding them up.
    internal static float RepairValue(
        float displayedOriginalValue, float displayedCurrentValue,
        float accumulatedPercent, float remainingPercent)
    {
        if (!IsFinite(displayedOriginalValue) || !IsFinite(displayedCurrentValue) ||
            !IsFinite(accumulatedPercent) || !IsFinite(remainingPercent) ||
            displayedOriginalValue <= 0f || displayedCurrentValue < 0f ||
            displayedCurrentValue >= displayedOriginalValue ||
            accumulatedPercent <= 0f || remainingPercent <= 0f)
            return 0f;

        double requested = (double)displayedOriginalValue *
            Math.Min(accumulatedPercent, remainingPercent) / 100d;
        double rounded = accumulatedPercent >= remainingPercent
            ? Math.Ceiling(requested) : Math.Floor(requested);
        return (float)Math.Min(rounded,
            Math.Ceiling((double)displayedOriginalValue - displayedCurrentValue));
    }

    private static bool IsFinite(float value) =>
        !float.IsNaN(value) && !float.IsInfinity(value);
}
