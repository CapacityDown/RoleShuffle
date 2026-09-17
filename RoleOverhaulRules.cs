using System;
using System.Collections.Generic;

namespace REPOJP.StageRoles;

internal static class RoleOverhaulRules
{
    internal static bool GrowsWithBase(StageRole role) =>
        role is StageRole.Tank or StageRole.Runner or StageRole.Lifter;

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

    internal static int StrengthTarget(int baseline, int minimum, double multiplier, double maximum = 6d)
    {
        baseline = Math.Clamp(baseline, 0, 200);
        multiplier = Multiplier(multiplier);
        // The configured role level is guaranteed even where vanilla force
        // decreases. Only bonus growth above that floor is penalty-limited.
        int floor = Floor(baseline, minimum);
        if (multiplier == 1d) return floor;
        double lightBase = EffectiveGrabStrength(baseline, lightObject: true);
        double heavyBase = EffectiveGrabStrength(baseline, lightObject: false);
        double lightFloor = EffectiveGrabStrength(floor, lightObject: true);
        double heavyFloor = EffectiveGrabStrength(floor, lightObject: false);
        double cap = Math.Max(Math.Max(lightFloor, heavyFloor), Math.Clamp(maximum, 0d, MaximumValue("Strength")));
        double lightGoal = Math.Min(cap, lightBase * multiplier);
        double heavyGoal = Math.Min(cap, heavyBase * multiplier);
        double lightRotationFloor = EffectiveGrabStrength(floor, lightObject: true, rotation: true);
        double heavyRotationFloor = EffectiveGrabStrength(floor, lightObject: false, rotation: true);
        int best = floor;
        double bestGain = Math.Min(lightFloor / lightBase, heavyFloor / heavyBase);
        for (int target = floor; target <= 200; target++)
        {
            double light = EffectiveGrabStrength(target, lightObject: true);
            double heavy = EffectiveGrabStrength(target, lightObject: false);
            double lightRotation = EffectiveGrabStrength(target, lightObject: true, rotation: true);
            double heavyRotation = EffectiveGrabStrength(target, lightObject: false, rotation: true);
            if (light > cap || heavy > cap || light < lightFloor || heavy < heavyFloor ||
                lightRotation < lightRotationFloor || heavyRotation < heavyRotationFloor) continue;
            // Find the lowest level meeting both grip goals. Rotation is a
            // safety constraint: its final torque is monotonic in this coefficient.
            if (light + 1e-9 >= lightGoal && heavy + 1e-9 >= heavyGoal) return target;
            double gain = Math.Min(light / lightBase, heavy / heavyBase);
            if (gain > bestGain + 1e-9) { best = target; bestGain = gain; }
        }
        // Unreachable goal: maximize the weaker relative grip improvement;
        // equal scores keep the lower level, avoiding arbitrary level inflation.
        return best;
    }

    // R.E.P.O. 0.4.4.3 PhysGrabObject reduces both translation and rotation,
    // with denominator 7 below mass 2 and 20 otherwise. Rotation omits the
    // strength-30 cap in the reduced value. Both curves are non-monotonic.
    // Raw levels also stay at least Base to preserve PhysGrabber rotation input.
    internal static double EffectiveGrabStrength(int level, bool lightObject, bool rotation = false)
    {
        double strength = 1d + 0.2d * Math.Clamp(level, 0, 200);
        double reduced = strength / (1d + (rotation ? strength : Math.Min(strength, 30d)));
        double blend = Math.Min((strength - 1d) / (lightObject ? 7d : 20d), 0.9d);
        return strength + (reduced - strength) * blend;
    }
}

// Owned by the stage assignment, so death, avatar replacement and rejoining
// cannot replenish completed contracts.
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

    internal bool Carry(int cargoId, float distance, bool inTruck, float now,
        float requiredDistance, float grace, int limit)
    {
        if (cargoId == 0 || _delivered.Contains(cargoId) || ContractsCompleted >= limit ||
            float.IsNaN(distance) || float.IsInfinity(distance) || distance < 0 || distance > 4f)
        { ResetCargo(); return false; }
        if (CargoId != cargoId)
        {
            ResetCargo();
            if (inTruck) return false;
            CargoId = cargoId;
            return false; // The first sample only establishes the held object.
        }
        if (!inTruck) CarryDistance += distance;
        if (!inTruck || CarryDistance < requiredDistance) return false;
        _delivered.Add(cargoId);
        PaidUntil = now + grace;
        ResetCargo();
        return true;
    }
}
