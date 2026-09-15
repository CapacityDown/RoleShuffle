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
