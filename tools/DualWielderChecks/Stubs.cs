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
        public void SetPositionAndRotation(Vector3 p, Quaternion r) { position = p; rotation = r; }
    }
    public readonly record struct Vector3(float x, float y, float z)
    {
        public static Vector3 up => new(0, 1, 0);
        public float sqrMagnitude => x*x + y*y + z*z;
        public Vector3 normalized => this * (1 / MathF.Sqrt(sqrMagnitude));
        internal System.Numerics.Vector3 Native => new(x, y, z);
        internal static Vector3 From(System.Numerics.Vector3 v) => new(v.X, v.Y, v.Z);
        public static Vector3 operator +(Vector3 a, Vector3 b) => From(a.Native + b.Native);
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
    public class Collider : Component { public bool enabled = true; }
    public class Rigidbody : Component { public bool useGravity = true; public bool isKinematic; public Vector3 position; public Quaternion rotation; }
}
namespace HarmonyLib
{
    public class HarmonyPatch : Attribute { public HarmonyPatch() {} public HarmonyPatch(Type type, string method) {} }
    public class HarmonyPrefix : Attribute;
    public static class AccessTools
    {
        public static FieldInfo? Field(Type t, string n) => t.GetField(n, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        public static MethodInfo? Method(Type t, string n) => t.GetMethod(n, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    }
}
namespace Photon.Pun
{
    public class PhotonView : UnityEngine.Component
    {
        public int ViewID;
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
public static class SemiFunc { public static bool Multiplayer, Authority = true; public static bool IsMultiplayer() => Multiplayer; public static bool IsMasterClientOrSingleplayer() => Authority; }
public class PlayerAvatar : UnityEngine.MonoBehaviour { public bool Living = true; public PhysGrabber physGrabber = null!; }
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
    public readonly List<PlayerAvatar> ignorePlayers = new();
    public readonly List<PhysGrabObject> ignoreObjects = new();
    public int enemyDamage = 20, playerDamage = 5;
    public PlayerAvatar? playerCausingHurtOverride;
}
public class ItemMelee : UnityEngine.MonoBehaviour
{
    public bool swingLogicOnly, usesBatteryLogic = true;
    public HurtCollider hurtCollider = null!;
    public ItemBattery itemBattery = null!;
    public float hitTimer;
    public int TimerTicks;
    private void TimersTick() { TimerTicks++; hitTimer = Math.Max(0, hitTimer - .02f); }
    // Stands in for the native hit callbacks; their battery-field use is checked against game IL separately.
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
    internal static class PlayerState { internal static bool IsLiving(PlayerAvatar player) => player.Living; }
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
