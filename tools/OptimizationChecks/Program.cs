using System.Text;
using Photon.Pun;
using REPOJP.StageRoles;

int assertions = 0;
void Check(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
    assertions++;
}
string Encode(string text) => Convert.ToBase64String(Encoding.UTF8.GetBytes(text));
string ExpectedGuide(int value, bool legacy) => string.Join(";", RoleCatalog.AllRoles.Select(role =>
    legacy
        ? $"{(int)role}:{Encode(RoleGuideCatalog.Expected(role, value, RoleGuideLanguage.English))}"
        : $"{(int)role}:{Encode(RoleGuideCatalog.Expected(role, value, RoleGuideLanguage.English))}:{Encode(RoleGuideCatalog.Expected(role, value, RoleGuideLanguage.Japanese))}"));
long Allocations(Action action)
{
    for (int i = 0; i < 100; i++) action();
    long before = GC.GetAllocatedBytesForCurrentThread();
    for (int i = 0; i < 5000; i++) action();
    return GC.GetAllocatedBytesForCurrentThread() - before;
}

RoleAssignmentSync.Clear();
var empty = RoleAssignmentSync.Read();
Check(empty.Count == 0 && ReferenceEquals(empty, RoleAssignmentSync.Read()), "Empty cache");
var assignments = Enumerable.Range(1, 30).Select(i => new RoleAssignment
{
    SteamId = $"p{i}", Player = new PlayerAvatar { Id = $"p{i}", Name = $"参加者 {i};," },
    AssignedRole = StageRole.Imitator, Role = StageRole.Runner
}).ToList();
RoleAssignmentSync.Publish(assignments);
var first = RoleAssignmentSync.Read();
Check(first.Count == 30 && first[29].PlayerName == "参加者 30;,", "30 players / Unicode");
Check(first.Select((player, index) => player.PlayerNumber == index + 1).All(match => match), "Numbers match assignment order");
Check(first[0].Role == StageRole.Imitator && first[0].EffectiveRole == StageRole.Runner, "Copied role");
Check(ReferenceEquals(first, RoleAssignmentSync.Read()), "Unchanged assignment cache");
Check(((ICollection<RoleSnapshot>)first).IsReadOnly, "Snapshots cannot be modified by consumers");
assignments[0].Role = StageRole.Superbot;
RoleAssignmentSync.Publish(assignments);
Check(!ReferenceEquals(first, RoleAssignmentSync.Read()) && RoleAssignmentSync.Read()[0].EffectiveRole == StageRole.Superbot, "Reassignment invalidation");
assignments[0].Player.Name = "Renamed";
RoleAssignmentSync.Publish(assignments);
Check(RoleAssignmentSync.Read()[0].PlayerName == "Renamed", "Name invalidation");
SemiFunc.Local = assignments[29].Player;
RoleAssignmentSync.SetPreviewPlayerCount(1);
Check(RoleAssignmentSync.Read().Count == 1 && RoleAssignmentSync.Read()[0].SteamId == "p30", "Preview pins local player");
Check(RoleAssignmentSync.Read()[0].PlayerNumber == 30, "Preview reordering preserves real player number");
RoleAssignmentSync.SetPreviewPlayerCount(100);
Check(RoleAssignmentSync.Read().Count == 30, "Preview upper clamp");
RoleAssignmentSync.SetPreviewPlayerCount(-1);
Check(RoleAssignmentSync.Read().Count == 1, "Preview lower clamp");
RoleAssignmentSync.ClearPreview();
Check(RoleAssignmentSync.Read().Count == 30, "Preview clear");
RoleAssignmentSync.Publish(assignments.Skip(1).ToList());
Check(RoleAssignmentSync.Read()[0].SteamId == "p2" && RoleAssignmentSync.Read()[0].PlayerNumber == 1, "Departure renumbers current list");
RoleAssignmentSync.Publish(assignments);
Check(RoleAssignmentSync.Read()[29].PlayerNumber == 30, "Restored list restores corresponding numbers");
RoleAssignmentSync.Clear();
RoleAssignmentSync.SetPreviewPlayerCount(3);
Check(RoleAssignmentSync.Read()[0].SteamId == "p30", "Empty map preview local identity");
Check(RoleAssignmentSync.Read().All(player => player.PlayerNumber == 0), "Preview-only players have no command target number");
SemiFunc.Local = new PlayerAvatar { Id = "new", Name = "New Name" };
Check(RoleAssignmentSync.Read()[0].SteamId == "new", "Preview identity invalidation");
SemiFunc.Local.Name = "Changed";
Check(RoleAssignmentSync.Read()[0].PlayerName == "Changed", "Preview name invalidation");
RoleAssignmentSync.ClearPreview();

