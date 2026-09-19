using REPOJP.StageRoles;
using Photon.Pun;
using UnityEngine;
using System.Globalization;

int checks = 0;
void Check(bool condition, string name) { checks++; if (!condition) throw new Exception(name); }
void Clock(float value) { Time.time = Time.unscaledTime = value; PhotonNetwork.Time = value; }

SemiFunc.GameReady = false;
PhotonAccess.FailOnRead = true;
RoleAbilitySync.Clear();
RoleAbilitySync.Clear();
Check(PhotonAccess.Reads == 0, "Startup cleanup never initializes Photon or accesses GameManager");
PhotonAccess.FailOnRead = false;
SemiFunc.GameReady = true;

foreach (string command in new[] { "Health", "Speed", "Stamina" })
{
    double fullCap = RoleOverhaulRules.MaximumValue(command);
    Check(Math.Ceiling(Enumerable.Range(0, 201).Max(n => RoleOverhaulRules.UpgradeValue(command, n))) == fullCap,
        "Effective cap is the ceiling of the maximum over ALL levels 0-200");
    foreach (int baseline in Enumerable.Range(0, 201))
    foreach (int minimum in new[] { 0, 21, 46, 200 })
    foreach (double multiplier in new[] { 1d, 1.3d, 1.5d, 2d, 10d })
    foreach (double cap in new[] { fullCap, RoleOverhaulRules.UpgradeValue(command, 50) })
    {
        int floor = Math.Max(baseline, minimum);
        int target = RoleOverhaulRules.UpgradeTarget(command, baseline, minimum, multiplier, cap);
        double limit = Math.Max(cap, RoleOverhaulRules.UpgradeValue(command, floor));
        double goal = Math.Min(limit, RoleOverhaulRules.UpgradeValue(command, baseline) * multiplier);
        Check(target >= floor && target <= 200 && RoleOverhaulRules.UpgradeValue(command, target) <= limit,
            "Linear growth preserves Base/minimum and obeys the effective growth cap");
        Check(RoleOverhaulRules.UpgradeValue(command, target) + 1e-8 >= goal,
            "Integer upgrades reach the multiplied actual value when cap is level-aligned");
        Check(target == floor || RoleOverhaulRules.UpgradeValue(command, target - 1) < goal,
            "Select the first adequate integer level");
    }
}
Check(RoleOverhaulRules.UpgradeValue("Health", 0) == 100, "Vanilla base HP");
Check(RoleOverhaulRules.UpgradeValue("Speed", 0) == 5, "Serialized sprint speed, not C# initializer 1");
Check(RoleOverhaulRules.UpgradeValue("Stamina", 0) == 40, "Serialized stamina, not C# initializer 100");
Check(RoleOverhaulRules.UpgradeTarget("Health", 100, 21, 1.5, 4100) == 153, "HP multiplication includes initial 100 HP");
Check(RoleOverhaulRules.UpgradeTarget("Speed", 6, 6, 1.5, 205) == 12, "Sprint-speed multiplication includes initial speed 5");
Check(RoleOverhaulRules.UpgradeTarget("Stamina", 46, 46, 1.5, 2040) == 71, "Stamina multiplication includes initial capacity 40");
Check(RoleOverhaulRules.UpgradeTarget("Health", 100, 21, 1.5, 3101) == 150, "Rounding never crosses a non-level-aligned cap");
Check(RoleOverhaulRules.UpgradeTarget("Health", 10, 21, double.NaN, 4100) == 21, "Invalid multiplier preserves floor");
Check(!RoleOverhaulRules.GrowsWithBase(StageRole.Rammer), "Rammer locks stay outside growth rules");

Check(Math.Abs(RoleOverhaulRules.EffectiveGrabStrength(20, true) - 2.619047619d) < 1e-8, "Light-force reference");
Check(Math.Abs(RoleOverhaulRules.EffectiveGrabStrength(50, false) - 5.958333333d) < 1e-8, "Heavy-force peak at 50");
Check(Math.Ceiling(Enumerable.Range(0, 201).Max(n => Math.Max(
    RoleOverhaulRules.EffectiveGrabStrength(n, true), RoleOverhaulRules.EffectiveGrabStrength(n, false)))) ==
    RoleOverhaulRules.MaximumValue("Strength"), "Strength cap uses the entire level range");
double[] Forces(int level) => new[] {
    RoleOverhaulRules.EffectiveGrabStrength(level, true), RoleOverhaulRules.EffectiveGrabStrength(level, false),
    RoleOverhaulRules.EffectiveGrabStrength(level, true, true), RoleOverhaulRules.EffectiveGrabStrength(level, false, true) };
