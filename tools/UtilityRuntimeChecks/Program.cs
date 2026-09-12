using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Photon.Pun;
using REPOJP.StageRoles;
using UnityEngine;

int checks = 0;
void Check(bool condition, string name) { if (!condition) throw new Exception(name); checks++; }
var config = new StageRolesConfig();
var sync = new RoleSyncStatus();
sync.Initialize(config);
void Tick(float time, int timestamp)
{
    Time.unscaledTime = time; PhotonNetwork.ServerTimestamp = timestamp;
    typeof(RoleSyncStatus).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(sync, null);
}
Check(sync.Describe(false).StartsWith("Local"), "Offline status is local");
var room = new Room(); PhotonNetwork.CurrentRoom = room;
Tick(1, 1000);
Check(room.LastSent.Count == 6, "Joining host publishes complete snapshot and stamp");
Check((string)room.LastSent["RS.RoleMap"] == "", "Empty assignment list is explicitly synchronized");
Tick(6, 6000);
Check(room.LastSent.Count == 1 && room.LastSent.ContainsKey("RS.Sync.Status"), "Unchanged heartbeat sends only metadata");
int sends = room.Sends;
Tick(7, 7000);
Check(room.Sends == sends, "No per-frame snapshot publishing");

var guest = new Player { ActorNumber = 2, NickName = "Secret Guest", UserId = "private-account-id" };
PhotonNetwork.PlayerList = new[] { PhotonNetwork.LocalPlayer, guest };
PhotonNetwork.LocalPlayer = guest; PhotonNetwork.IsMasterClient = false;
Check(sync.Describe(false).StartsWith("Synchronized"), "Guest confirms matching data");
room.CustomProperties["RS.BaseUpgrades"] = "not-arrived-yet";
Check(sync.Describe(false).StartsWith("Receiving"), "Partially updated snapshot is not synchronized");
room.CustomProperties["RS.BaseUpgrades"] = "base";
sync.RequestRefresh();
Check(guest.CustomProperties.ContainsKey("RS.Sync.Request") && sync.Describe(false).StartsWith("Refresh requested"), "Guest requests refresh and waits");
PhotonNetwork.LocalPlayer = PhotonNetwork.MasterClient!; PhotonNetwork.IsMasterClient = true;
Tick(10, 10000);
Check(room.LastSent.Count == 6, "Host responds to request with a full snapshot");
PhotonNetwork.LocalPlayer = guest; PhotonNetwork.IsMasterClient = false;
Check(sync.Describe(false).StartsWith("Synchronized"), "Fresh matching snapshot acknowledges request");
PhotonNetwork.ServerTimestamp = 30001;
Check(sync.Describe(false).StartsWith("Host updates delayed"), "Missing heartbeat reports delay");
PhotonNetwork.ServerTimestamp = 10001;
string originalStamp = (string)room.CustomProperties["RS.Sync.Status"];
room.CustomProperties["RS.Sync.Status"] = originalStamp.Replace("4.4.6", "4.4.7");
Check(sync.Describe(false).Contains("Version differs"), "Different version remains visible");
room.CustomProperties.Remove("RS.Sync.Status");
Check(sync.Describe(false).Contains("older version"), "Older host is identified without false synchronization");
room.CustomProperties["RS.Sync.Status"] = originalStamp;
PhotonNetwork.MasterClient = new Player { ActorNumber = 3 };
Tick(11, 11000);
Check(sync.Describe(false).Contains("new host"), "Host migration rejects previous host stamp");
Tick(32, 32000);
Check(sync.Describe(false).Contains("New host data unavailable"), "Missing replacement host status stops waiting indefinitely");
PhotonNetwork.CurrentRoom = new Room();
Tick(33, 33000);
Check(sync.Describe(false).StartsWith("Waiting for host data"), "Rejoining clears previous room status");
Tick(54, 54000);
Check(sync.Describe(false).StartsWith("Host data unavailable"), "Vanilla host does not look synchronized");

PhotonNetwork.CurrentRoom = room; PhotonNetwork.IsMasterClient = true; PhotonNetwork.LocalPlayer = PhotonNetwork.MasterClient;
BaseUpgradeHistory.Record("AllUpgrades", -1, new int[12], config);
Check(BaseUpgradeHistory.Read().Count == 1 && BaseUpgradeHistory.Read()[0].Upgrade == -1, "Host records history with stable All Upgrades identity");
string history = BaseUpgradeHistory.LocalPayload;
PhotonNetwork.IsMasterClient = false;
Check(BaseUpgradeHistory.Read().Count == 1 && BaseUpgradeHistory.CurrentPayload == history, "Guest history uses published host data");
BaseUpgradeHistory.Record("Health", 1, new int[12], config);
Check(BaseUpgradeHistory.Local.Count == 1, "Guests cannot append authoritative history");
PhotonNetwork.CurrentRoom = new Room();
Check(BaseUpgradeHistory.Read().Count == 0, "Remote history does not leak into a different room");

