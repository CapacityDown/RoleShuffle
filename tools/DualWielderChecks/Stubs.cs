using System.Reflection;
namespace UnityEngine
{
    public class DefaultExecutionOrder(int order) : Attribute { public int Order = order; }
    public class Object
    {
        public bool Destroyed;
        public static readonly List<GameObject> Spawned = new();
        public static GameObject Instantiate(GameObject original, Vector3 position, Quaternion rotation)
        {
            var copy = original.Factory!(); copy.transform.SetPositionAndRotation(position, rotation); Spawned.Add(copy); return copy;
        }
        public static void Destroy(GameObject item) { item.Destroyed = true; foreach (var component in item.Components) component.Destroyed = true; }
        public static bool operator ==(Object? a, Object? b) => (ReferenceEquals(a, null) || a.Destroyed) ? ReferenceEquals(b, null) || b.Destroyed : ReferenceEquals(a, b);
        public static bool operator !=(Object? a, Object? b) => !(a == b);
        public override bool Equals(object? other) => ReferenceEquals(this, other);
        public override int GetHashCode() => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this);
    }
    public class Component : Object
    {
        public GameObject gameObject = null!;
        public Transform transform => gameObject.transform;
        public T GetComponent<T>() where T : Component => gameObject.Components.OfType<T>().FirstOrDefault()!;
        public T[] GetComponentsInChildren<T>(bool inactive) where T : Component => gameObject.Components.OfType<T>().Concat(gameObject.Children.SelectMany(c => c.GetComponentsInChildren<T>(inactive))).ToArray();
        public T GetComponentInParent<T>(bool inactive) where T : Component => GetComponent<T>() ?? gameObject.Parent?.GetComponentInParent<T>(inactive)!;
    }
    public class MonoBehaviour : Component { public bool enabled = true; public bool isActiveAndEnabled => enabled && gameObject.activeSelf; }
    public class GameObject : Component
    {
        public readonly List<Component> Components = new();
        public readonly List<GameObject> Children = new();
        public GameObject? Parent;
        public Func<GameObject>? Factory;
        public new Transform transform;
        public bool activeSelf = true;
        public GameObject() { gameObject = this; transform = AddComponent<Transform>(); }
        public T AddComponent<T>() where T : Component, new() { var result = new T { gameObject = this }; Components.Add(result); return result; }
        public GameObject Child() { var child = new GameObject { Parent = this }; Children.Add(child); return child; }
        public void SetActive(bool value) => activeSelf = value;
    }
    public class Transform : Component
    {
        public Vector3 position;
        public Quaternion rotation = Quaternion.identity;
        public Vector3 right => new(1, 0, 0);
        public Vector3 up => Vector3.up;
        public Vector3 forward => new(0, 0, 1);
        public void SetPositionAndRotation(Vector3 p, Quaternion r) { position = p; rotation = r; }
    }
    public readonly record struct Vector3(float x, float y, float z)
    {
        public static Vector3 zero => new(0, 0, 0);
        public static Vector3 up => new(0, 1, 0);
        public float sqrMagnitude => x*x + y*y + z*z;
        public Vector3 normalized => sqrMagnitude == 0 ? zero : this * (1 / MathF.Sqrt(sqrMagnitude));
        internal System.Numerics.Vector3 Native => new(x, y, z);
        internal static Vector3 From(System.Numerics.Vector3 v) => new(v.X, v.Y, v.Z);
        public static Vector3 operator +(Vector3 a, Vector3 b) => From(a.Native + b.Native);
        public static Vector3 operator -(Vector3 a) => From(-a.Native);
        public static Vector3 operator -(Vector3 a, Vector3 b) => From(a.Native - b.Native);
        public static Vector3 operator *(Vector3 a, float b) => From(a.Native * b);
        public static Vector3 Lerp(Vector3 a, Vector3 b, float t) => From(System.Numerics.Vector3.Lerp(a.Native, b.Native, t));
        public static Vector3 Cross(Vector3 a, Vector3 b) => From(System.Numerics.Vector3.Cross(a.Native, b.Native));
    }
    public readonly record struct Quaternion(float x, float y, float z, float w)
    {
        public static Quaternion identity => new(0, 0, 0, 1);
        internal System.Numerics.Quaternion Native => new(x, y, z, w);
        internal static Quaternion From(System.Numerics.Quaternion q) => new(q.X, q.Y, q.Z, q.W);
        public static Quaternion Slerp(Quaternion a, Quaternion b, float t) => From(System.Numerics.Quaternion.Slerp(a.Native, b.Native, t));
    }
    public static class Mathf { public static float Clamp01(float value) => Math.Clamp(value, 0, 1); }
    public static class Time { public static float time; }
    public struct Bounds { public Vector3 center; }
    public class Collider : Component { public bool enabled = true, isTrigger; public Bounds bounds; }
    public class MeshCollider : Collider { public bool convex; }
    public class Joint : Component { public float breakForce = 10, breakTorque = 10; }
    public class Rigidbody : Component { public bool useGravity = true; public bool isKinematic; public Vector3 position; public Quaternion rotation; }
}
namespace HarmonyLib
{
    public class HarmonyPatch : Attribute { public HarmonyPatch() {} public HarmonyPatch(Type type, string method) {} }
    public class HarmonyPrefix : Attribute;
    public class HarmonyPriority(int priority) : Attribute { public int Value = priority; }
    public static class Priority { public const int Last = 0; }
    public static class AccessTools
    {
        public static FieldInfo? Field(Type t, string n) => t.GetField(n, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        public static MethodInfo? Method(Type t, string n) => t.GetMethod(n, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    }
}
namespace Photon.Pun
{
    public enum RpcTarget { All }
    public class PhotonView : UnityEngine.Component
    {
        public int ViewID;
        public readonly List<string> Sent = new();
        public void RPC(string name, RpcTarget target, params object[] args) { Sent.Add(name); if(name=="EnemyOrPVPSwingHitRPC") GetComponent<ItemMelee>().EnemyOrPVPSwingHitRPC((bool)args[0]); }
        public static readonly Dictionary<int, PhotonView> Views = new();
        public static PhotonView? Find(int id) => Views.GetValueOrDefault(id);
    }
    public static class PhotonNetwork
    {
        public static bool InRoom = true, FailDestroy;
        public static int NetworkDestroys;
        public static readonly Dictionary<string, UnityEngine.GameObject> Prefabs = new();
        public static UnityEngine.GameObject Instantiate(string path, UnityEngine.Vector3 p, UnityEngine.Quaternion q) => UnityEngine.Object.Instantiate(Prefabs[path], p, q);
        public static void Destroy(UnityEngine.GameObject item) { if (FailDestroy) throw new Exception("temporary network failure"); NetworkDestroys++; UnityEngine.Object.Destroy(item); }
    }
}
public static class SemiFunc { public static bool Multiplayer, Authority = true; public static bool IsMultiplayer() => Multiplayer; public static bool IsMasterClientOrSingleplayer() => Authority;
    public static int ShopHits, PlayerHits;
    public static void PlayerShopKeeperMeleePhysHitHost(PlayerAvatar player) => ShopHits++;
    public static UnityEngine.Vector3 ClampDirection(UnityEngine.Vector3 direction, UnityEngine.Vector3 forward, float spread) => direction;
    public static void PlayerDamagingPlayer(PlayerAvatar a, PlayerAvatar b, int damage, bool tumble, bool killed, bool force = false) => PlayerHits++;
}
public class PlayerAvatar : UnityEngine.MonoBehaviour { public bool Living = true, isTumbling; public PhysGrabber physGrabber = null!;
    public PlayerHealth playerHealth = new(); public PlayerTumble? tumble;
    public UnityEngine.Vector3 LastImpulse;
    public void ForceImpulse(UnityEngine.Vector3 value) => LastImpulse = value; }
public class PhysGrabber : UnityEngine.Component { public UnityEngine.Transform? grabbedObjectTransform; public int Released; public void OverrideGrabRelease(int id, float seconds) => Released = id; }
public class PhysGrabObject : UnityEngine.Component
{
    public readonly List<PhysGrabber> playerGrabbing = new();
    public void OverrideKinematic(float seconds) => GetComponent<UnityEngine.Rigidbody>().isKinematic = true;
    public void OverrideGrabDisable(float seconds) {}
    public void OverrideIndestructible(float seconds) {}
    public void DisableDeathPitEffect(float seconds) {}
}
public class HurtCollider : UnityEngine.MonoBehaviour
{
    public enum BreakImpact { None, Light, Medium, Heavy, Special }
    public enum HitType { Player, PhysObject, Enemy }
    public enum TorqueAxis { up, down, left, right, forward, back }
    public readonly List<PlayerAvatar> ignorePlayers = new();
    public readonly List<PhysGrabObject> ignoreObjects = new();
    public int enemyDamage = 20, playerDamage = 5, playerTumbleImpactHurtDamage;
    public float playerDamageCooldown = .25f, physDamageCooldown = .25f, hitSpread = 180;
    public float physHitForce = 4, physHitTorque, playerHitForce, playerTumbleForce, playerTumbleTorque, playerTumbleTime, playerStunTime, playerTumbleImpactHurtTime, playerTumbleEnemyHurtDisableTime;
    public bool playerLogic = true, physLogic = true, physHingeDestroy, physHingeBreak, physDestroy, physReleaseGrab, physRayCast, playerRayCast, playerKill, playerHitFullRuckus;
    public BreakImpact physImpact = BreakImpact.Medium;
    public TorqueAxis playerTumbleTorqueAxis;
    public PlayerAvatar? playerCausingHurtOverride, onImpactPlayerAvatar;
    public UnityEngine.Events.UnityEvent onImpactAny = new(), onImpactPhysObject = new(), onImpactPlayer = new();
    private readonly Dictionary<UnityEngine.GameObject,float> cooldowns = new();
    private bool CanHit(UnityEngine.GameObject target, float cooldown, bool raycast, UnityEngine.Vector3 position, HitType type)
    { if(cooldowns.TryGetValue(target,out float until) && until>UnityEngine.Time.time)return false;cooldowns[target]=UnityEngine.Time.time+cooldown;return true; }
    // Stand in for the patched native PlayerHurt entry point, including body contacts.
    private void PlayerHurt(PlayerAvatar player) => GetComponentInParent<REPOJP.StageRoles.DualWielderEcho>(true).HitPlayer(this, player);
    public int PhysicalHits;
    private bool PhysObjectHurt(PhysGrabObject target, BreakImpact impact, float force, float torque, bool apply, bool launch, Enemy? enemy)
    { PhysicalHits++; target.GetComponent<PhysGrabObjectImpactDetector>().Hits++; return true; }
    private UnityEngine.Vector3 HitForceGet(UnityEngine.Vector3 direction, float force) => direction * force;
}
public class ItemMelee : UnityEngine.MonoBehaviour
{
    public bool swingLogicOnly, usesBatteryLogic = true;
    public HurtCollider hurtCollider = null!;
    public ItemBattery itemBattery = null!;
    public float hitTimer, hitBoxTimer, enemyOrPVPDurabilityLossCooldown;
    public int GroundHits;
    public int TimerTicks;
    private void TimersTick() { TimerTicks++; hitTimer = Math.Max(0, hitTimer - .02f); }
    // Stands in for the native hit callbacks; their battery-field use is checked against game IL separately.
    public void PlayerSwingHit() => throw new Exception("local disarm must not run for generated copies");
    public void GroundHit() { if(hitTimer>0 || hitBoxTimer<=0)return; GroundHits++; hitTimer=.2f; }
    public void EnemyOrPVPSwingHitRPC(bool playerHit) { enemyOrPVPDurabilityLossCooldown=.1f; itemBattery.batteryLife-=5; }
    public void Hit() => itemBattery.batteryLife -= 5;
}
public class ItemBattery : UnityEngine.MonoBehaviour { public float batteryLife = 100; }
public class ItemAttributes : UnityEngine.Component { public Item item = null!; public string instanceName = "source-weapon"; }
public class ItemEquippable : UnityEngine.Component;
public class Item { public PrefabRef prefab = null!; }
public class PrefabRef { public string ResourcePath = "melee"; public UnityEngine.GameObject Prefab = null!; public bool IsValid() => Prefab != null; }
public class ItemManager { public static ItemManager instance = new(); public readonly List<ItemAttributes> spawnedItems = new(); public void AddSpawnedItem(ItemAttributes item) => spawnedItems.Add(item); }
public class StatsManager { public void ItemFetchName(string name, ItemAttributes item, int id) {} }
public class PunManager { public static PunManager instance = new(); public void SetItemName(string name, ItemAttributes item, int id) => item.instanceName = name; }
namespace REPOJP.StageRoles
{
    internal enum StageRole { Tank, DualWielder }
    internal sealed record RoleAssignment(string SteamId, PlayerAvatar Player, StageRole Role);
    internal static class RoleCatalog { internal static bool HasCapability(StageRole role, StageRole capability) => role == capability; }
    internal static class PlayerState { internal static bool IsLiving(PlayerAvatar player) => player.Living; internal static bool TryGetCurrentHealth(PlayerAvatar p, out int health) { health=p.playerHealth.Health; return true; } }
    internal static class MageSpellAim { internal static UnityEngine.Vector3 GetDirection(PlayerAvatar player) => new(0, 0, 1); }
    internal class Entry<T>(T value) { internal T Value = value; }
    internal class StageRolesConfig
    {
        internal Entry<bool> Enabled = new(true);
        internal Entry<float> DualWielderLeftOffset = new(.5f), DualWielderDelaySeconds = new(.3f);
    }
    internal static class StageRolesPlugin { internal static Logger ModLogger = new(); }
    internal class Logger { internal readonly List<string> Warnings = new(); internal void LogWarning(string message) => Warnings.Add(message); }
}

namespace UnityEngine.Events { public class UnityEvent { public Action? Callback; public int Calls; public void Invoke(){Calls++;Callback?.Invoke();} } }
public class Enemy : UnityEngine.Component {}
public class EnemyRigidbody : UnityEngine.Component {}
public class EnemyParent : UnityEngine.Component {}
public class PlayerController : UnityEngine.Component { public PlayerAvatar playerAvatarScript = null!; }
public class PlayerHealth { public int Health=100, Calls; public void HurtOther(int damage, UnityEngine.Vector3 position, bool savingGrace, int enemy=-1, bool heal=false){Health-=damage;Calls++;} }
public class PlayerTumble : UnityEngine.Component {
 public PlayerAvatar playerAvatar=null!; public float OverrideTime, StunTime; public UnityEngine.Vector3 Force;
 public void TumbleRequest(bool tumble,bool input) {playerAvatar.isTumbling=tumble;}
 public void TumbleOverrideTime(float time)=>OverrideTime=time;
 public void TumbleStunTime(float time)=>StunTime=time;
 public void ImpactHurtSet(float time,int damage){}
 public void OverrideEnemyHurt(float time){}
 public void TumbleForce(UnityEngine.Vector3 force)=>Force=force;
 public void TumbleTorque(UnityEngine.Vector3 torque){}
}
public class PhysGrabObjectImpactDetector : UnityEngine.Component { public bool destroyDisable; public int Hits; public void DestroyObject()=>Hits++; }
public class PhysGrabHinge : UnityEngine.Component { public UnityEngine.Joint? joint; public int DestroyedHinge; public void DestroyHinge()=>DestroyedHinge++; }
