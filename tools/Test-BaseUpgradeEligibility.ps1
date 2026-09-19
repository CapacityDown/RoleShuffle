$ErrorActionPreference = 'Stop'
$roleSource = Get-Content -LiteralPath (Join-Path $PSScriptRoot '../RoleModels.cs') -Raw
$match = [regex]::Match($roleSource, '(?s)    internal static bool BaseUpgradeMeetsOrExceedsRoleTarget\(.*?(?=    internal static bool InfluencerHasUpgradeBenefit)')
if (-not $match.Success) { throw 'Production eligibility method not found.' }
$methodSource = $match.Value.Replace('internal static bool', 'public static bool')
$targetMatch = [regex]::Match($roleSource, '(?s)    internal static IReadOnlyList<UpgradeGrant> TargetUpgrades\(.*?(?=    private static IReadOnlyList<UpgradeGrant> RoleUpgrades)')
if (-not $targetMatch.Success) { throw 'Production upgrade-target method not found.' }
$rules = Get-Content -LiteralPath (Join-Path $PSScriptRoot '../RoleOverhaulRules.cs') -Raw
$rules = $rules.Replace('using System;', '').Replace('using System.Collections.Generic;', '').Replace('namespace REPOJP.StageRoles;', '')
$testSource = @'
using System;
using System.Collections.Generic;
__RULES__
public enum StageRole { Tank, Runner, Jumper, Lifter, Launcher, Climber, Flyer, Tracker, Ghost, Mage, Superbot }
public class UpgradeGrant {
    public string CommandName;
    public string DictionaryName;
    public int Level;
    public UpgradeGrant(string name, int level) { CommandName = DictionaryName = name; Level = level; }
    public UpgradeGrant(string command, string name, int level) { CommandName = command; DictionaryName = name; Level = level; }
}
public class Entry<T> { public T Value; public Entry(T value) { Value = value; } }
public class StageRolesConfig {
    public UpgradeGrant[] Bases;
    public UpgradeGrant[] Targets;
    public Entry<float> TankHealthMultiplier = new(1.5f);
    public Entry<float> RunnerSpeedMultiplier = new(1.5f);
    public Entry<float> RunnerStaminaMultiplier = new(1.5f);
    public Entry<int> TankMaximumHealth = new(4100);
    public Entry<int> RunnerMaximumSpeed = new(205);
    public Entry<int> RunnerMaximumStamina = new(2040);

}
public static class EligibilityChecks {
    static IReadOnlyList<UpgradeGrant> BaseUpgrades(StageRolesConfig config) { return config.Bases; }
    static IReadOnlyList<UpgradeGrant> RoleUpgrades(StageRole role, StageRolesConfig config) { return config.Targets; }
__METHOD__
__TARGETS__
    public static void Run() {
        int count = 0;
        for (int target = 0; target <= 100; target++) {
            for (int baseline = 0; baseline <= 101; baseline++) {
                var config = new StageRolesConfig {
                    Bases = new[] { new UpgradeGrant("Health", baseline) },
                    Targets = new[] { new UpgradeGrant("Health", target) },
                    TankHealthMultiplier = new(1f)
                };
                if (BaseUpgradeMeetsOrExceedsRoleTarget(StageRole.Tank, config) != (baseline >= target))
                    throw new Exception("With multiplier one, only the configured minimum can provide a gain");
                count++;
            }
        }
        var tracker = new StageRolesConfig {
            Bases = new[] { new UpgradeGrant("Map", 1), new UpgradeGrant("Health", 1) },
            Targets = new[] { new UpgradeGrant("Map", 1), new UpgradeGrant("Health", 3) }
        };
        if (!BaseUpgradeMeetsOrExceedsRoleTarget(StageRole.Tracker, tracker)) throw new Exception("Tracker equality");
        tracker.Bases[0].Level = 0;
        if (BaseUpgradeMeetsOrExceedsRoleTarget(StageRole.Tracker, tracker)) throw new Exception("Tracker below targets");
        tracker.Bases[1].Level = 3;
        if (!BaseUpgradeMeetsOrExceedsRoleTarget(StageRole.Tracker, tracker)) throw new Exception("Any matching target");
        if (BaseUpgradeMeetsOrExceedsRoleTarget(StageRole.Mage, tracker)) throw new Exception("Non-static role scope");
        tracker.Bases = new[] { new UpgradeGrant("Unrelated", 9999) };
        if (BaseUpgradeMeetsOrExceedsRoleTarget(StageRole.Tracker, tracker)) throw new Exception("Unrelated upgrade");
        var runner = new StageRolesConfig {
            Bases = new[] { new UpgradeGrant("Speed", 200), new UpgradeGrant("Stamina", 46) },
            Targets = new[] { new UpgradeGrant("Speed", 6), new UpgradeGrant("Stamina", 46) }
        };
        if (!BaseUpgradeMeetsOrExceedsRoleTarget(StageRole.Runner, runner)) throw new Exception("Capped Speed excludes Runner even when Stamina still grows");
        var runnerTargets = TargetUpgrades(StageRole.Runner, runner);
        if (runnerTargets[0].Level != 200 || runnerTargets[1].Level != 71) throw new Exception("Production Runner targets");
        runner.Bases[1].Level = 200;
        if (!BaseUpgradeMeetsOrExceedsRoleTarget(StageRole.Runner, runner)) throw new Exception("Both upgrades capped");
        var superbot = new StageRolesConfig {
            Bases = new[] { new UpgradeGrant("Health", 100) }, Targets = new[] { new UpgradeGrant("Health", 21) }
        };
        superbot.TankHealthMultiplier.Value = 1;
        if (TargetUpgrades(StageRole.Superbot, superbot)[0].Level != 100) throw new Exception("Zero bonus cannot lower Superbot below Base");
        count += 9;
        var lifter = new StageRolesConfig {
            Bases = new[] { new UpgradeGrant("Strength", 20) }, Targets = new[] { new UpgradeGrant("Strength", 25) }
        };
        RoleOverhaulRules.LifterPhysicsAvailable = true;
        // Execute actual target composition and eligibility over the whole Base range,
        // including old role inputs that the fixed Strength target must ignore.
        foreach (int minimum in new[] { 0, 25, 50, 200 })
        for (int baseline = 0; baseline <= 200; baseline++) {
            lifter.Bases[0].Level = baseline;
            lifter.Targets[0].Level = minimum;
            if (TargetUpgrades(StageRole.Lifter, lifter)[0].Level != 200)
                throw new Exception("Lifter must grant fixed Strength 200 regardless of Base/minimum");
            if (BaseUpgradeMeetsOrExceedsRoleTarget(StageRole.Lifter, lifter))
                throw new Exception("All Base levels 0-200 remain below the fixed effective target");
            if (TargetUpgrades(StageRole.Superbot, lifter)[0].Level != 200)
                throw new Exception("Superbot inherits fixed Strength 200");
            count += 3;
        }
        lifter.Targets[0].Level = 25;
        RoleOverhaulRules.LifterPhysicsAvailable = false;
        if (!BaseUpgradeMeetsOrExceedsRoleTarget(StageRole.Lifter, lifter))
            throw new Exception("Missing physics patch excludes ineffective random Lifter");
        RoleOverhaulRules.LifterPhysicsAvailable = true;
        count++;
        var tank = new StageRolesConfig {
            Bases = new[] { new UpgradeGrant("Health", 199) }, Targets = new[] { new UpgradeGrant("Health", 21) }
        };
        if (TargetUpgrades(StageRole.Tank, tank)[0].Level != 200 || BaseUpgradeMeetsOrExceedsRoleTarget(StageRole.Tank, tank))
            throw new Exception("Base below cap remains eligible even when role would reach cap");
        tank.Bases[0].Level = 200;
        if (!BaseUpgradeMeetsOrExceedsRoleTarget(StageRole.Tank, tank)) throw new Exception("Tank at 4100 HP excluded");
        tank.TankMaximumHealth.Value = 1000;
        tank.Targets[0].Level = 60;
        foreach (int level in new[] { 44, 45, 46 }) {
            tank.Bases[0].Level = level;
            if (BaseUpgradeMeetsOrExceedsRoleTarget(StageRole.Tank, tank) != (level >= 45))
                throw new Exception("Custom HP cap uses Base regardless of configured role minimum");
            if (TargetUpgrades(StageRole.Tank, tank)[0].Level != 60) throw new Exception("Forced assignment still preserves role minimum");
            count += 2;
        }
        runner.Bases[0].Level = 6; runner.Bases[1].Level = 200;
        if (!BaseUpgradeMeetsOrExceedsRoleTarget(StageRole.Runner, runner)) throw new Exception("Capped Stamina alone excludes Runner");
        runner.Bases[1].Level = 46;
        if (BaseUpgradeMeetsOrExceedsRoleTarget(StageRole.Runner, runner)) throw new Exception("Runner below both caps still grows");
        runner.RunnerMaximumSpeed.Value = 11;
        if (!BaseUpgradeMeetsOrExceedsRoleTarget(StageRole.Runner, runner)) throw new Exception("Custom Speed cap equality");
        runner.RunnerMaximumSpeed.Value = 12; runner.RunnerMaximumStamina.Value = 500;
        if (!BaseUpgradeMeetsOrExceedsRoleTarget(StageRole.Runner, runner)) throw new Exception("Custom Stamina cap equality");
        runner.RunnerMaximumStamina.Value = 501;
        if (BaseUpgradeMeetsOrExceedsRoleTarget(StageRole.Runner, runner)) throw new Exception("Below both custom caps with Speed gain stays eligible");
        count += 7;
        foreach (string command in new[] { "Health", "Speed", "Stamina", "Strength" })
        foreach (int level in System.Linq.Enumerable.Range(0, 201)) {
            double value = command == "Strength"
                ? Math.Max(RoleOverhaulRules.EffectiveGrabStrength(level, true), RoleOverhaulRules.EffectiveGrabStrength(level, false))
                : RoleOverhaulRules.UpgradeValue(command, level);
            double cap = RoleOverhaulRules.MaximumValue(command);
            if (RoleOverhaulRules.ReachesMaximum(command, level, cap) != (value >= cap)) throw new Exception("Full cap over all 201 levels");
            if (command == "Strength" && RoleOverhaulRules.ReachesMaximum(command, level, 6)) throw new Exception("Rounded Strength cap 6 is unattainable through level 200");
            count++;
        }
        Console.WriteLine("PASS: " + count + " production eligibility and upgrade-target checks (stubbed base/role inputs).");
    }
}
'@
Add-Type -TypeDefinition $testSource.Replace('__METHOD__', $methodSource).Replace('__TARGETS__', $targetMatch.Value).Replace('__RULES__', $rules)
[EligibilityChecks]::Run()