Check(!RoleOverhaulRules.GrowsWithBase(StageRole.Lifter), "Lifter no longer uses Base multipliers");
Check(Math.Abs(RoleOverhaulRules.EffectiveGrabStrength(200, false) - 5.290322580645d) < 1e-8,
    "Vanilla level-200 grip reference before Lifter correction");
Check(Math.Abs(RoleOverhaulRules.EffectiveGrabStrength(200, false, true) - 4.978571428571d) < 1e-8,
    "Vanilla level-200 rotation reference before Lifter correction");
Check(Enumerable.Range(0, 201).Where(n => RoleOverhaulRules.EffectiveGrabStrength(n, false) >
    RoleOverhaulRules.EffectiveGrabStrength(200, false)).SequenceEqual(Enumerable.Range(32, 37)),
    "Vanilla Strength 200 reduces heavy grip from Base 32 through 68");
Check(Enumerable.Range(0, 201).Where(n => RoleOverhaulRules.EffectiveGrabStrength(n, false, true) >
    RoleOverhaulRules.EffectiveGrabStrength(200, false, true)).SequenceEqual(Enumerable.Range(28, 45)),
    "Vanilla Strength 200 reduces heavy rotation coefficient from Base 28 through 72");

var state = new RoleOverhaulState();
state.Start(10, 30); state.Start(20, 30);
Check(state.PaidUntil == 40, "Initial grace cannot be refreshed by repeated setup");
Check(!state.Carry(1, 0, false, 10, 5, 30, 3), "Pick-up establishes cargo");
Check(!state.Carry(1, 3, false, 11, 5, 30, 3), "Partial journey");
Check(!state.Carry(1, 2, false, 12, 5, 30, 3), "Outside travel alone does not complete");
Check(state.Carry(1, 1, true, 13, 5, 30, 3), "Delivery completes outside-to-truck journey");
Check(state.ContractsCompleted == 1 && state.PaidUntil == 43, "Bounded reward recorded once");
Check(!state.Carry(1, 3, false, 14, 5, 30, 3) && state.CargoId == 0, "Duplicate object cannot pay twice");
state.Carry(2, 0, false, 15, 5, 30, 3); state.Carry(2, 3, false, 16, 5, 30, 3);
state.ResetCargo(); state.Carry(2, 0, false, 17, 5, 30, 3);
Check(state.CarryDistance == 0 && state.ContractsCompleted == 1, "Drop/death resets progress while retaining paid work");
Check(!state.Carry(2, 10, true, 18, 5, 30, 3) && state.CargoId == 0, "Teleport cannot complete a contract");
Check(!state.Carry(3, 0, true, 19, 5, 30, 3), "Starting inside truck is not work");
foreach (float invalid in new[] { float.NaN, float.PositiveInfinity, -1f })
    Check(!state.Carry(4, invalid, false, 20, 5, 30, 3), "Invalid travel rejected");
for (int item = 2; item <= 3; item++)
{
    state.Carry(item, 0, false, 21, 5, 30, 3);
    state.Carry(item, 3, false, 22, 5, 30, 3);
    state.Carry(item, 3, false, 23, 5, 30, 3);
    Check(state.Carry(item, 0, true, 24, 5, 30, 3), "Another distinct delivery");
}
Check(!state.Carry(4, 0, false, 25, 5, 30, 3) && state.ContractsCompleted == 3, "Stage contract cap");

var config = new StageRolesConfig();
var runtime = new RoleOverhaulRuntime(config);
var notifier = new RoleNotifier();
var worker = new RoleAssignment();
var cargo = new PhysGrabObject { Id = 10 };
UnityEngine.Object.Items = new[] { cargo };
Clock(0); runtime.Tick(new[] { worker }, notifier);
Clock(1); cargo.centerPoint = new Vector3(3,0,0); runtime.Tick(new[] { worker }, notifier);
Clock(2); cargo.centerPoint = new Vector3(6,0,0); runtime.Tick(new[] { worker }, notifier);
Clock(3); worker.Player.InTruck = true; runtime.Tick(new[] { worker }, notifier);
Check(worker.Overhaul.ContractsCompleted == 1 && worker.Player.playerHealth.Health == 60 && notifier.Notifications == 1,
    "Production contract runtime delivers one health reward and notification");
