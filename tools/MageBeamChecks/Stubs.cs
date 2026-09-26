namespace UnityEngine
{
    public class DefaultExecutionOrder(int order) : Attribute { public int Order = order; }
    public readonly record struct Vector3(float x, float y, float z)
    {
        public static Vector3 zero => new(0, 0, 0);
        public static Vector3 up => new(0, 1, 0);
        public static Vector3 forward => new(0, 0, 1);
        public float sqrMagnitude => x*x + y*y + z*z;
        public Vector3 normalized => this * (1f / MathF.Sqrt(sqrMagnitude));
        internal System.Numerics.Vector3 Native => new(x, y, z);
        internal static Vector3 From(System.Numerics.Vector3 v) => new(v.X, v.Y, v.Z);
        public static Vector3 operator +(Vector3 a, Vector3 b) => From(a.Native + b.Native);
        public static Vector3 operator -(Vector3 a, Vector3 b) => From(a.Native - b.Native);
        public static Vector3 operator *(Vector3 a, float b) => From(a.Native * b);
    }
    public readonly record struct Quaternion(float x, float y, float z, float w)
    {
        public static Quaternion identity => new(0, 0, 0, 1);
        internal System.Numerics.Quaternion Native => new(x, y, z, w);
        internal static Quaternion From(System.Numerics.Quaternion q) => new(q.X, q.Y, q.Z, q.W);
        public static Quaternion Inverse(Quaternion q) => From(System.Numerics.Quaternion.Inverse(q.Native));
        public static Quaternion operator *(Quaternion a, Quaternion b) => From(a.Native * b.Native);
        public static Vector3 operator *(Quaternion q, Vector3 v) => Vector3.From(System.Numerics.Vector3.Transform(v.Native, q.Native));
        public static Quaternion LookRotation(Vector3 forward)
        {
            var z = System.Numerics.Vector3.Normalize(forward.Native);
            var up = MathF.Abs(z.Y) > 0.999f ? System.Numerics.Vector3.UnitZ : System.Numerics.Vector3.UnitY;
            var x = System.Numerics.Vector3.Normalize(System.Numerics.Vector3.Cross(up, z));
            var y = System.Numerics.Vector3.Cross(z, x);
            return From(System.Numerics.Quaternion.CreateFromRotationMatrix(new(
                x.X, x.Y, x.Z, 0, y.X, y.Y, y.Z, 0, z.X, z.Y, z.Z, 0, 0, 0, 0, 1)));
        }
    }
    public class Component
    {
        public GameObject gameObject = null!;
        public Transform transform => gameObject.transform;
        public T? GetComponent<T>() where T : Component => gameObject.GetComponent<T>();
        public T[] GetComponentsInChildren<T>(bool includeInactive) where T : Component =>
            gameObject.Components.OfType<T>().Concat(gameObject.Children.SelectMany(c => c.GetComponentsInChildren<T>(includeInactive))).ToArray();
        public T? GetComponentInParent<T>(bool includeInactive) where T : Component =>
            GetComponent<T>() ?? gameObject.Parent?.GetComponentInParent<T>(includeInactive);
    }
    public class MonoBehaviour : Component;
    public class Transform : Component
    {
        public Vector3 position;
        public Quaternion rotation = Quaternion.identity;
        public Vector3 forward => rotation * Vector3.forward;
        public void SetPositionAndRotation(Vector3 p, Quaternion r) { position = p; rotation = r; }
    }
    public class GameObject : Component
    {
        public GameObject? Parent;
        public List<GameObject> Children = new();
        public List<Component> Components = new();
        public new Transform transform;
        public GameObject()
        {
            gameObject = this;
            transform = new() { gameObject = this };
            Components.Add(transform);
        }
        public GameObject Child() { var child = new GameObject { Parent = this }; Children.Add(child); return child; }
        public T AddComponent<T>() where T : Component, new()
        {
            var component = new T { gameObject = this }; Components.Add(component); return component;
        }
        public new T? GetComponent<T>() where T : Component => Components.OfType<T>().FirstOrDefault();
    }
    public class Renderer : Component { public bool enabled = true; }
    public class Light : Component { public bool enabled = true; }
    public class Collider : Component { public bool enabled = true; }
    public enum RigidbodyConstraints { None, FreezeAll }
    public class Rigidbody : Component
    {
        public bool isKinematic;
        public bool useGravity = true;
        public Vector3 velocity;
        public Vector3 angularVelocity;
        public Vector3 position;
        public Quaternion rotation;
        public RigidbodyConstraints constraints;
    }
}
namespace HarmonyLib
{
    public class HarmonyPatch(Type type, string name) : Attribute { public Type Type = type; public string Name = name; }
    public class HarmonyPrefix : Attribute;
    public class HarmonyPostfix : Attribute;
}
namespace Photon.Pun
{
    public class PhotonView : UnityEngine.Component { public object[]? InstantiationData; }
}
public class SemiLaser : UnityEngine.Component;
public class HurtCollider : UnityEngine.Component;
public class ValuableWizardStaff : UnityEngine.Component;
public class PlayerAvatar : UnityEngine.Component
{
    public bool Living = true;
    public PlayerAvatarVisuals? playerAvatarVisuals;
    public UnityEngine.Transform? playerTransform;
    public LocalCamera? localCamera;
}
public class PlayerAvatarVisuals { public UnityEngine.Transform? headLookAtTransform; }
public class LocalCamera
{
    public UnityEngine.Transform Aim = new UnityEngine.GameObject().transform;
    public bool Throw;
    public UnityEngine.Transform GetOverrideTransform() => Throw ? throw new InvalidOperationException() : Aim;
}
public static class SemiFunc
{
    public static bool Authority = true;
    public static bool IsMasterClientOrSingleplayer() => Authority;
}
public class PhysGrabObject : UnityEngine.Component
{
    public float KinematicSeconds, GravitySeconds, GrabDisableSeconds;
    public void OverrideKinematic(float seconds) { KinematicSeconds = seconds; GetComponent<UnityEngine.Rigidbody>()!.isKinematic = true; }
    public void OverrideZeroGravity(float seconds) { GravitySeconds = seconds; }
    public void OverrideGrabDisable(float seconds) { GrabDisableSeconds = seconds; }
}
namespace REPOJP.StageRoles
{
    internal static class PlayerState { internal static bool IsLiving(PlayerAvatar player) => player.Living; }
    internal static class StageRolesPlugin { internal static Logger ModLogger = new(); }
    internal sealed class Logger { internal void LogWarning(string message) => throw new Exception(message); }
}
