using REPOJP.StageRoles;
using UnityEngine;
using Photon.Pun;
using System.Reflection;
int checks = 0;
void Check(bool condition, string label) { checks++; if (!condition) throw new Exception(label); }
void Near(float actual, float expected, string label) => Check(MathF.Abs(actual - expected) < .0001f, label + $" ({actual} vs {expected})");
DualWielderPose Pose(float t, bool attacking = false) => new(t, new(t, 0, 0), Quaternion.identity, Quaternion.identity, attacking, 20, 5);
foreach (int fps in new[] { 15, 30, 60, 144 })
{
    var history = new DualWielderHistory();
    for (int i = 0; i < fps * 10; i++)
    {
        float now = (float)i / fps;
        history.Record(Pose(now, now >= 1 && now < 2));
        bool ready = history.Sample(now - .3f, out var pose);
        Check(ready == (now >= .3f), "delay warmup at " + fps);
        if (ready) { Near(pose.Position.x, now - .3f, "timestamp interpolation at " + fps); Check(!pose.Attacking || now >= 1.3f, "never attack early"); }
    }
}
var h = new DualWielderHistory();
h.Record(Pose(0)); h.Record(new(.2f, new(.2f, 0, 0), new(0, 1, 0, 0), new(0, 1, 0, 0), true, 50, 10));
Check(h.Sample(.1f, out var middle) && !middle.Attacking && middle.EnemyDamage == 20, "attack state remains historical during interpolation");
Near(middle.Rotation.y, MathF.Sqrt(.5f), "rotation interpolation");
Check(h.Sample(.2f, out var hit) && hit.Attacking && hit.EnemyDamage == 50, "attack at its timestamp");
h.Record(new(.3f, new(100, 0, 0), Quaternion.identity, Quaternion.identity, false, 0, 0));
Check(h.Count == 1 && !h.Sample(.2f, out _), "teleport never sweeps through the map");
h.Record(Pose(.1f)); Check(h.Count == 1, "clock reset clears stale history");
h.Clear(); Check(h.Count == 0 && !h.Sample(10, out _), "reset erases previous attacks");
for (int i = 0; i < 10000; i++) h.Record(Pose(i / 10000f));
Check(h.Count == 4096, "high-frame-rate memory bounded");
h.Clear(); for (int i = 0; i < 100; i++) h.Record(Pose(i / 10f));
Check(h.Count <= 31, "long-running memory bounded by time");
object? Invoke(object target, string name, params object[] args) => target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(target, args);
bool Patch(string name, params object[] args) => (bool)typeof(DualWielderPatches).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, args)!;
int view = 100;
GameObject Weapon()
{
    var go = new GameObject(); go.AddComponent<Rigidbody>(); go.AddComponent<PhysGrabObject>(); go.AddComponent<Collider>();
    var photon = go.AddComponent<PhotonView>(); photon.ViewID = view++; PhotonView.Views[photon.ViewID] = photon;
    var attributes = go.AddComponent<ItemAttributes>(); var battery = go.AddComponent<ItemBattery>(); var melee = go.AddComponent<ItemMelee>();
    go.AddComponent<ItemEquippable>();
    var hurt = go.Child().AddComponent<HurtCollider>(); hurt.gameObject.AddComponent<Collider>(); hurt.gameObject.SetActive(false);
    melee.hurtCollider = hurt; melee.itemBattery = battery;
    var prefab = new GameObject { Factory = Weapon }; prefab.AddComponent<ItemMelee>();
    attributes.item = new Item { prefab = new PrefabRef { Prefab = prefab } }; PhotonNetwork.Prefabs["melee"] = prefab;
    return go;
}
var ownerGo = new GameObject(); var owner = ownerGo.AddComponent<PlayerAvatar>(); owner.physGrabber = ownerGo.AddComponent<PhysGrabber>();
var ownerView = ownerGo.AddComponent<PhotonView>(); ownerView.ViewID = 99; PhotonView.Views[99] = ownerView;
void Hold(GameObject weapon) { owner.physGrabber.grabbedObjectTransform = weapon.transform; weapon.GetComponent<PhysGrabObject>().playerGrabbing.Clear(); weapon.GetComponent<PhysGrabObject>().playerGrabbing.Add(owner.physGrabber); }
var source = Weapon(); Hold(source);
var config = new StageRolesConfig(); var runtime = new DualWielderRuntime(config);
RoleAssignment[] assignments = [new("owner", owner, StageRole.DualWielder)];
Time.time = 0; runtime.Tick(assignments);
var copy = UnityEngine.Object.Spawned.Last(); var echo = copy.GetComponent<DualWielderEcho>();
Check(echo.Source == source.GetComponent<ItemMelee>() && echo.Owner == owner, "copy matches source and owner");
Near(copy.transform.position.x, -.5f, "left offset at spawn");
Check(!copy.GetComponent<ItemBattery>().enabled, "independent battery writer disabled");
Check(!copy.GetComponent<Collider>().enabled && copy.GetComponent<Rigidbody>().isKinematic && !copy.GetComponent<Rigidbody>().useGravity, "copy has no solid collision or gravity");
Check(!Patch("SaveList", copy.GetComponent<ItemAttributes>()) && Patch("SaveList", source.GetComponent<ItemAttributes>()), "copy excluded from saved inventory without affecting source");
Check(!Patch("Name", copy.GetComponent<ItemAttributes>(), copy.GetComponent<PhotonView>().ViewID) && copy.GetComponent<ItemAttributes>().instanceName == source.GetComponent<ItemAttributes>().instanceName, "native item name is shared");
Check(!Patch("Equip", copy.GetComponent<ItemEquippable>()) && Patch("Equip", source.GetComponent<ItemEquippable>()), "cannot pocket the copy");
foreach (var name in new[] { "GrabLink", "GrabStart", "GrabAdd" }) Check(!Patch(name, copy.GetComponent<PhysGrabObject>(), 99) && owner.physGrabber.Released == copy.GetComponent<PhotonView>().ViewID, "reject generated-weapon grab: " + name);
Invoke(echo, "LateUpdate"); Invoke(echo, "FixedUpdate");
Check(!copy.GetComponent<ItemMelee>().hurtCollider.gameObject.activeSelf, "no attack during warmup");
var sourceMelee = source.GetComponent<ItemMelee>(); var copyMelee = copy.GetComponent<ItemMelee>();
Check(ReferenceEquals(copyMelee.itemBattery, sourceMelee.itemBattery), "both hit callbacks share the exact battery component");
sourceMelee.Hit(); copyMelee.Hit(); Near(sourceMelee.itemBattery.batteryLife, 90, "both weapons consume the shared reserve");
Check(copyMelee.hurtCollider.ignorePlayers.Contains(owner) && copyMelee.hurtCollider.ignoreObjects.Contains(source.GetComponent<PhysGrabObject>()) && copyMelee.hurtCollider.playerCausingHurtOverride == owner, "owner excluded and credited");
for (int i = 1; i <= 6; i++)
{
    Time.time = i / 10f; source.transform.position = new(Time.time, 0, 0); sourceMelee.hurtCollider.gameObject.SetActive(i >= 3); Invoke(echo, "LateUpdate"); Invoke(echo, "FixedUpdate");
}
Near(copy.transform.position.x, -.2f, "copy follows 0.3-second delayed source position plus left offset");
Check(copyMelee.hurtCollider.isActiveAndEnabled, "historical swing becomes active");
sourceMelee.itemBattery.batteryLife = 0; Invoke(echo, "FixedUpdate"); Check(!copyMelee.hurtCollider.gameObject.activeSelf, "empty battery prevents delayed extra damage");
sourceMelee.itemBattery.batteryLife = 50; copyMelee.hitTimer = 1; Invoke(echo, "FixedUpdate"); Check(!copyMelee.hurtCollider.gameObject.activeSelf, "native hit cooldown respected");
Check(!Patch("MeleeUpdate", copyMelee) && copyMelee.TimerTicks == 1 && Patch("MeleeUpdate", sourceMelee), "native copy timers continue while original update remains normal");
Check(!Patch("MeleeFixed", copyMelee) && Patch("MeleeFixed", sourceMelee), "only copy autonomous swing detector suppressed");
copyMelee.hitTimer = 0; config.DualWielderDelaySeconds.Value = .5f; Invoke(echo, "LateUpdate"); Invoke(echo, "FixedUpdate"); Check(!copyMelee.hurtCollider.gameObject.activeSelf, "config change resets stale swing history");
var replacement = Weapon(); Hold(replacement); runtime.Tick(assignments); Check(copy.Destroyed && !source.Destroyed, "switch removes old copy only");
copy = UnityEngine.Object.Spawned.Last(); echo = copy.GetComponent<DualWielderEcho>();
owner.physGrabber.grabbedObjectTransform = null; Invoke(echo, "FixedUpdate"); Check(copy.Destroyed, "release removes copy before another physics tick");
Hold(source); runtime.Tick(assignments); copy = UnityEngine.Object.Spawned.Last(); owner.Living = false; runtime.Tick(assignments); Check(copy.Destroyed, "death removes copy"); owner.Living = true;
runtime.Tick(assignments); copy = UnityEngine.Object.Spawned.Last(); runtime.Tick([new("owner", owner, StageRole.Tank)]); Check(copy.Destroyed, "role replacement removes copy");
runtime.Tick(assignments); copy = UnityEngine.Object.Spawned.Last(); runtime.Tick([]); Check(copy.Destroyed, "disconnect removes copy");
runtime.Tick(assignments); copy = UnityEngine.Object.Spawned.Last(); config.Enabled.Value = false; runtime.Tick(assignments); Check(copy.Destroyed, "disabling mod removes copy"); config.Enabled.Value = true;
source.GetComponent<PhysGrabObject>().playerGrabbing.Add(new GameObject().AddComponent<PhysGrabber>());
int count = UnityEngine.Object.Spawned.Count; runtime.Tick(assignments); Check(UnityEngine.Object.Spawned.Count == count, "shared holding creates no ambiguous copy");
Hold(source); SemiFunc.Multiplayer = true; runtime.Tick(assignments); copy = UnityEngine.Object.Spawned.Last(); echo = copy.GetComponent<DualWielderEcho>();
PhotonNetwork.FailDestroy = true; runtime.Stop(); Check(!copy.Destroyed && !copy.GetComponent<ItemMelee>().hurtCollider.enabled, "failed network cleanup is harmless immediately");
PhotonNetwork.FailDestroy = false; Time.time += 1.1f; Invoke(echo, "LateUpdate"); Check(copy.Destroyed && PhotonNetwork.NetworkDestroys == 1, "network cleanup retries after runtime releases role tracking");
runtime.Tick(assignments); copy = UnityEngine.Object.Spawned.Last(); runtime.Stop(); Check(copy.Destroyed && !source.Destroyed, "stage stop destroys network copy and preserves original");
Console.WriteLine($"PASS: {checks} delayed-pose and production-runtime checks with isolated Unity/native adapters. Live game physics and multiplayer are not simulated.");
