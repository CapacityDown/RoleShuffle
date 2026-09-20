using System.Collections;
using System.Reflection;
using REPOJP.StageRoles;
using UnityEngine;
using Photon.Pun;

int checks = 0;
void Check(bool value, string message)
{
    if (!value) throw new Exception(message);
    checks++;
}

foreach (bool multiplayer in new[] { false, true })
foreach (string scenario in new[] { "normal", "disaster", "returned", "dead", "truck", "removed", "replaced", "stopped", "authority", "invalid-view", "send-failed" })
{
    if (!multiplayer && scenario is "invalid-view" or "send-failed") continue;
    SemiFunc.Multiplayer = multiplayer;
    SemiFunc.Authority = true;
    PhotonNetwork.Sends = 0;
    PhotonNetwork.PriceSends = 0;
    PhotonNetwork.ThrowOnSend = scenario == "send-failed";
    PhysGrabObjectImpactDetector.LocalBreaks = 0;
    var assignment = new RoleAssignment { Player = new PlayerAvatar() };
    if (scenario == "disaster") assignment.Role = StageRole.Disaster;
    assignment.Player.transform.position = new Vector3(5, 0, 0);
    var runtime = new StinkerRoleRuntime(new MonoBehaviour(), new StageRolesConfig(), new VanillaRolePrefabResolver());
    runtime.Begin(new[] { assignment });
    var flags = BindingFlags.NonPublic | BindingFlags.Instance;
    var points = (Dictionary<string, List<Vector3>>)typeof(StinkerRoleRuntime).GetField("_pendingTrailPoints", flags)!.GetValue(runtime)!;
    var generation = (int)typeof(StinkerRoleRuntime).GetField("_generation", flags)!.GetValue(runtime)!;
    var routine = (IEnumerator)typeof(StinkerRoleRuntime).GetMethod("SpawnAndBreak", flags)!.Invoke(runtime,
        new object[] { Vector3.zero, new ResolvedRolePrefab(), generation, assignment, points[assignment.SteamId] })!;
    Check(routine.MoveNext() && routine.Current == null, scenario + ": waits for local Start");
    GameObject carrier = GameObject.LastCreated!;
    Check(carrier.GetComponent<ValuableObject>() is { Value: 0f, PriceInitialized: true }, scenario + ": zero price before Start");
    Check(PhotonNetwork.PriceSends == (SemiFunc.Multiplayer ? 1 : 0), scenario + ": zero price synchronized only in multiplayer");
    Rigidbody body = carrier.GetComponent<Rigidbody>()!;
    Check(body.isKinematic && body.constraints == RigidbodyConstraints.FreezeAll, scenario + ": position and rotation locked");
    body.isKinematic = false; // Vanilla EnableRigidbody resets this after 0.1 seconds.
    Check(body.constraints == RigidbodyConstraints.FreezeAll, scenario + ": freeze survives vanilla activation");
    Check(PhotonNetwork.Sends == 0 && PhysGrabObjectImpactDetector.LocalBreaks == 0, scenario + ": no immediate break");
    Check(routine.MoveNext() && routine.Current is WaitForSeconds { Seconds: 0.5f }, scenario + ": half-second grace after spawn");
    Check(PhotonNetwork.Sends == 0 && PhysGrabObjectImpactDetector.LocalBreaks == 0,
        scenario + ": no local or remote break during grace");
    switch (scenario)
    {
        case "returned": assignment.Player.transform.position = Vector3.zero; break;
        case "dead": assignment.Player.Living = false; break;
        case "truck": assignment.Player.InTruck = true; break;
        case "removed": runtime.RemovePlayer(assignment.SteamId); break;
        case "replaced": runtime.RemovePlayer(assignment.SteamId); runtime.AddPlayer(assignment); break;
        case "stopped": runtime.Stop(); break;
        case "authority": SemiFunc.Authority = false; break;
        case "invalid-view": carrier.GetComponent<PhotonView>()!.ViewID = 0; break;
    }
    bool waiting = routine.MoveNext();
    if ((scenario == "normal" || scenario == "disaster") && multiplayer)
    {
        Check(waiting && routine.Current is WaitForSeconds { Seconds: 2f }, "tracks carrier through server delivery");
        Check(PhotonNetwork.Sends == 1 && PhotonNetwork.Target == RpcTarget.AllViaServer, "vanilla break uses server ordering");
        Check(PhotonNetwork.Method == "DestroyObjectRPC", "uses vanilla-client RPC");
        Check(PhysGrabObjectImpactDetector.LocalBreaks == 0, "no early local destruction");
        Check(!routine.MoveNext(), "bounded delivery cleanup completes");
    }
    else if (scenario == "normal" || scenario == "disaster")
    {
        Check(!waiting && PhysGrabObjectImpactDetector.LocalBreaks == 1 && PhotonNetwork.Sends == 0, "singleplayer retains local break");
    }
    else
    {
        Check(!waiting && PhotonNetwork.Sends == 0 && PhysGrabObjectImpactDetector.LocalBreaks == 0, scenario + ": break canceled safely");
    }
    Check(carrier.Destroyed, scenario + ": carrier cleaned up");
    Check(((IList)typeof(StinkerRoleRuntime).GetField("_carriers", flags)!.GetValue(runtime)!).Count == 0, scenario + ": tracking cleared");
}
Console.WriteLine($"PASS: {checks} Stinker coroutine checks (stubbed Unity/Photon; multiplayer timing requires in-game testing).");