PhotonNetwork.CurrentRoom = room;
var savedStats = StatsManager.instance!.runStats;
var savedStatsSnapshot = savedStats.ToArray();
string? actualPayload = BaseUpgradeHistory.CurrentPayload;
int[] actualLevels = BaseUpgradeHistory.Levels(config);
int sendsBeforePreview = room.Sends;
Check(BaseUpgradeHistory.TrySetDisplayPreview(Array.Empty<string>(), out _) && BaseUpgradeHistory.IsDisplayPreview, "No-argument command enables a local preview");
var samples = BaseUpgradeHistory.ReadForDisplay();
Check(samples.Count == 50 && samples.First().Number == 50 && samples.Last().Number == 1, "Default preview shows 50 draws newest first");
Check(DrawHistoryStore.TryParse(BaseUpgradeHistory.DisplayPayload!, out var parsedSamples) && parsedSamples.Count == 50, "Samples pass the real history parser");
Check(samples.Select(r => r.Upgrade).Distinct().Count() == 13, "Preview covers all 12 upgrades and All Upgrades");
Check(samples.Any(r => r.Delta > 0 && !r.Before.SequenceEqual(r.After)) && samples.Any(r => r.Delta < 0 && !r.Before.SequenceEqual(r.After)), "Preview contains positive and negative changes");
Check(samples.Any(r => r.Delta == 0) && samples.Any(r => r.Delta > 0 && r.Before.SequenceEqual(r.After)), "Preview contains zero draws and capped draws");
Check(samples.All(r => r.Before[10] <= 1 && r.After[10] <= 1), "Preview respects the MapPlayerCount cap");
Check(samples.All(r => !string.IsNullOrWhiteSpace(BaseUpgradeHistory.Describe(r, false))), "Every sample is renderable by the actual history formatter");
Check(BaseUpgradeHistory.CurrentPayload == actualPayload && BaseUpgradeHistory.Read().Count == 1, "Guest preview leaves actual history intact");
Check(savedStats.SequenceEqual(savedStatsSnapshot) && BaseUpgradeHistory.Levels(config).SequenceEqual(actualLevels) && room.Sends == sendsBeforePreview, "Preview writes no save data, upgrades or room properties");
string? previewPayload = BaseUpgradeHistory.DisplayPayload;
foreach (string[] invalid in new[] { new[] { "-1" }, new[] { "51" }, new[] { "abc" }, new[] { "99999999999999" }, new[] { "1", "2" } })
    Check(!BaseUpgradeHistory.TrySetDisplayPreview(invalid, out _) && BaseUpgradeHistory.DisplayPayload == previewPayload, "Invalid preview arguments preserve the current display");
Check(BaseUpgradeHistory.TrySetDisplayPreview(new[] { "0" }, out _) && BaseUpgradeHistory.IsDisplayPreview && BaseUpgradeHistory.ReadForDisplay().Count == 0, "Zero samples exercises the empty history view");
Check(BaseUpgradeHistory.TrySetDisplayPreview(new[] { "ReSeT" }, out _) && !BaseUpgradeHistory.IsDisplayPreview && BaseUpgradeHistory.DisplayPayload == actualPayload, "Reset restores actual history");
Check(BaseUpgradeHistory.TrySetDisplayPreview(new[] { "1" }, out _) && BaseUpgradeHistory.ReadForDisplay().Count == 1, "Explicit sample count is honored");
PhotonNetwork.CurrentRoom = new Room();
Check(!BaseUpgradeHistory.IsDisplayPreview && BaseUpgradeHistory.ReadForDisplay().Count == 0, "Changing rooms clears the local preview");
PhotonNetwork.CurrentRoom = room;
BaseUpgradeHistory.TrySetDisplayPreview(new[] { "10" }, out _);
StatsManager.instance.runStats = new();
Check(!BaseUpgradeHistory.IsDisplayPreview, "Changing saves clears the local preview");
StatsManager.instance.runStats = savedStats;
PhotonNetwork.IsMasterClient = true;
BaseUpgradeHistory.TrySetDisplayPreview(Array.Empty<string>(), out _);
sync.PublishNow();
Check((string)room.CustomProperties[BaseUpgradeHistory.PropertyKey] == history && BaseUpgradeHistory.LocalPayload == history, "Host publication excludes sample history");