Clock(4); runtime.Tick(new[] { worker }, notifier);
Check(notifier.Notifications == 1, "Holding delivered cargo cannot repeat rewards");
worker.Player.InTruck = false; cargo.Id = 11; cargo.Valuable!.Value = 0;
Clock(5); runtime.Tick(new[] { worker }, notifier);
Check(worker.Overhaul.CargoId == 0, "Zero-value generated valuables are ineligible");
cargo.Valuable.Value = 100;
Clock(6); runtime.Tick(new[] { worker }, notifier);
Clock(7); cargo.centerPoint = new Vector3(8,0,0); runtime.Tick(new[] { worker }, notifier);
worker.Player.Living = false;
Clock(8); runtime.Tick(new[] { worker }, notifier);
Check(worker.Overhaul.CarryDistance == 0 && worker.Overhaul.ContractsCompleted == 1, "Death preserves contract budget");
worker.Player = new PlayerAvatar { Id = "p" };
Clock(9); runtime.Tick(new[] { worker }, notifier);
Check(worker.Overhaul.ContractsCompleted == 1 && worker.Overhaul.PaidUntil == 33, "Replacement avatar retains earned state");

runtime.Stop(); RoleHealingRuntime.Clear();
var king = new RoleAssignment { Role = StageRole.King, Player = new PlayerAvatar { Id = "king" } };
var ally1 = new RoleAssignment { Role = StageRole.Tank, Player = new PlayerAvatar { Id = "a" } };
var ally2 = new RoleAssignment { Role = StageRole.Tank, Player = new PlayerAvatar { Id = "b" } };
var party = new[] { king, ally1, ally2 };
int Level(string id, string command) => StatsManager.instance.FetchPlayerUpgrades(id).GetValueOrDefault("playerUpgrade" + command, 0);
void Set(string id, string command, int level) => UpgradeService.SetLevels(id, new[] { new UpgradeGrant(command, "playerUpgrade" + command, level) });
void Aura() => KingUpgradeAura.Tick(config, party);
UnityEngine.Object.Scans = 0;
Clock(10); runtime.Tick(party, notifier);
Check(king.Player.playerHealth.Health == 50 && ally1.Player.playerHealth.Health == 50 && Level("king", "Speed") == 0,
    "King neither heals nor buffs itself");
Check(Level("a", "Speed") == 1 && Level("a", "Range") == 1 && Level("a", "Strength") == 1 && king.Overhaul.KingSupportedAllies == 2,
    "Nearby allies receive native upgrades and King sees supported ally count");
for (int i = 0; i < 10; i++) Aura();
Check(Level("a", "Speed") == 1 && UnityEngine.Object.Scans == 0, "Aura never accumulates or scans valuables");
UpgradeService.AddLevels("a", "Speed", 4); // Purchased/event upgrade while buffed.
ally1.Player.transform.position = new Vector3(9,0,0); Aura();
Check(Level("a", "Speed") == 4 && Level("a", "Range") == 0 && king.Overhaul.KingSupportedAllies == 1,
    "Leaving range removes only King's addition and preserves external upgrades");
ally1.Player.transform.position = new Vector3(8,0,0); Aura();
Check(Level("a", "Speed") == 5, "Exact radius is included");
Set("a", "Speed", 10); Aura();
Check(Level("a", "Speed") == 11, "Role reset replaces old aura ownership before reapplying it");
Set("a", "Speed", 11); Aura();
Check(Level("a", "Speed") == 12, "An absolute no-op still replaces the previous aura contribution");
UpgradeService.EnsureAtLeastLevels("a", new[] { new UpgradeGrant("Speed", "playerUpgradeSpeed", 12) }); Aura();
Check(Level("a", "Speed") == 13, "Returning-role minimum is evaluated without aura");
Check(KingUpgradeAura.WithoutBonus("a", "playerUpgradeSpeed", 13) == 12, "Dynamic roles snapshot baseline without temporary aura");
Set("a", "Speed", 200); Set("a", "Range", 199); Set("a", "Strength", 50); Aura();
Check(Level("a", "Speed") == 200 && Level("a", "Range") == 200 && Level("a", "Strength") == 50,
    "Level cap and non-monotonic Strength peak are respected");
