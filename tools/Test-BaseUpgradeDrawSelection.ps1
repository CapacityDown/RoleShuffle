$ErrorActionPreference = 'Stop'
$source = Get-Content -LiteralPath (Join-Path $PSScriptRoot '../BaseUpgradeDrawRuntime.cs') -Raw
$names = 'BuildEligibleList', 'BuildPoolForDelta', 'CanApplyDelta', 'EffectiveUpgradeWeight', 'ParseDeltaRules', 'ApplyResults', 'IsAllUpgrades', 'AbsoluteMaximumLevel', 'DrawMaximumLevel'
$methods = foreach ($name in $names) {
    $match = [regex]::Match($source, "(?ms)^    private (?:static )?[^\r\n]+\b$name\(.*?(?=^    (?:private|internal|public) )")
    if (-not $match.Success) { throw "Production method not found: $name" }
    $match.Value
}
$weightSource = (Get-Content -LiteralPath (Join-Path $PSScriptRoot '../TruckDrawWeight.cs') -Raw).Replace('using System;', '').Replace('namespace REPOJP.StageRoles;', '')
$harness = @'
using System;
using System.Collections.Generic;
using System.Linq;

public readonly record struct UpgradeGrant(string CommandName, string DictionaryName, int Level);
public class Entry<T>(T value) { public T Value = value; }
public class StageRolesConfig {
    public Dictionary<string, bool> Enabled = new();
    public Dictionary<string, int> Weights = new();
    public UpgradeGrant[] Bases = Array.Empty<UpgradeGrant>();
    public UpgradeGrant[] Configured = Array.Empty<UpgradeGrant>();
    public Entry<string> TruckUpgradeDrawDeltaWeights = new("-1:10,0:15,1:60,2:15");
    public Entry<int> TruckUpgradeDrawMaximumLevel = new(50);
    public Entry<float> TruckUpgradeDrawCappedWeightMultiplier = new(0.25f);
    public Entry<float> TruckUpgradeDrawWeightFalloffExponent = new(2f);
    public bool BaseUpgradeDrawIsEnabled(string name) => Enabled.GetValueOrDefault(name);
    public int TruckUpgradeDrawWeight(string name) => Weights.GetValueOrDefault(name, 10);
}
public static class RoleCatalog {
    public static IReadOnlyList<UpgradeGrant> BaseUpgrades(StageRolesConfig c) => c.Bases;
    public static IReadOnlyList<UpgradeGrant> ConfiguredBaseUpgrades(StageRolesConfig c) => c.Configured;
}
public static class UpgradeService { public static bool Ready = true; }
public static class GameDirector { public static object instance = new(); }
public class Logger { public void LogInfo(string s) {} public void LogWarning(string s) {} }
public static class StageRolesPlugin { public static Logger ModLogger = new(); }
public static class BaseUpgradeBonusStore {
    public static List<(string Name, int Level, int Configured, int Delta)> Calls = new();
    public static bool ApplyEffectiveDelta(string name, int level, int configured, int delta, int maximum) {
        Calls.Add((name, level, configured, delta)); return true;
    }
}
public class DrawSelectionChecks {
    const int DefaultMaximumLevel = 200;
    const int MaximumConfiguredDelta = 200;
    const string DefaultDeltaWeights = "-1:10,0:15,1:60,2:15";
    static readonly UpgradeGrant AllUpgrades = new("AllUpgrades", "", 0);
    readonly List<UpgradeGrant> _allBaseUpgrades = new();
    readonly HashSet<string> _enabledDrawNames = new(StringComparer.Ordinal);
    bool _allUpgradesEnabledForDraw;
    readonly List<UpgradeGrant> _individualCandidates = new();
    readonly List<UpgradeGrant> _displayCandidates = new();
    readonly List<WeightedDelta> _deltaRules = new();
    readonly List<UpgradeGrant> _selected = new();
    StageRolesConfig _config = new();
    int _selectedDelta;
    readonly record struct AppliedUpgrade(UpgradeGrant Upgrade, int Delta);
    readonly record struct WeightedDelta(int Delta, int Weight);
    static void ApplyUpgradeChangesToPlayers(IReadOnlyList<AppliedUpgrade> values) {}
    static string DisplayName(UpgradeGrant value) => value.CommandName;
    string ResultNames() => "test";
__METHODS__
    static int checks;
    static void Check(bool value, string message) { checks++; if (!value) throw new Exception(message); }
    public static void Run() {
        var draw = new DrawSelectionChecks();
        var c = draw._config;
        string[] names = { "Health", "Stamina", "ExtraJump", "Speed", "Strength", "Range", "Launch", "TumbleClimb", "TumbleWings", "CrouchRest", "MapPlayerCount", "DeathHeadBattery" };
        c.Bases = names.Select(n => new UpgradeGrant(n, "playerUpgrade" + n, 0)).ToArray();
        c.Configured = c.Bases;
        // Exhaust every switch combination, including All Upgrades without a target.
        for (int mask = 0; mask < 8192; mask++) {
            for (int i = 0; i < names.Length; i++) c.Enabled[names[i]] = (mask & (1 << i)) != 0;
            c.Enabled["AllUpgrades"] = (mask & 4096) != 0;
            draw.BuildEligibleList();
            var pool = draw.BuildPoolForDelta(1);
            for (int i = 0; i < names.Length; i++)
                Check(pool.Any(u => u.CommandName == names[i]) == c.Enabled[names[i]], "Individual selection mask " + mask);
            bool expectedAll = c.Enabled["AllUpgrades"] && (mask & 4095) != 0;
            Check(pool.Contains(AllUpgrades) == expectedAll, "Combined selection mask " + mask);
            Check(draw._displayCandidates.Contains(AllUpgrades) == expectedAll, "Animation selection mask " + mask);
            Check(!draw.BuildPoolForDelta(0).Contains(AllUpgrades), "No combined zero result");
            Check(!draw.BuildPoolForDelta(-1).Contains(AllUpgrades), "No combined negative result");
        }
        c.Enabled.Clear();
        c.Enabled["Health"] = c.Enabled["AllUpgrades"] = true;
        c.Weights["Health"] = 0;
        draw.BuildEligibleList();
        Check(draw.BuildPoolForDelta(1).SequenceEqual(new[] { AllUpgrades }), "Weight zero remains eligible through combined draw");
        // The captured selection applies throughout the animation; changes affect the next draw.
        c.Enabled["Health"] = false;
        c.Enabled["Stamina"] = true;
        c.Enabled["AllUpgrades"] = false;
        draw._selected.Add(AllUpgrades);
        draw._selectedDelta = 1;
        BaseUpgradeBonusStore.Calls.Clear();
        draw.ApplyResults();
        Check(BaseUpgradeBonusStore.Calls.Count == 1 && BaseUpgradeBonusStore.Calls[0].Name == "playerUpgradeHealth", "Frozen combined targets");
        Check(c.Bases.All(u => u.Level == 0) && c.Configured.All(u => u.Level == 0), "Configured targets untouched");
        draw.BuildEligibleList();
        Check(draw.BuildPoolForDelta(1).Select(u => u.CommandName).SequenceEqual(new[] { "Stamina" }), "New selection on next draw");
        // Existing bonus levels for disabled types must never be revoked.
        c.Bases = new[] { new UpgradeGrant("Health", "playerUpgradeHealth", 7), new UpgradeGrant("Stamina", "playerUpgradeStamina", 2) };
        c.Configured = new[] { new UpgradeGrant("Health", "playerUpgradeHealth", 5), new UpgradeGrant("Stamina", "playerUpgradeStamina", 1) };
        c.Enabled["AllUpgrades"] = true;
        draw.BuildEligibleList();
        BaseUpgradeBonusStore.Calls.Clear();
        draw.ApplyResults();
        Check(BaseUpgradeBonusStore.Calls.SequenceEqual(new[] { ("playerUpgradeStamina", 2, 1, 1) }), "Disabled acquired levels preserved");
        // Switching off an already selected individual does not cancel this draw.
        draw._selected.Clear(); draw._selected.Add(c.Bases[1]);
        c.Enabled["Stamina"] = false;
        BaseUpgradeBonusStore.Calls.Clear(); draw.ApplyResults();
        Check(BaseUpgradeBonusStore.Calls.Count == 1, "Frozen individual result");
        var previous = c.Bases;
        c.Bases = new[] { previous[0], new UpgradeGrant("Stamina", "playerUpgradeStamina", 5) };
        BaseUpgradeBonusStore.Calls.Clear(); draw.ApplyResults();
        Check(BaseUpgradeBonusStore.Calls.Single().Level == 5, "Individual draw uses the level after an in-flight manual adjustment");
        c.TruckUpgradeDrawMaximumLevel.Value = 4;
        BaseUpgradeBonusStore.Calls.Clear(); draw.ApplyResults();
        Check(BaseUpgradeBonusStore.Calls.Count == 0, "A positive result never lowers a manually increased level above the draw cap");
        c.Bases = previous;
        draw._selected.Clear(); draw._selected.Add(previous[1]);
        c.Enabled["Stamina"] = true;
        c.TruckUpgradeDrawMaximumLevel.Value = 2;
        draw.BuildEligibleList();
        Check(draw.BuildPoolForDelta(1).Count == 0, "Positive cap respected");
        Check(draw.BuildPoolForDelta(-1).Count == 1, "Negative draw remains eligible at cap");
        Check(draw.BuildPoolForDelta(-3).Count == 0, "Negative draw cannot pass zero");
        Check(draw.DrawMaximumLevel("playerUpgradeMapPlayerCount") == 1, "Map hard cap respected");
        draw._selectedDelta = 0;
        BaseUpgradeBonusStore.Calls.Clear(); draw.ApplyResults();
        Check(BaseUpgradeBonusStore.Calls.Count == 0, "Zero result does not change bonuses");
        draw._selectedDelta = -1; draw.ApplyResults();
        Check(BaseUpgradeBonusStore.Calls.Single().Delta == -1, "Negative result applies expected delta");
        Console.WriteLine("PASS: " + checks + " production draw selection and application checks (game and bonus persistence stand-ins).");
    }
}
'@
Add-Type -TypeDefinition ($harness.Replace('__METHODS__', ($methods -join "`n")) + $weightSource)
[DrawSelectionChecks]::Run()