SemiFunc.Multiplayer = true;
PhotonNetwork.IsMasterClient = false;
PhotonNetwork.CurrentRoom = new TestRoom();
PhotonNetwork.CurrentRoom.CustomProperties["RS.RoleMap"] = $"old,1,{Encode("Old")};new,38,2,{Encode("New")};bad;unknown,77,eA==;broken,1,%%%";
var remote = RoleAssignmentSync.Read();
Check(remote.Count == 3 && remote[0].EffectiveRole == StageRole.Tank && remote[2].PlayerName == "", "Legacy / invalid assignment compatibility");
Check(remote[0].PlayerNumber == 1 && remote[1].PlayerNumber == 2, "Guest numbers use host payload order");
Check(ReferenceEquals(remote, RoleAssignmentSync.Read()), "Guest cache");
PhotonNetwork.CurrentRoom = new TestRoom();
Check(RoleAssignmentSync.Read().Count == 0, "Room change removes previous remote data");

var config = new StageRolesConfig();
PhotonNetwork.IsMasterClient = true;
RoleGuideSync.Invalidate();
RoleGuideCatalog.Calls = 0;
string signature = RoleGuideSync.CurrentSignature(config);
Check(signature == ExpectedGuide(21, false), "Guide wire format unchanged");
Check(RoleGuideCatalog.Calls == 2 * RoleCatalog.AllRoles.Count, "Both languages generated once");
var english = RoleGuideSync.Read(config, RoleGuideLanguage.English);
Check(ReferenceEquals(english, RoleGuideSync.Read(config, RoleGuideLanguage.English)), "Local guide reuse");
Check(ReferenceEquals(signature, RoleGuideSync.CurrentSignature(config)), "Signature reuse");
Check(RoleGuideCatalog.Calls == 2 * RoleCatalog.AllRoles.Count, "Polling does not regenerate text");
RoleGuideSync.Publish(config);
Check(PhotonNetwork.CurrentRoom.Writes == 1, "Initial guide publication");
Check((string)PhotonNetwork.CurrentRoom.CustomProperties["RoleShuffleGuideV1"] == ExpectedGuide(21, true), "Legacy wire format unchanged");
RoleGuideSync.Publish(config);
Check(PhotonNetwork.CurrentRoom.Writes == 1, "Identical publication skipped");
config.Value = 25;
RoleGuideSync.Invalidate();
Check(RoleGuideSync.CurrentSignature(config) == ExpectedGuide(25, false), "Config invalidation");
RoleGuideSync.Publish(config);
Check(PhotonNetwork.CurrentRoom.Writes == 2, "Changed config published");
PhotonNetwork.CurrentRoom = new TestRoom();
RoleGuideSync.Publish(config);
Check(PhotonNetwork.CurrentRoom.Writes == 1, "New room receives guide");

PhotonNetwork.IsMasterClient = false;
var guestEnglish = RoleGuideSync.Read(new StageRolesConfig { Value = 999 }, RoleGuideLanguage.English);
Check(guestEnglish[StageRole.Tank] == RoleGuideCatalog.Expected(StageRole.Tank, 25, RoleGuideLanguage.English), "Guest uses host settings");
Check(ReferenceEquals(guestEnglish, RoleGuideSync.Read(config, RoleGuideLanguage.English)), "Guest guide cache");
Check(RoleGuideSync.Read(config, RoleGuideLanguage.Japanese)[StageRole.Runner] == RoleGuideCatalog.Expected(StageRole.Runner, 25, RoleGuideLanguage.Japanese), "Language switch");
PhotonNetwork.CurrentRoom.CustomProperties.Remove("RoleShuffleGuideV2");
Check(RoleGuideSync.Read(config, RoleGuideLanguage.English)[StageRole.Tank] == RoleGuideCatalog.Expected(StageRole.Tank, 25, RoleGuideLanguage.English), "Legacy fallback");
Check(RoleGuideSync.Read(config, RoleGuideLanguage.Japanese)[StageRole.Tank].StartsWith("generic:"), "Old host Japanese fallback");
string legacySignature = RoleGuideSync.CurrentSignature(config);
PhotonNetwork.CurrentRoom.CustomProperties["RoleShuffleGuideV1"] = ExpectedGuide(26, true);
Check(RoleGuideSync.CurrentSignature(config) != legacySignature, "Legacy settings invalidate guide");
PhotonNetwork.CurrentRoom.CustomProperties["RoleShuffleGuideV2"] = "1:%%%:%%%";
string fallbackSignature = RoleGuideSync.CurrentSignature(config);
PhotonNetwork.CurrentRoom.CustomProperties["RoleShuffleGuideV1"] = ExpectedGuide(27, true);
Check(RoleGuideSync.CurrentSignature(config) != fallbackSignature, "Legacy changes invalidate malformed V2 fallback");
int warningCount = StageRolesPlugin.ModLogger.Warnings;
var invalid = RoleGuideSync.Read(config, RoleGuideLanguage.English);
Check(ReferenceEquals(invalid, RoleGuideSync.Read(config, RoleGuideLanguage.English)), "Invalid guide fallback cache");
Check(StageRolesPlugin.ModLogger.Warnings == warningCount + 1, "Invalid payload warning is not repeated on polling");
PhotonNetwork.CurrentRoom.CustomProperties["RoleShuffleGuideV2"] = ExpectedGuide(30, false);
Check(RoleGuideSync.Read(config, RoleGuideLanguage.Japanese)[StageRole.Tank] == RoleGuideCatalog.Expected(StageRole.Tank, 30, RoleGuideLanguage.Japanese), "Recovery after invalid payload");
PhotonNetwork.CurrentRoom = null;
Check(RoleGuideSync.Read(config, RoleGuideLanguage.Japanese)[StageRole.Tank].StartsWith("generic:"), "Room exit invalidation");