foreach (int baseline in Enumerable.Range(0, 201))
foreach (int request in new[] { 0, 1, 5, 200 })
{
    int bonus = KingUpgradeAura.DesiredBonus("Strength", baseline, request);
    Check(bonus >= 0 && bonus <= request && baseline + bonus <= 200, "Bounded King Strength bonus");
    Check(Forces(baseline + bonus).Zip(Forces(baseline)).All(pair => pair.First >= pair.Second), "Aura never weakens any grip/rotation coefficient");
}
ally2.Player.Living = false; Aura();
Check(Level("b", "Speed") == 0 && Level("b", "Range") == 0, "Death removes recipient aura");
ally2.Player = new PlayerAvatar { Id = "b" }; Aura();
Check(Level("b", "Speed") == 1, "Replacement avatar receives one aura");
king.Player.Living = false; Aura();
Check(Level("b", "Speed") == 0 && Level("a", "Range") == 199, "King death removes all support");
king.Player.Living = true; Aura();
king.Role = StageRole.Medic; Aura();
Check(Level("b", "Speed") == 0, "King role change removes support");
king.Role = StageRole.King; Aura();
KingUpgradeAura.Tick(config, new[] { king, ally1 });
Check(Level("b", "Speed") == 0, "Disconnected ally's addition is removed from host stats");
Aura(); Check(Level("b", "Speed") == 1, "Rejoin receives one copy");
var otherKing = new RoleAssignment { Role = StageRole.King, Player = new PlayerAvatar { Id = "king2" } };
KingUpgradeAura.Tick(config, new[] { king, otherKing, ally2 });
Check(Level("b", "Speed") == 1 && Level("king2", "Speed") == 0, "Overlapping Kings never stack or buff Kings");
config.OverhaulEnabled.Value = false; Aura();
Check(Level("b", "Speed") == 0, "Legacy toggle removes existing additions");
Clock(25); runtime.Tick(new[] { worker }, notifier);
Check(UnityEngine.Object.Scans == 0, "Legacy mode does not scan contracts");
config.OverhaulEnabled.Value = true;
SemiFunc.Multiplayer = true; PhotonNetwork.IsMasterClient = false; Aura();
Check(Level("b", "Speed") == 0, "Guest cannot grant upgrades");
PhotonNetwork.IsMasterClient = true; int sendsBefore = PhotonView.Sends; Aura();
Check(Level("b", "Speed") == 1 && PhotonView.Sends > sendsBefore, "Host applies locally and sends vanilla RPCs to guests");
runtime.Stop();
Check(Level("b", "Speed") == 0 && Level("a", "Range") == 199, "Stage cleanup removes only aura");
PhotonView.FailSend = true; Aura(); Aura();
Check(Level("b", "Speed") == 1, "Send failure after local application cannot stack grants on retry");
PhotonView.FailSend = false; runtime.Stop();
PhotonAccess.FailOnRead = true; KingUpgradeAura.Stop(); PhotonAccess.FailOnRead = false;
SemiFunc.Multiplayer = false;

// Preserve coverage of shared remote healing locks, independent of King's removed healing.
RoleHealingRuntime.Clear(); SemiFunc.Multiplayer = true;
ally1.Player.photonView.IsMine = false;
ally1.Player.playerHealth.OnHeal = _ => { };
int beforeRequests = ally1.Player.playerHealth.Requests;
Clock(26); Check(RoleHealingRuntime.TryHeal(ally1.Player, 2, _ => { }), "Remote heal accepted");
Clock(27); Check(!RoleHealingRuntime.TryHeal(ally1.Player, 2, _ => { }), "Unacknowledged heal locks recipient");
Check(ally1.Player.playerHealth.Requests == beforeRequests + 1, "Pending remote heal is not duplicated");
Clock(32); Check(RoleHealingRuntime.TryHeal(ally1.Player, 2, _ => { }), "Remote lock expires");
SemiFunc.Multiplayer = false; RoleHealingRuntime.Clear();
Check(RoleAbilityText.Format(new[] { new AbilityValue(AbilityMetric.RoyalSupport, 2, 0) }, RoleGuideLanguage.Japanese).Contains("強化中の味方 2"), "King HUD counts supported allies");

var metrics = new[] { new AbilityValue(AbilityMetric.Medic, 90, 150), new AbilityValue(AbilityMetric.MageCooldown, 2, 0) };
var snapshot = new AbilitySnapshot("p|日本語", StageRole.Imitator, StageRole.Medic, metrics);
string encoded = RoleAbilityCodec.Encode(new[] { snapshot });
var decoded = RoleAbilityCodec.Decode(encoded);
Check(decoded.Count == 1 && decoded[snapshot.SteamId].Effective == StageRole.Medic && decoded[snapshot.SteamId].Values[0].Remaining == 90,
    "Versioned status preserves identity, copied role and resource values");