namespace UnityEngine
{
    public class Object
    {
        public bool Destroyed;
        public static GameObject Instantiate(GameObject prefab, Vector3 position, Quaternion rotation) => GameObject.Create(position);
        public static void Destroy(Object obj) => obj.Destroyed = true;
    }
    public class Component : Object
    {
        public GameObject gameObject = null!;
        public Transform transform => gameObject.transform;
        public T? GetComponent<T>() where T : Component => gameObject.GetComponent<T>();
        public T? GetComponentInParent<T>() where T : Component => gameObject.GetComponent<T>();
    }
    public class Transform { public Vector3 position; }
    public class GameObject : Object
    {
        public static GameObject? LastCreated;
        public Transform transform = new();
        private readonly List<Component> components = new();
        public static GameObject Create(Vector3 position)
        {
            var obj = new GameObject();
            obj.transform.position = position;
            foreach (Component component in new Component[] { new UraniumScript(), new PhysGrabObjectImpactDetector(), new PhysGrabObject(), new Rigidbody(), new PhotonView(), new ValuableObject() })
            {
                component.gameObject = obj;
                obj.components.Add(component);
            }
            return LastCreated = obj;
        }
        public T? GetComponent<T>() where T : Component => components.OfType<T>().FirstOrDefault();
        public T? GetComponentInChildren<T>(bool includeInactive) where T : Component => GetComponent<T>();
        public T[] GetComponentsInChildren<T>(bool includeInactive) where T : Component => components.OfType<T>().ToArray();
    }
    public class MonoBehaviour : Component { public object StartCoroutine(IEnumerator routine) => routine; }
    public enum RigidbodyConstraints { None, FreezeAll }
    public class Rigidbody : Component { public bool isKinematic; public Vector3 velocity, angularVelocity; public RigidbodyConstraints constraints; }
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static Vector3 zero => new(0, 0, 0);
        public static Vector3 up => new(0, 1, 0);
        public static Vector3 down => new(0, -1, 0);
        public float magnitude => MathF.Sqrt(x * x + y * y + z * z);
        public static Vector3 operator +(Vector3 a, Vector3 b) => new(a.x + b.x, a.y + b.y, a.z + b.z);
        public static Vector3 operator -(Vector3 a, Vector3 b) => new(a.x - b.x, a.y - b.y, a.z - b.z);
        public static Vector3 operator *(Vector3 a, float b) => new(a.x * b, a.y * b, a.z * b);
        public static Vector3 operator /(Vector3 a, float b) => a * (1 / b);
    }
    public struct Quaternion { public static Quaternion identity => new(); }
    public class WaitForSeconds(float seconds) { public float Seconds => seconds; }
    public static class Time { public static float time; }
    public struct RaycastHit { public Vector3 point; }
    public enum QueryTriggerInteraction { Ignore }
    public static class Physics
    {
        public const int AllLayers = -1;
        public static bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit hit, float distance, int mask, QueryTriggerInteraction triggers) { hit = default; return false; }
    }
}
namespace Photon.Pun
{
    public enum RpcTarget { AllViaServer, Others }
    public class PhotonView : Component
    {
        public int ViewID = 1;
        public void RPC(string name, RpcTarget target, object[] values)
        {
            if (name == "DollarValueSetRPC")
            {
                if (target != RpcTarget.Others || values.Length != 1 || (float)values[0] != 0f)
                    throw new Exception("Invalid zero-price synchronization");
                PhotonNetwork.PriceSends++;
                return;
            }
            if (PhotonNetwork.ThrowOnSend) throw new Exception("Simulated send failure");
            PhotonNetwork.Sends++; PhotonNetwork.Method = name; PhotonNetwork.Target = target;
        }
    }
    public static class PhotonNetwork
    {
        public static bool IsMasterClient => SemiFunc.Authority;
        public static bool ThrowOnSend;
        public static int Sends;
        public static int PriceSends;
        public static string Method = "";
        public static RpcTarget Target;
        public static GameObject InstantiateRoomObject(string path, Vector3 position, Quaternion rotation, byte group) => GameObject.Create(position);
        public static void Destroy(GameObject obj) => UnityEngine.Object.Destroy(obj);
    }
}
public static class SemiFunc
{
    public static bool Multiplayer, Authority;
    public static bool IsMultiplayer() => Multiplayer;
    public static bool IsMasterClientOrSingleplayer() => Authority;
}
public class PlayerAvatar { public Transform transform = new(); public bool Living = true, InTruck; }
public class UraniumScript : Component { }
public class ValuableObject : Component
{
    public float Value = 1000f;
    public bool PriceInitialized;
    public void DollarValueSetRPC(float value) { Value = value; PriceInitialized = true; }
}
public class PhysGrabObject : Component { public void OverrideGrabDisable(float duration) { } }
public class PhysGrabObjectImpactDetector : Component
{
    public static int LocalBreaks;
    public void DestroyObject(bool effects) => LocalBreaks++;
    public void DestroyObjectRPC() { }
}
namespace REPOJP.StageRoles
{
    internal enum StageRole { Stinker, Disaster }
    internal static class RoleCatalog
    {
        internal static bool HasCapability(StageRole role, StageRole capability) => role == capability || role == StageRole.Disaster;
    }
    internal class RoleAssignment
    {
        public StageRole Role = StageRole.Stinker;
        public string SteamId = "test";
        public PlayerAvatar Player = new();
        public Vector3 StinkerPreviousPosition, StinkerTrailAnchor;
        public float StinkerTravelDistance;
    }
    internal class StageRolesConfig
    {
        public readonly Setting StinkerAllowTruckSpawns = new();
        public float ClampedStinkerDistance => 2;
        public float ClampedStinkerSafetyDistance => 2;
    }
    internal class Setting { public bool Value => false; }
    internal class ResolvedRolePrefab { public string ResourcePath = "uranium"; public GameObject Prefab = new(); }
    internal class VanillaRolePrefabResolver
    {
        public bool TryGetRandomUraniumValuable(out ResolvedRolePrefab resolved) { resolved = new(); return true; }
    }
    internal static class PlayerState
    {
        public static bool IsLiving(PlayerAvatar player) => player != null && player.Living;
        public static bool IsInTruck(PlayerAvatar player) => player.InTruck;
    }
    internal static class StageFluxCompatibility { public static void MarkInternalCarrier(GameObject carrier) { } }
    internal static class StageRolesPlugin { public static readonly Logger ModLogger = new(); }
    internal class Logger { public void LogWarning(string message) { } public void LogDebug(string message) { } }
}
