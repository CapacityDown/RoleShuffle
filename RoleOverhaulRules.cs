using System;
using System.Collections.Generic;

namespace REPOJP.StageRoles;

internal static class RoleOverhaulRules
{
    internal const int LifterStrengthLevel = 200;
    internal const int LifterReferenceLevel = 1;
    internal const int LifterEffectiveMultiplier = 6;
    internal static bool LifterPhysicsAvailable { get; set; }
    internal static double LifterEffectiveStrength(bool lightObject, bool rotation = false) =>
        EffectiveGrabStrength(LifterReferenceLevel, lightObject, rotation) * LifterEffectiveMultiplier;
    internal static bool LifterBaseReachesTarget(int baseline) =>
        EffectiveGrabStrength(baseline, true) + 1e-9 >= LifterEffectiveStrength(true) ||
        EffectiveGrabStrength(baseline, false) + 1e-9 >= LifterEffectiveStrength(false);
    internal static bool GrowsWithBase(StageRole role) =>
        role is StageRole.Tank or StageRole.Runner;

    // Vanilla 0.4.4.3: Health starts at 100 (+20/level). The serialized
    // PlayerController in level0 starts at speed 5 and stamina 40 (+1/+10).
    internal static double UpgradeValue(string command, int level) => command switch
    {
        "Health" => 100d + 20d * Math.Clamp(level, 0, 200),
        "Speed" => 5d + Math.Clamp(level, 0, 200),
        "Stamina" => 40d + 10d * Math.Clamp(level, 0, 200),
        _ => throw new ArgumentException("Unsupported linear upgrade.", nameof(command))
    };

    // ceil(maximum vanilla value over upgrade levels 0..200).
    internal static double MaximumValue(string command) => command switch
    {
        "Health" => 4100d,
        "Speed" => 205d,
        "Stamina" => 2040d,
        "Strength" => 6d,
        _ => throw new ArgumentException("Unsupported upgrade.", nameof(command))
    };

    internal static bool ReachesMaximum(string command, int level, double maximum)
    {
        double value = command == "Strength"
            ? Math.Max(EffectiveGrabStrength(level, true), EffectiveGrabStrength(level, false))
            : UpgradeValue(command, level);
        return value + 1e-9 >= Math.Clamp(maximum, 0d, MaximumValue(command));
    }

    private static double Multiplier(double value) =>
        double.IsNaN(value) || double.IsInfinity(value) ? 1d : Math.Clamp(Math.Round(value, 6), 1d, 10d);

    // A growth ceiling never removes existing Base levels or the configured
    // minimum. Those floors still obey the global upgrade limit of 200.
    private static int Floor(int baseline, int minimum) =>
        Math.Max(Math.Clamp(baseline, 0, 200), Math.Clamp(minimum, 0, 200));

    internal static int UpgradeTarget(string command, int baseline, int minimum, double multiplier, double maximum)
    {
        baseline = Math.Clamp(baseline, 0, 200);
        int floor = Floor(baseline, minimum);
        double cap = Math.Max(UpgradeValue(command, floor), Math.Clamp(maximum, 0d, MaximumValue(command)));
        int ceiling = floor;
        while (ceiling < 200 && UpgradeValue(command, ceiling + 1) <= cap) ceiling++;
        double desired = Math.Min(cap, UpgradeValue(command, baseline) * Multiplier(multiplier));
        for (int target = floor; target < ceiling; target++)
            if (UpgradeValue(command, target) + 1e-9 >= desired) return target;
        return ceiling;
    }

    // R.E.P.O. 0.4.4.3 PhysGrabObject reduces both translation and rotation,
    // with denominator 7 below mass 2 and 20 otherwise. Rotation omits the
    // strength-30 cap in the reduced value. Both curves are non-monotonic.
    // Used by King's non-weakening bonus selection; Lifter grants level 200.
    internal static double EffectiveGrabStrength(int level, bool lightObject, bool rotation = false)
    {
        double strength = 1d + 0.2d * Math.Clamp(level, 0, 200);
        double reduced = strength / (1d + (rotation ? strength : Math.Min(strength, 30d)));
        double blend = Math.Min((strength - 1d) / (lightObject ? 7d : 20d), 0.9d);
        return strength + (reduced - strength) * blend;
    }
}

// Owned by the stage assignment, so death, avatar replacement and rejoining
// cannot reward the same valuable again.
internal sealed class RoleOverhaulState
{
    private readonly HashSet<int> _delivered = new();
    internal bool Started { get; private set; }
    internal float PaidUntil { get; private set; }
    internal int ContractsCompleted => _delivered.Count;
    internal int CargoId { get; private set; }
    internal float CarryDistance { get; private set; }
    internal int KingSupportedAllies { get; set; }

    internal void Start(float now, float grace)
    {
        if (Started) return;
        Started = true;
        PaidUntil = now + grace;
    }

    internal void ResetCargo() { CargoId = 0; CarryDistance = 0; }
    internal bool IsDelivered(int cargoId) => _delivered.Contains(cargoId);

    internal bool Carry(int cargoId, float distance, bool inDeliveryArea, float now,
        float requiredDistance, float grace)
    {
        if (cargoId == 0 || _delivered.Contains(cargoId) ||
            float.IsNaN(distance) || float.IsInfinity(distance) || distance < 0 || distance > 4f)
        { ResetCargo(); return false; }
        if (CargoId != cargoId)
        {
            ResetCargo();
            if (inDeliveryArea) return false;
            CargoId = cargoId;
            return false; // The first sample only establishes the held object.
        }
        if (!inDeliveryArea) CarryDistance += distance;
        if (!inDeliveryArea || CarryDistance < requiredDistance) return false;
        _delivered.Add(cargoId);
        // A smaller delivery must not shorten an existing longer break.
        PaidUntil = Math.Max(PaidUntil, now + grace);
        ResetCargo();
        return true;
    }
}
