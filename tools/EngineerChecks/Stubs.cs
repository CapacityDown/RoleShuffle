using System.Runtime.CompilerServices;
#pragma warning disable CS0169, CS0414, CS0649
namespace UnityEngine
{
    public class Component
    {
        public PhysGrabObject? Owner;
        public GameObject gameObject = new();
        public T[] GetComponentsInChildren<T>(bool inactive) => Owner!.Components.OfType<T>().ToArray();
        public T? GetComponent<T>() => Owner!.Components.OfType<T>().FirstOrDefault();
        public T? GetComponentInChildren<T>(bool inactive) => GetComponent<T>();
    }
    public class GameObject { public bool activeSelf = true; public void SetActive(bool active) => activeSelf = active; }
    public class Transform { public Quaternion localRotation = Quaternion.Euler(10, 20, 30); }
    public record struct Quaternion(float X, float Y, float Z)
    {
        public static Quaternion identity => new(0, 0, 0);
        public static Quaternion Euler(float x, float y, float z) => new(x, y, z);
    }
    public class LineRenderer : Component { }
    public enum ParticleSystemStopBehavior { StopEmittingAndClear }
    public class ParticleSystem
    {
        public bool isPlaying = true;
        public void Stop(bool children, ParticleSystemStopBehavior behavior) => isPlaying = false;
    }
}
public class PhysGrabObject : UnityEngine.Component
{
    public readonly List<UnityEngine.Component> Components = new();
    public PhysGrabObject() { Owner = this; Components.Add(this); }
    public T Add<T>(T component) where T : UnityEngine.Component { component.Owner = this; Components.Add(component); return component; }
}
public class Trap : UnityEngine.Component { [MethodImpl(MethodImplOptions.NoInlining)] public void TrapActivate() { } }
public class ValuableHorseOnWheels : Trap
{
    [MethodImpl(MethodImplOptions.NoInlining)] public void Update() { }
    [MethodImpl(MethodImplOptions.NoInlining)] public void FixedUpdate() { }
}
public class ValuableCamera : UnityEngine.Component { }
public class DecorativeValuable : UnityEngine.Component { }
public class HurtCollider : UnityEngine.Component { }
public class Sound
{
    public bool Playing = true;
    public void PlayLoop(bool playing, float fadeInSpeed, float fadeOutSpeed, float pitchMultiplier = 1, float volumeMultiplier = 1) => Playing = playing;
}
public class ValuableShootingCowboy : UnityEngine.Component
{
    public UnityEngine.Transform? arm = new(), armShakeTransform = new(), cowboyNeck = new(), scarf1 = new(), scarf2 = new();
    public HurtCollider? hurtCollider = new();
    public UnityEngine.LineRenderer? lineRenderer = new();
    public List<UnityEngine.ParticleSystem> shootEffects = new() { new() };
    public Sound? armRotationLoop = new(), mechanicWhirringLoop = new();
    public float recoilForce = 5;
    private float armRotationTimer, reloadTimer, shootTimer, afterShootTimer, coolDownTimer, hurtColliderTimer, lineRendererLerp, animPhase, animWeight;
    private float currentArmAngle = 100, armReturnTimer = 3, armReturnTime = 3;
    private bool grabbed, shot, reloadSound, shakeSound, lineRendererActive;
    public int FrameCalls, ShootCalls, FireCalls;
    [MethodImpl(MethodImplOptions.NoInlining)] public void Update() => FrameCalls++;
    [MethodImpl(MethodImplOptions.NoInlining)] public void Shoot() => ShootCalls++;
    [MethodImpl(MethodImplOptions.NoInlining)] public void FireBullet() => FireCalls++;
}
namespace REPOJP.StageRoles
{
    // Tests exercise production catalog, hook selection and cleanup; holder
    // authentication is unchanged production controller code, not simulated here.
    public class StageRolesPlugin
    {
        public static StageRolesPlugin? Instance = new();
        public Controller? Controller = new();
    }
    public class Controller
    {
        public bool Suppress;
        public bool ShouldSuppressEngineerEffect(UnityEngine.Component component)
        {
            if (!Suppress || !EngineerEffectCatalog.IsEffectValuable(component.Owner!)) return false;
            if (component is ValuableShootingCowboy cowboy) EngineerCowboySuppression.Reset(cowboy);
            return true;
        }
    }
}