SemiFunc.Multiplayer = false;
RoleAssignmentSync.Publish(assignments);
config.DisabledRoles.Add(StageRole.Tank);
config.DisabledRoles.Add(StageRole.Superbot);
config.DisabledRoles.Add(StageRole.Disaster);
RoleGuideSync.Invalidate();
Check(!RoleGuideSync.IsVisible(StageRole.Tank, config), "Disabled role hidden locally");
Check(!RoleGuideSync.IsVisible(StageRole.Superbot, config), "Disabled secret role hidden");
Check(!RoleGuideSync.IsVisible(StageRole.Disaster, config), "Disabled Disaster hidden");
Check(RoleGuideSync.IsVisible(StageRole.Runner, config), "Enabled role stays visible");
Check(RoleGuideSync.Read(config, RoleGuideLanguage.English).ContainsKey(StageRole.Tank), "Disabled role retains Current Roles description");
SemiFunc.Multiplayer = true;
PhotonNetwork.IsMasterClient = true;
PhotonNetwork.CurrentRoom = new TestRoom();
RoleGuideSync.Publish(config);
string visibility = RoleGuideSync.VisibilitySignature(config);
PhotonNetwork.IsMasterClient = false;
var guestSettings = new StageRolesConfig();
guestSettings.DisabledRoles.Add(StageRole.Runner);
Check(!RoleGuideSync.IsVisible(StageRole.Tank, guestSettings), "Guest respects host disabled flag");
Check(RoleGuideSync.IsVisible(StageRole.Runner, guestSettings), "Guest does not apply its own disabled flag");
PhotonNetwork.IsMasterClient = true;
config.DisabledRoles.Clear();
RoleGuideSync.Invalidate();
RoleGuideSync.Publish(config);
Check(visibility != RoleGuideSync.VisibilitySignature(config), "Enabled changes invalidate guide visibility");
PhotonNetwork.IsMasterClient = false;
Check(RoleGuideSync.IsVisible(StageRole.Tank, guestSettings), "Re-enabled host role appears on guest");
PhotonNetwork.IsMasterClient = true;
foreach (StageRole role in RoleCatalog.AllRoles) config.DisabledRoles.Add(role);
RoleGuideSync.Invalidate();
RoleGuideSync.Publish(config);
PhotonNetwork.IsMasterClient = false;
Check(RoleCatalog.AllRoles.All(role => !RoleGuideSync.IsVisible(role, guestSettings)), "All-disabled host gives empty guide");
PhotonNetwork.CurrentRoom.CustomProperties.Remove("RoleShuffleGuideEnabledV1");
Check(RoleGuideSync.IsVisible(StageRole.Runner, guestSettings), "Old host safely shows guide without local filtering");
PhotonNetwork.CurrentRoom.CustomProperties["RoleShuffleGuideEnabledV1"] = "broken";
Check(RoleGuideSync.IsVisible(StageRole.Tank, guestSettings), "Malformed visibility falls back safely");
PhotonNetwork.CurrentRoom.CustomProperties["RoleShuffleGuideEnabledV1"] = "2";
Check(!RoleGuideSync.IsVisible(StageRole.Tank, guestSettings), "Visibility recovers after malformed payload");
PhotonNetwork.CurrentRoom = new TestRoom();
Check(RoleGuideSync.IsVisible(StageRole.Tank, guestSettings), "New room does not retain previous visibility");
config.DisabledRoles.Clear();
RoleGuideSync.Invalidate();
SemiFunc.Multiplayer = false;
long assignmentCached = Allocations(() => RoleAssignmentSync.Read());
long assignmentCold = Allocations(() => { RoleAssignmentSync.ResetCache(); RoleAssignmentSync.Read(); });
long guideCached = Allocations(() => RoleGuideSync.CurrentSignature(config));
long guideCold = Allocations(() => { RoleGuideSync.Invalidate(); RoleGuideSync.CurrentSignature(config); });
Check(assignmentCached < assignmentCold, "Assignment allocation reduction");
Check(guideCached < guideCold, "Guide allocation reduction");
Console.WriteLine($"PASS: {assertions} assertions.");
Console.WriteLine($"5000 reads, 30 assignments: cached {assignmentCached:N0} bytes; forced reparse {assignmentCold:N0} bytes.");
Console.WriteLine($"5000 guide signatures (stub role text): cached {guideCached:N0} bytes; forced regeneration {guideCold:N0} bytes.");
Console.WriteLine("These checks do not simulate Unity frame time, network transport, or live multiplayer.");
