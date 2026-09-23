namespace UnityEngine
{
    public class DefaultExecutionOrder(int order) : Attribute { public int Order = order; }
    public readonly record struct Vector3(float x, float y, float z)
    {
        public static Vector3 zero => new(0, 0, 0);
    }
    public readonly record struct Quaternion(float x, float y, float z, float w);
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
        public Quaternion rotation;
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
public class PhysGrabObject : UnityEngine.Component
{
    public float KinematicSeconds, GravitySeconds, GrabDisableSeconds;
    public void OverrideKinematic(float seconds) { KinematicSeconds = seconds; GetComponent<UnityEngine.Rigidbody>()!.isKinematic = true; }
    public void OverrideZeroGravity(float seconds) { GravitySeconds = seconds; }
    public void OverrideGrabDisable(float seconds) { GrabDisableSeconds = seconds; }
}
namespace REPOJP.StageRoles
{
    internal static class StageRolesPlugin { internal static Logger ModLogger = new(); }
    internal sealed class Logger { internal void LogWarning(string message) => throw new Exception(message); }
}