Check(RoleAbilityCodec.Decode(new string('x', 20001)).Count == 0, "Oversized payload rejected");
Check(RoleAbilityCodec.Decode("bad|0|0|0,5,10").Count == 0, "Malformed identity rejected");
Check(RoleAbilityCodec.Decode(encoded.Replace("0,90,150", "0,-1,150")).Count == 0, "Negative remote values rejected");
Check(RoleAbilityCodec.Decode(encoded.Replace("0,90,150", "999,90,150")).Count == 0, "Unknown metric rejected");
var crowd = Enumerable.Range(1, 30).Select(i => new AbilitySnapshot(i.ToString(), StageRole.Medic, StageRole.Medic, metrics)).ToArray();
string crowdPayload = RoleAbilityCodec.Encode(crowd);
Check(RoleAbilityCodec.Decode(crowdPayload).Count == 30 && crowdPayload.Length < RoleAbilityCodec.MaximumPayloadLength,
    "Thirty-player status fits the packet bound and round-trips");
CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
Check(RoleAbilityCodec.Encode(new[] { snapshot }) == encoded, "Wire format is culture independent");
Check(RoleAbilityText.Format(metrics, RoleGuideLanguage.Japanese).Contains("回復 90/150"), "Japanese resource text");

SemiFunc.Multiplayer = true; PhotonNetwork.IsMasterClient = true; PhotonNetwork.CurrentRoom = new Room();
Clock(30); RoleAbilitySync.Publish(new[] { snapshot });
var role = new RoleSnapshot(snapshot.SteamId, StageRole.Imitator, StageRole.Medic);
Check(RoleAbilitySync.Read(role)?.Values.Count == 2, "Host publishes readable state");
int sends = PhotonNetwork.CurrentRoom.Publications;
Clock(31); RoleAbilitySync.Publish(new[] { snapshot });
Check(PhotonNetwork.CurrentRoom.Publications == sends, "Unchanged payload is not resent every tick");
Clock(35); RoleAbilitySync.Publish(new[] { snapshot });
Check(PhotonNetwork.CurrentRoom.Publications == sends + 1, "Idle state heartbeat");
PhotonNetwork.IsMasterClient = false;
Check(RoleAbilitySync.Read(role) != null, "Installed guest reads host state");
Check(RoleAbilitySync.Read(role with { EffectiveRole = StageRole.King }) == null, "Role mismatch cannot show another ability's state");
Clock(51); Check(RoleAbilitySync.Read(role) == null, "Stale status expires");
Clock(36); PhotonNetwork.CurrentRoom = new Room();
Check(RoleAbilitySync.Read(role) == null, "New room/old host cannot reuse local cached state");
RoleAbilitySync.Publish(new[] { snapshot });
Check(PhotonNetwork.CurrentRoom.Publications == 0, "Guest cannot publish authority state");
PhotonNetwork.IsMasterClient = true; RoleAbilitySync.Clear();
Check(RoleAbilitySync.Read(role) == null, "Stage cleanup clears ability data");
SemiFunc.GameReady = false;
RoleAbilitySync.Clear();
Check(PhotonNetwork.CurrentRoom.CustomProperties.Count == 0, "Shutdown cleanup does not access a destroyed GameManager");
SemiFunc.GameReady = true;
var publishedRoom = new Room();
PhotonNetwork.CurrentRoom = publishedRoom;
Clock(60); RoleAbilitySync.Publish(new[] { snapshot });
Check(publishedRoom.CustomProperties.Count == 1, "Host has published ability state before cleanup");
SemiFunc.GameReady = false;
RoleAbilitySync.Clear();
Check(publishedRoom.CustomProperties.Count == 0 && publishedRoom.Publications == 2,
    "Shutdown clears only the room where this host published, without GameManager");
PhotonAccess.FailOnRead = true;
RoleAbilitySync.Clear();
PhotonAccess.FailOnRead = false;
Check(publishedRoom.Publications == 2, "Repeated cleanup cannot access Photon or resend removal");
SemiFunc.GameReady = true;
Clock(70); RoleAbilitySync.Publish(new[] { snapshot });
var nextRoom = new Room();
nextRoom.CustomProperties["RS.Ability.v1"] = "next host's state";
PhotonNetwork.CurrentRoom = nextRoom;
RoleAbilitySync.Clear();
Check(nextRoom.Publications == 0 && nextRoom.CustomProperties.Count == 1,
    "Cleanup after changing rooms cannot clear another host's state");
Clock(80); RoleAbilitySync.Publish(new[] { snapshot });
PhotonNetwork.IsMasterClient = false;
RoleAbilitySync.Clear();
Check(nextRoom.Publications == 1 && nextRoom.CustomProperties.Count == 1,
    "Former host cannot clear state after authority has changed");
Console.WriteLine($"PASS: {checks} overhaul growth, contract runtime, King aura, healing locks, codec and synchronization checks.");
