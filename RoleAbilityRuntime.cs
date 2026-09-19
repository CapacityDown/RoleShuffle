using System;
using System.Collections.Generic;
using UnityEngine;

namespace REPOJP.StageRoles;

internal sealed partial class StageRoleController
{
    private float _nextAbilityPublishAt;

    private void PublishAbilityStatus()
    {
        if (Time.unscaledTime < _nextAbilityPublishAt) return;
        _nextAbilityPublishAt = Time.unscaledTime + 0.5f;
        List<AbilitySnapshot> snapshots = new();
        foreach (RoleAssignment assignment in _assignments) snapshots.Add(BuildAbilityStatus(assignment));
        RoleAbilitySync.Publish(snapshots);
    }

    private AbilitySnapshot BuildAbilityStatus(RoleAssignment assignment)
    {
        List<AbilityValue> values = new();
        bool Has(StageRole role) => RoleCatalog.HasCapability(assignment.Role, role);
        void Budget(AbilityMetric metric, float used, float limit) =>
            values.Add(RoleAbilityResources.Budget(metric, used, limit));
        void Seconds(AbilityMetric metric, float until) =>
            values.Add(new AbilityValue(metric, Mathf.CeilToInt(Mathf.Max(0f, until - Time.time)), 0));
        if (Has(StageRole.Medic)) Budget(AbilityMetric.Medic, _medic.HealingUsed(assignment.SteamId), _config.MedicTotalHealingLimit.Value);
        if (Has(StageRole.Rescuer)) Budget(AbilityMetric.Rescuer, assignment.RescuerRevivesUsed, _config.RescuerMaximumRevives.Value);
        if (Has(StageRole.Phoenix)) Budget(AbilityMetric.Phoenix, assignment.PhoenixUsed || assignment.PhoenixRevivePending ? 1 : 0, 1);
        if (Has(StageRole.Mage))
        {
            Seconds(AbilityMetric.MageCooldown, assignment.MageNextCastAt);
            if (_config.MageAutoRecoveryEnabled.Value) Budget(AbilityMetric.MageRecovery, assignment.MageAutoRecoveryUsed, _config.MageAutoRecoveryTotalHealingLimit.Value);
        }
        if (Has(StageRole.Mechanic)) Budget(AbilityMetric.Repair, assignment.MechanicRepairPercentUsed, _config.MechanicMaximumRepairPercentPerStage.Value);
        if (Has(StageRole.Electrician)) Budget(AbilityMetric.Charge, assignment.ElectricianChargePercentUsed, _config.ElectricianMaximumChargePercentPerStage.Value);
        if (Has(StageRole.Gambler)) Budget(AbilityMetric.Wager, assignment.GamblerWagersUsed, 1);
        if (Has(StageRole.Trickster)) values.Add(_trickster.Status(assignment.SteamId));
        if (Has(StageRole.Diver)) values.Add(_diver.Status(assignment.SteamId));
        if (Has(StageRole.Avenger)) Seconds(AbilityMetric.Avenger, assignment.AvengerEmpoweredUntil);
        if (Has(StageRole.Stinker)) values.Add(new AbilityValue(AbilityMetric.CloudDistance,
            Mathf.FloorToInt(assignment.StinkerTravelDistance), Mathf.CeilToInt(_config.ClampedStinkerDistance)));
        if (Has(StageRole.Bomber)) values.Add(new AbilityValue(AbilityMetric.GrenadeDistance,
            Mathf.FloorToInt(assignment.TravelDistance), Mathf.CeilToInt(_config.BomberDistance.Value)));
        if (assignment.Role == StageRole.King)
            values.Add(new AbilityValue(AbilityMetric.RoyalSupport, assignment.Overhaul.KingSupportedAllies, 0));
        if (assignment.Role == StageRole.Jobless)
        {
            Budget(AbilityMetric.Contracts, assignment.Overhaul.ContractsCompleted, _config.JoblessContractLimit.Value);
            Seconds(AbilityMetric.Grace, assignment.Overhaul.PaidUntil);
            values.Add(new AbilityValue(AbilityMetric.Carry, Mathf.FloorToInt(assignment.Overhaul.CarryDistance),
                Mathf.CeilToInt(_config.JoblessContractDistance.Value)));
        }
        return new AbilitySnapshot(assignment.SteamId, assignment.AssignedRole, assignment.Role, values);
    }
}
