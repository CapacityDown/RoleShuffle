using System.Text.Json;
using REPOJP.StageRoles;

int checks = 0;
void Check(bool value, string reason) { if (!value) throw new Exception(reason); checks++; }
const string a = "player-a", b = "player-b", late = "late-player";
var config = StageRolesPlugin.Instance.Settings;
void Reset()
{
    StatsManager.instance = new();
    config.Enabled.Value = true;
    config.KeepUpgradeItems.Value = false;
    config.UpgradeItemScope.Value = UpgradeItemScope.Player;
    SemiFunc.Authority = true;
    GameSaveState.CurrentName = "save-a";
    GameSaveState.CanSave = true;
    GameDirector.instance.PlayerList = new() { new(a, 1), new(b, 2) };
}
int Current(string player, string command)
{
    UpgradeService.TryGetLevels(player, out var levels);
    return levels.GetValueOrDefault("playerUpgrade" + command);
}
int Target(string player, string command, int baseline, StageRole? role = null) =>
    UpgradeItemRetention.Apply(new[] { new UpgradeGrant(command, "playerUpgrade" + command, baseline) }, config, player, role)[0].Level;
{
    foreach (string command in RoleUpgradeScaling.Definitions.Select(d => d.Name).Append("Throw"))
    {
        Reset();
        new ItemUpgrade(1, command).PlayerUpgrade();
        Check(Current(a, command) == 1 && StatsManager.instance.runStats.Count == 0, "Default-off leaves native consumption alone: " + command);
        config.KeepUpgradeItems.Value = true;
        var item = new ItemUpgrade(1, command);
        item.PlayerUpgrade();
        int gain = 1;
        Check(UpgradeItemRetention.Get(a, "playerUpgrade" + command) == gain, "Only actual positive native delta is retained: " + command);
        Check(Current(b, command) == 0, "Player scope never changes teammates: " + command);
        int before = StatsManager.instance.runStats.Values.Sum();
        item.PlayerUpgrade();
        Check(StatsManager.instance.runStats.Values.Sum() == before, "Already-used object cannot record twice: " + command);
        UpgradeService.AddLevelsHostAuthoritative(a, command, 10);
        Check(StatsManager.instance.runStats.Values.Sum() == before, "Role/debug/King-style grants are not item use: " + command);
    }
    Reset();
    config.KeepUpgradeItems.Value = true;
    new ItemUpgrade(1, "Strength", 3).PlayerUpgrade();
    Check(Target(a, "Strength", 2) == 5 && Target(b, "Strength", 2) == 2, "Saved personal bonus adds to later base targets");
    Check(Target(a, "Strength", 6, StageRole.Runner) == 9, "Bonus survives role switch");
    Check(Target(a, "Strength", 200, StageRole.Lifter) == 200 && Target(a, "Strength", 200, StageRole.Superbot) == 200, "Fixed Lifter display wins, saved levels retained for other roles");
    config.UpgradeItemScope.Value = UpgradeItemScope.AllPlayers;
    new ItemUpgrade(2, "Strength", 2).PlayerUpgrade();
    Check(Current(a, "Strength") == 5 && Current(b, "Strength") == 2, "Shared use adds once to consumer and every teammate");
    Check(Target(a, "Strength", 1) == 6 && Target(b, "Strength", 1) == 3 && Target(late, "Strength", 1) == 3, "Personal and shared amounts coexist; late joiners receive shared amount");
    config.UpgradeItemScope.Value = UpgradeItemScope.Player;
    new ItemUpgrade(2, "Health").PlayerUpgrade();
    Check(Target(a, "Health", 1) == 1 && Target(b, "Health", 1) == 2 && Target(a, "Strength", 1) == 6, "Scope changes affect only future uses");
    var snapshot = JsonSerializer.Serialize(StatsManager.instance.runStats);
    StatsManager.instance = new() { runStats = JsonSerializer.Deserialize<Dictionary<string, int>>(snapshot)! };
    Check(Target(a, "Strength", 1) == 6 && Target(b, "Health", 1) == 2, "Counts survive clearing process state and reloading the save");
    config.KeepUpgradeItems.Value = false;
    Check(Target(a, "Strength", 1) == 1 && JsonSerializer.Serialize(StatsManager.instance.runStats) == snapshot, "Off excludes saved bonuses without deleting them");
    config.KeepUpgradeItems.Value = true;
    Check(Target(a, "Strength", 1) == 6, "Re-enable restores recorded amounts");
    StatsManager.instance = new();
    Check(Target(a, "Strength", 1) == 1, "New or different saves do not inherit bonuses");
    Reset(); config.KeepUpgradeItems.Value = true;
    foreach (string command in new[] { "Launch", "TumbleClimb", "TumbleWings" })
    {
        new ItemUpgrade(1, command, 2).PlayerUpgrade();
        Check(Target(a, command, 0, StageRole.Rammer) == 0 && Target(a, command, 0, StageRole.Runner) == 2, "Rammer suppresses but does not erase consumed levels: " + command);
    }
    new ItemUpgrade(1, "Health").PlayerUpgrade();
    Check(Target(a, "Health", int.MaxValue) == int.MaxValue, "Retained target addition does not overflow");
    new ItemUpgrade(1, "MapPlayerCount").PlayerUpgrade();
    Check(Target(a, "MapPlayerCount", 1) == 2, "Actual Map item increments persist above the configurable Base limit");
    Reset(); config.KeepUpgradeItems.Value = true; config.UpgradeItemScope.Value = UpgradeItemScope.AllPlayers;
    var repeated = new ItemUpgrade(1, "Throw"); repeated.PlayerUpgrade();
    Check(Current(a, "Throw") == 1 && Current(b, "Throw") == 1, "Throw is shared without double-applying consumer");
    repeated.PlayerUpgrade(); UpgradeItemRetentionPatch.ApplyPendingThrow(a); UpgradeItemRetentionPatch.ApplyPendingThrow(b);
    Check(Current(a, "Throw") == 1 && Current(b, "Throw") == 1, "Throw is not duplicated by replay/reconnection");
    UpgradeItemRetentionPatch.ApplyPendingThrow(late);
    Check(Current(late, "Throw") == 1, "A later player receives historical shared Throw");
    GameDirector.instance.PlayerList.Add(new(late, 3));
    new ItemUpgrade(2, "Throw").PlayerUpgrade();
    Check(Current(a, "Throw") == 2 && Current(b, "Throw") == 2 && Current(late, "Throw") == 2, "Further shared Throw stacks exactly once");
    Reset(); config.KeepUpgradeItems.Value = true;
    new ItemUpgrade(1, "Speed") { Toggle = new() { playerTogglePhotonID = 1, toggleState = false } }.PlayerUpgrade();
    new ItemUpgrade(1, "Speed") { isPlayerUpgrade = false }.PlayerUpgrade();
    try { new ItemUpgrade(1, "Speed") { Fail = true }.PlayerUpgrade(); } catch (InvalidOperationException) { }
    Check(StatsManager.instance.runStats.Count == 0, "Invalid, untoggled and failed consumption never records a bonus");
    SemiFunc.Authority = false; new ItemUpgrade(1, "Health").PlayerUpgrade();
    Check(StatsManager.instance.runStats.Count == 0, "Guests cannot record or broadcast bonuses");
    SemiFunc.Authority = true;
    new ItemUpgrade(1, "Health") { AfterEffect = () => GameSaveState.CurrentName = "other-save" }.PlayerUpgrade();
    Check(StatsManager.instance.runStats.Count == 0, "A save switch invalidates an in-flight capture");
    Reset(); config.KeepUpgradeItems.Value = true; StatsManager.instance.FailSave = true;
    new ItemUpgrade(1, "Health").PlayerUpgrade();
    Check(Target(a, "Health", 1) == 2, "A failed disk write retains consumed amounts in memory for next save");
    Reset(); config.KeepUpgradeItems.Value = true;
    new ItemUpgrade(1, "Health") { AfterEffect = () => config.UpgradeItemScope.Value = UpgradeItemScope.AllPlayers }.PlayerUpgrade();
    Check(Target(a, "Health", 1) == 2 && Target(b, "Health", 1) == 1, "Scope is captured when use starts");
    Console.WriteLine($"PASS: {checks} upgrade-item retention checks, including production capture/postfix with simulated native consumption.");
}
