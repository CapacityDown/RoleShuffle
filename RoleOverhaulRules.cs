using System;
using System.Collections.Generic;

namespace REPOJP.StageRoles;

internal static class RoleOverhaulRules
{
    internal static bool GrowsWithBase(StageRole role) =>
        role is StageRole.Tank or StageRole.Runner or StageRole.Lifter;

    internal static int UpgradeTarget(int baseline, int minimum, int bonus, int maximum = 200) =>
        Math.Min(maximum, Math.Max(Math.Clamp(minimum, 0, maximum),
            Math.Clamp(baseline, 0, maximum) + Math.Clamp(bonus, 0, maximum)));

    internal static int StrengthTarget(int baseline, int minimum, int bonus, int maximum = 200)
    {
        baseline = Math.Clamp(baseline, 0, maximum);
        int requested = UpgradeTarget(baseline, minimum, bonus, maximum);
        // The configured role level is guaranteed even where vanilla force
        // decreases. Only bonus growth above that floor is penalty-limited.
        int floor = Math.Max(baseline, Math.Clamp(minimum, 0, maximum));
        double lightFloor = EffectiveGrabStrength(floor, lightObject: true);
        double heavyFloor = EffectiveGrabStrength(floor, lightObject: false);
        double lightRotationFloor = EffectiveGrabStrength(floor, lightObject: true, rotation: true);
        double heavyRotationFloor = EffectiveGrabStrength(floor, lightObject: false, rotation: true);
        for (int target = requested; target > floor; target--)
        {
            double light = EffectiveGrabStrength(target, lightObject: true);
            double heavy = EffectiveGrabStrength(target, lightObject: false);
            double lightRotation = EffectiveGrabStrength(target, lightObject: true, rotation: true);
            double heavyRotation = EffectiveGrabStrength(target, lightObject: false, rotation: true);
            if (light >= lightFloor && heavy >= heavyFloor &&
                lightRotation >= lightRotationFloor && heavyRotation >= heavyRotationFloor &&
                (light > lightFloor || heavy > heavyFloor || lightRotation > lightRotationFloor || heavyRotation > heavyRotationFloor))
                return target;
        }
        return floor;
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
// cannot replenish completed contracts or the King's healing allowance.
internal sealed class RoleOverhaulState
{
    private readonly HashSet<int> _delivered = new();
    internal bool Started { get; private set; }
    internal float PaidUntil { get; private set; }
    internal int ContractsCompleted => _delivered.Count;
    internal int CargoId { get; private set; }
    internal float CarryDistance { get; private set; }
    internal int KingHealingUsed { get; set; }
    internal float NextKingHealAt { get; set; }
    internal int NextKingTarget { get; set; }

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