using var report = new RoleBugReport();
report.RememberPlayers();
for (int i = 0; i < 520; i++) report.LogEvent(report, new LogEventArgs { Data = "Log entry " + i });
report.LogEvent(report, new LogEventArgs { Data = "Secret Guest Private Player private-account-id private-room-name 76561198000000001 10.2.3.4 token=abc123" });
report.LogEvent(report, new LogEventArgs { Source = new LogSource { SourceName = "Other.Mod" }, Data = "Other mod private log" });
report.LogEvent(report, new LogEventArgs { Source = new LogSource { SourceName = "Unity Log" }, Data = "NullReferenceException at REPOJP.StageRoles.RoleHud.Update()" });
var settings = new ConfigFile { [new("HUD", "RoleDisplay")] = new("NameOnly"), [new("Test", "LongValue")] = new(new string('a', 10000)) };
report.Create(settings);
Check(File.Exists(report.LatestPath) && File.ReadAllText(report.LatestPath) == report.LatestText, "Report is written to a local UTF-8 file");
Check(report.LatestText.Contains("NullReferenceException at REPOJP.StageRoles.RoleHud.Update()"), "Unity exceptions from this mod are included");
Check(report.LatestText.Contains("Game: 1.2.3.4") && report.LatestText.Contains("Test.Mod 10.2.3.4"), "Four-part versions survive masking");
Check(!report.LatestText.Contains("01 10.2.3.4") && report.LatestText.Contains("[IP]"), "Same number used as a logged IP is masked");
foreach (string secret in new[] { "Secret Guest", "Private Player", "private-account-id", "private-room-name", "76561198000000001", "abc123", "Other mod private log" })
    Check(!report.LatestText.Contains(secret), "Report omits private values and unrelated logs");
string logs = report.LatestText.Split("## Recent RoleShuffle logs", 2)[1];
Check(logs.Contains("up to 500 entries") && logs.Split('\n').Count(line => line.StartsWith("[20")) == 500, "Report contains the latest 500 matching log events");
Check(!logs.Contains("Log entry 21\r") && logs.Contains("Log entry 22\r") && logs.Contains("Log entry 519\r"), "500-event ring evicts only the oldest matching entries");
Check(logs.IndexOf("Log entry 519", StringComparison.Ordinal) < logs.IndexOf("Log entry 22", StringComparison.Ordinal), "Logs are shown newest first");
Check(!report.LatestText.Contains("#50  Level 50") && report.LatestText.Contains(BaseUpgradeHistory.Describe(BaseUpgradeHistory.Read()[0], false)), "Reports contain actual draws while the menu shows samples");
BaseUpgradeHistory.ClearDisplayPreview();
Check(report.LatestText.Contains("HUD.RoleDisplay = NameOnly") && report.LatestText.Contains("[TRUNCATED]"), "Settings remain useful with oversized values bounded");
string firstText = report.LatestText, firstPath = report.LatestPath;
settings[new("HUD", "RoleDisplay")] = new("IconAndName");
report.LogEvent(report, new LogEventArgs { Data = "New diagnostics after the first report" });
report.Create(settings);
Check(report.LatestPath != firstPath && File.Exists(report.LatestPath), "Each request generates a separate report file");
Check(report.LatestText.Contains("HUD.RoleDisplay = IconAndName") && report.LatestText.Contains("New diagnostics after the first report"), "Repeated requests capture current settings and logs");
Check(File.ReadAllText(firstPath) == firstText, "Regeneration preserves the previously opened report file");
Check(File.ReadAllText(report.LatestPath) == report.LatestText, "Latest clipboard source matches the newly saved report");
using (var largeReport = new RoleBugReport())
{
    for (int i = 0; i < 501; i++)
        largeReport.LogEvent(largeReport, new LogEventArgs { Data = $"LONG-{i:D3} Secret Guest token=hidden " + new string('x', 3000) });
    largeReport.Create(settings);
    string longLogs = largeReport.LatestText.Split("## Recent RoleShuffle logs", 2)[1];
    Check(longLogs.Length > 900000 && longLogs.Split('\n').Count(line => line.StartsWith("[20")) == 500, "Long messages still retain 500 entries beyond the metadata size limit");
    Check(longLogs.Contains("LONG-001") && longLogs.Contains("LONG-500") && !longLogs.Contains("LONG-000"), "Long-log ring retains the exact newest window");
    Check(!longLogs.Contains("Secret Guest") && !longLogs.Contains("token=hidden"), "All 500 long entries are redacted");
    Check(longLogs.Split('\n').All(line => line.TrimEnd('\r').Length <= 2065), "Individual long entries remain bounded after redaction");
    Check(largeReport.LatestText.Contains("## RoleShuffle configuration") && largeReport.LatestText.Contains("## Recent Base Upgrade draws"), "Large log sections preserve report metadata");
    Check(File.ReadAllText(largeReport.LatestPath) == largeReport.LatestText, "The full large report is saved as UTF-8");
}
string savedText = report.LatestText, savedPath = report.LatestPath;
Paths.BepInExRootPath = savedPath;
bool failed = false;
try { report.Create(settings); } catch (IOException) { failed = true; }
Check(failed && report.LatestText == savedText && report.LatestPath == savedPath, "Failed file write keeps previous report consistent");
Console.WriteLine($"PASS: {checks} runtime utility checks using deterministic game/network stand-ins and real report file I/O.");
