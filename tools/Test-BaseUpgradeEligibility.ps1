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
    public Entry<bool> OverhaulEnabled = new(false);
    public Entry<int> TankBaseBonus = new(5);
    public Entry<int> RunnerSpeedBaseBonus = new(2);
    public Entry<int> RunnerStaminaBaseBonus = new(10);
    public Entry<int> LifterBaseBonus = new(5);
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
                    Targets = new[] { new UpgradeGrant("Health", target) }
                };
                if (BaseUpgradeMeetsOrExceedsRoleTarget(StageRole.Tank, config) != (baseline >= target))
                    throw new Exception("Boundary comparison failed");
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
        runner.OverhaulEnabled.Value = true;
        if (BaseUpgradeMeetsOrExceedsRoleTarget(StageRole.Runner, runner)) throw new Exception("Runner must remain eligible when Stamina still grows");
        var runnerTargets = TargetUpgrades(StageRole.Runner, runner);
        if (runnerTargets[0].Level != 200 || runnerTargets[1].Level != 56) throw new Exception("Production Runner targets");
        runner.Bases[1].Level = 200;
        if (!BaseUpgradeMeetsOrExceedsRoleTarget(StageRole.Runner, runner)) throw new Exception("Both upgrades capped");
        runner.OverhaulEnabled.Value = false;
        if (!BaseUpgradeMeetsOrExceedsRoleTarget(StageRole.Runner, runner)) throw new Exception("Legacy eligibility remains intact");
        var superbot = new StageRolesConfig {
            Bases = new[] { new UpgradeGrant("Health", 100) }, Targets = new[] { new UpgradeGrant("Health", 21) }
        };
        superbot.OverhaulEnabled.Value = true;
        superbot.TankBaseBonus.Value = 0;
        if (TargetUpgrades(StageRole.Superbot, superbot)[0].Level != 100) throw new Exception("Zero bonus cannot lower Superbot below Base");
        Console.WriteLine("PASS: " + (count + 10) + " production eligibility and upgrade-target checks (stubbed base/role inputs).");
    }
}
'@
Add-Type -TypeDefinition $testSource.Replace('__METHOD__', $methodSource).Replace('__TARGETS__', $targetMatch.Value).Replace('__RULES__', $rules)
[EligibilityChecks]::Run()
