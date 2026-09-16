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

foreach (int baseline in Enumerable.Range(0, 201))
foreach (int minimum in new[] { 0, 21, 46, 200 })
foreach (int bonus in new[] { 0, 2, 5, 10, 200 })
{
    int target = RoleOverhaulRules.UpgradeTarget(baseline, minimum, bonus);
    Check(target >= baseline && target >= minimum && target <= 200, "Bounded growth preserves base and role minimum");
    Check(RoleOverhaulRules.UpgradeTarget(baseline, minimum, bonus) == target, "Reapplication does not accumulate bonuses");
}
Check(RoleOverhaulRules.UpgradeTarget(100, 21, 5) == 105, "Late-run Tank growth");
Check(RoleOverhaulRules.UpgradeTarget(1, 21, 5) == 21, "Early-run Tank minimum");
Check(RoleOverhaulRules.UpgradeTarget(199, 21, 5) == 200, "Cap");
Check(!RoleOverhaulRules.GrowsWithBase(StageRole.Rammer), "Rammer locks are outside growth rules");

Check(Math.Abs(RoleOverhaulRules.EffectiveGrabStrength(20, true) - 2.619047619d) < 1e-8,
    "Vanilla light-object Strength 20 reference");
Check(Math.Abs(RoleOverhaulRules.EffectiveGrabStrength(25, true) - 2.326530612d) < 1e-8,
    "Vanilla light-object Strength 25 is weaker despite the higher level");
Check(Math.Abs(RoleOverhaulRules.EffectiveGrabStrength(50, false) - 5.958333333d) < 1e-8,
    "Vanilla heavy-object Strength 50 reference");
Check(RoleOverhaulRules.EffectiveGrabStrength(55, false) < RoleOverhaulRules.EffectiveGrabStrength(50, false),
    "Vanilla heavy-object Strength can also decrease");
foreach (int baseline in Enumerable.Range(0, 201))
foreach (int minimum in new[] { 0, 17, 25, 50, 200 })
foreach (int bonus in new[] { 0, 1, 5, 20, 200 })
{
    int target = RoleOverhaulRules.StrengthTarget(baseline, minimum, bonus);
    int requested = RoleOverhaulRules.UpgradeTarget(baseline, minimum, bonus);
    int floor = Math.Max(baseline, minimum);
    Check(target >= floor && target <= requested && target <= 200,
        "Lifter preserves Base and the configured floor without exceeding the requested level");
    Check(RoleOverhaulRules.EffectiveGrabStrength(target, true) >= RoleOverhaulRules.EffectiveGrabStrength(floor, true) &&
        RoleOverhaulRules.EffectiveGrabStrength(target, false) >= RoleOverhaulRules.EffectiveGrabStrength(floor, false) &&
        RoleOverhaulRules.EffectiveGrabStrength(target, true, true) >= RoleOverhaulRules.EffectiveGrabStrength(floor, true, true) &&
        RoleOverhaulRules.EffectiveGrabStrength(target, false, true) >= RoleOverhaulRules.EffectiveGrabStrength(floor, false, true),
        "Growth above the floor never weakens ordinary light/heavy translation or rotation");
    Check(RoleOverhaulRules.StrengthTarget(baseline, minimum, bonus) == target,
        "Lifter reassignment cannot accumulate Strength bonuses");
}
Check(RoleOverhaulRules.StrengthTarget(15, 25, 5) == 25, "Configured minimum takes priority over light-object penalties");
Check(RoleOverhaulRules.StrengthTarget(20, 25, 5) == 25, "Preserve the configured floor even if its force is below Base");
Check(RoleOverhaulRules.StrengthTarget(50, 25, 5) == 50, "Do not grant a weakening bonus on heavy objects");
Check(RoleOverhaulRules.StrengthTarget(90, 25, 5) == 95, "Growth resumes beyond the declining region");
Check(RoleOverhaulRules.StrengthTarget(-50, 25, -1) == 25, "Invalid negative inputs are clamped");
Check(RoleOverhaulRules.StrengthTarget(250, 500, 500) == 200, "Oversized inputs remain capped");
Check(RoleOverhaulRules.EffectiveGrabStrength(200, false) > RoleOverhaulRules.EffectiveGrabStrength(70, false) &&
    RoleOverhaulRules.EffectiveGrabStrength(200, false, true) < RoleOverhaulRules.EffectiveGrabStrength(70, false, true),
    "High Strength can improve translation while weakening rotation");
Check(RoleOverhaulRules.StrengthTarget(70, 200, 5) == 200, "Even a high configured floor takes priority over rotation penalties");
Check(RoleOverhaulRules.StrengthTarget(70, 25, 130) == 70, "High bonus growth still respects rotation penalties above the floor");
Check(RoleOverhaulRules.StrengthTarget(15, 25, 0) == 25, "Zero bonus does not disable the configured floor");
Check(RoleOverhaulRules.StrengthTarget(24, 25, 5) == 25, "Unsafe growth stops at the configured floor, not below it");
Check(RoleOverhaulRules.StrengthTarget(32, 25, 5) == 37, "Safe growth above the configured floor remains enabled");

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
config.KingHealLimit.Value = 5;
var party = new[] { king, ally1, ally2 };
UnityEngine.Object.Scans = 0;
Clock(10); runtime.Tick(party, notifier);
Check(king.Player.playerHealth.Health == 50 && ally1.Player.playerHealth.Health == 52 && ally2.Player.playerHealth.Health == 52,
    "King heals living allies but not itself");
Clock(15); runtime.Tick(party, notifier); Clock(20); runtime.Tick(party, notifier);
Check(king.Overhaul.KingHealingUsed == 5 && ally1.Player.playerHealth.Health + ally2.Player.playerHealth.Health == 105,
    "King's last tick cannot exceed the shared stage allowance");
Check(UnityEngine.Object.Scans == 0, "No valuable scan without an eligible Jobless");
config.OverhaulEnabled.Value = false;
Clock(25); runtime.Tick(new[] { worker }, notifier);
Check(UnityEngine.Object.Scans == 0, "Legacy mode does not scan contracts");

runtime.Stop(); RoleHealingRuntime.Clear();
config.OverhaulEnabled.Value = true;
config.KingHealInterval.Value = 0.5f;
king.Overhaul = new RoleOverhaulState();
SemiFunc.Multiplayer = true;
ally1.Player.photonView.IsMine = false;
ally1.Player.playerHealth.OnHeal = _ => { }; // A delayed remote acknowledgement.
ally2.Player.Living = false;
int beforeRequests = ally1.Player.playerHealth.Requests;
Clock(26); runtime.Tick(party, notifier);
Clock(27); runtime.Tick(party, notifier);
Check(king.Overhaul.KingHealingUsed == 2 && ally1.Player.playerHealth.Requests == beforeRequests + 1,
    "Unacknowledged remote heal reserves budget and locks the recipient");
Clock(32); runtime.Tick(party, notifier);
Clock(38); runtime.Tick(party, notifier);
Clock(44); runtime.Tick(party, notifier);
Check(king.Overhaul.KingHealingUsed == 5 && ally1.Player.playerHealth.Requests == beforeRequests + 3,
    "Expired recipient locks never refund unacknowledged remote healing or exceed King's cap");
SemiFunc.Multiplayer = false; RoleHealingRuntime.Clear();

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
Console.WriteLine($"PASS: {checks} overhaul growth, contract runtime, healing budget, codec and synchronization checks.");
