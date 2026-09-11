$ErrorActionPreference = 'Stop'
$roleSource = Get-Content -LiteralPath (Join-Path $PSScriptRoot '../RoleModels.cs') -Raw
$match = [regex]::Match($roleSource, '(?s)    internal static bool BaseUpgradeMeetsOrExceedsRoleTarget\(.*?(?=    internal static bool InfluencerHasUpgradeBenefit)')
if (-not $match.Success) { throw 'Production eligibility method not found.' }
$methodSource = $match.Value.Replace('internal static bool', 'public static bool')
$testSource = @'
using System;
using System.Collections.Generic;
public enum StageRole { Tank, Runner, Jumper, Lifter, Launcher, Climber, Flyer, Tracker, Ghost, Mage }
public class UpgradeGrant {
    public string DictionaryName;
    public int Level;
    public UpgradeGrant(string name, int level) { DictionaryName = name; Level = level; }
}
public class StageRolesConfig {
    public UpgradeGrant[] Bases;
    public UpgradeGrant[] Targets;
}
public static class EligibilityChecks {
    static IReadOnlyList<UpgradeGrant> BaseUpgrades(StageRolesConfig config) { return config.Bases; }
    static IReadOnlyList<UpgradeGrant> RoleUpgrades(StageRole role, StageRolesConfig config) { return config.Targets; }
__METHOD__
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
        Console.WriteLine("PASS: " + (count + 5) + " production eligibility-method checks (stubbed base/role targets).");
    }
}
'@
Add-Type -TypeDefinition $testSource.Replace('__METHOD__', $methodSource)
[EligibilityChecks]::Run()
