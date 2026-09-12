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
using var report = new RoleBugReport();
report.RememberPlayers();
for (int i = 0; i < 120; i++) report.LogEvent(report, new LogEventArgs { Data = "Log entry " + i });
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
Check(!report.LatestText.Contains("Log entry 0\n") && report.LatestText.Contains("Log entry 119"), "Log ring retains recent entries only");
Check(report.LatestText.Contains("HUD.RoleDisplay = NameOnly") && report.LatestText.Contains("[TRUNCATED]"), "Settings remain useful with oversized values bounded");
string firstText = report.LatestText, firstPath = report.LatestPath;
settings[new("HUD", "RoleDisplay")] = new("IconAndName");
report.LogEvent(report, new LogEventArgs { Data = "New diagnostics after the first report" });
report.Create(settings);
Check(report.LatestPath != firstPath && File.Exists(report.LatestPath), "Each request generates a separate report file");
Check(report.LatestText.Contains("HUD.RoleDisplay = IconAndName") && report.LatestText.Contains("New diagnostics after the first report"), "Repeated requests capture current settings and logs");
Check(File.ReadAllText(firstPath) == firstText, "Regeneration preserves the previously opened report file");
Check(File.ReadAllText(report.LatestPath) == report.LatestText, "Latest clipboard source matches the newly saved report");
string savedText = report.LatestText, savedPath = report.LatestPath;
Paths.BepInExRootPath = savedPath;
bool failed = false;
try { report.Create(settings); } catch (IOException) { failed = true; }
Check(failed && report.LatestText == savedText && report.LatestPath == savedPath, "Failed file write keeps previous report consistent");
Console.WriteLine($"PASS: {checks} runtime utility checks using deterministic game/network stand-ins and real report file I/O.");
