using REPOJP.StageRoles;

namespace UnityEngine
{
    public sealed class Rigidbody { public float mass = 10; }
    public static class Mathf
    {
        public static float Min(float a, float b) => Math.Min(a, b);
        public static float Lerp(float a, float b, float t) => a + (b - a) * Math.Clamp(t, 0f, 1f);
    }
}
public sealed class PlayerAvatar
{
    public PhysGrabber physGrabber = null!;
    public bool isTumbling;
    public bool Living = true;
    internal StageRole Role = StageRole.Lifter;
}
public sealed class PhysGrabber
{
    public PlayerAvatar playerAvatar = new();
    public PhysGrabber() { playerAvatar.physGrabber = this; }
    public float grabStrength = 41;
    internal float overrideGrabStrength = -1;
    public float Grip, Torque;
}
// Preserve the fixture emitter's simple foreach while exposing the game's
// Count/indexer collection contract to the melee prefix.
public sealed class GrabberCollection(PhysGrabber[] items)
{
    public int Count => items.Length;
    public PhysGrabber this[int index] => items[index];
    public Enumerator GetEnumerator() => new(items);
    public static implicit operator GrabberCollection(PhysGrabber[] items) => new(items);
    public struct Enumerator(PhysGrabber[] items)
    {
        private int index = -1;
        public PhysGrabber Current => items[index];
        public bool MoveNext() => ++index < items.Length;
    }
}
public sealed class PhysGrabObject
{
    public UnityEngine.Rigidbody rb = new();
    public bool isGun, isMelee;
    public ItemAttributes? itemAttributes;
    public T? GetComponent<T>() where T : class => itemAttributes as T;
    public bool overrideExtraGrabStrengthDisable, overrideExtraTorqueStrengthDisable;
    public float overrideGrabStrengthTimer, overrideMinGrabStrengthTimer, overrideTorqueStrengthTimer, overrideMinTorqueStrengthTimer;
    public float overrideGrabStrength, overrideMinGrabStrength, overrideTorqueStrength = 1, overrideMinTorqueStrength;
    public GrabberCollection playerGrabbing = Array.Empty<PhysGrabber>();

    // Vanilla's two blends in a per-player loop, emitted and run after patching.
    public void PhysicsGrabbingManipulation()
    {
        foreach (PhysGrabber item in playerGrabbing)
        {
            float strength = item.grabStrength;
            float reduced = strength / (1f + UnityEngine.Mathf.Min(strength, 30f));
            float blend = UnityEngine.Mathf.Min((strength - 1f) / (rb.mass < 2f ? 7f : 20f), 0.9f);
            item.Grip = UnityEngine.Mathf.Lerp(strength, reduced, blend);
            strength = item.grabStrength;
            reduced = strength / (1f + strength);
            item.Torque = UnityEngine.Mathf.Lerp(strength, reduced, blend);
        }
    }

    // Installed-game override ordering for comparison with native level-1 guns.
    public void PhysicsGrabbingGunFixture()
    {
        foreach (PhysGrabber item in playerGrabbing)
        {
            float strength = item.grabStrength;
            if (item.playerAvatar.isTumbling || overrideExtraGrabStrengthDisable) strength = 1;
            if (overrideMinGrabStrengthTimer > 0) strength = Math.Max(strength, overrideMinGrabStrength + strength / 5);
            if (overrideGrabStrengthTimer > 0) strength = overrideGrabStrength;
            if (item.overrideGrabStrength != -1) strength = item.overrideGrabStrength;
            float reduced = strength / (1f + UnityEngine.Mathf.Min(strength, 30f));
            float blend = UnityEngine.Mathf.Min((strength - 1f) / (rb.mass < 2f ? 7f : 20f), 0.9f);
            item.Grip = UnityEngine.Mathf.Lerp(strength, reduced, blend);

            strength = item.grabStrength;
            if (overrideExtraTorqueStrengthDisable || item.playerAvatar.isTumbling) strength = 1;
            if (overrideMinTorqueStrengthTimer > 0) strength = Math.Max(strength, overrideMinTorqueStrength + strength / 5);
            strength = Math.Max(strength, overrideTorqueStrength);
            reduced = strength / (1f + strength);
            blend = UnityEngine.Mathf.Min((strength - 1f) / (rb.mass < 2f ? 7f : 20f), 0.9f);
            item.Torque = UnityEngine.Mathf.Lerp(strength, reduced, blend);
        }
    }
}
public sealed class ItemAttributes { }
public sealed class ItemMelee
{
    public static bool HoldingPatchesEnabled;
    private readonly PhysGrabObject physGrabObject;
    public ItemMelee(PhysGrabObject owner) => physGrabObject = owner;
    public bool Rotate, Attack, ThrowWhileHolding;
    public float MassRatio = 1f, CustomTorque = 0.4f;
    public Action? DuringHolding;
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    public float MeleeStrengthBonus(float divisor)
    {
        float result = 0;
        if (HoldingPatchesEnabled && !LifterMeleeHoldingPatch.HoldingBonus(this, divisor, ref result)) return result;
        if (physGrabObject.playerGrabbing.Count == 0) return 0;
        float level = Math.Max(0, (physGrabObject.playerGrabbing[0].grabStrength - 1f) / 0.2f);
        return level / (level + divisor);
    }
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    public float MeleeStrengthScale(float amplitude) => 1f + amplitude * MeleeStrengthBonus(5f);
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    public void GrabOverridesLogic()
    {
        ItemMelee? previous = null;
        if (HoldingPatchesEnabled) LifterMeleeHoldingPatch.BeginHolding(this, out previous);
        try { GrabOverridesBody(); }
        finally { if (HoldingPatchesEnabled) LifterMeleeHoldingPatch.EndHolding(previous); }
    }
    private void GrabOverridesBody()
    {
        float scale = MeleeStrengthScale(1f);
        float attackScale = Attack ? MeleeStrengthScale(0.5f) : 1f;
        if (!Rotate && physGrabObject.playerGrabbing.Count > 0)
        {
            physGrabObject.overrideMinGrabStrength = (17f + 42f * MeleeStrengthBonus(90f)) * MassRatio;
            physGrabObject.overrideMinGrabStrengthTimer = 0.1f;
            physGrabObject.overrideExtraGrabStrengthDisable = physGrabObject.overrideExtraTorqueStrengthDisable = true;
            physGrabObject.overrideTorqueStrength = CustomTorque * MassRatio * scale * attackScale;
            physGrabObject.overrideTorqueStrengthTimer = 0.1f;
        }
        if (Rotate)
        {
            physGrabObject.overrideMinTorqueStrength = 4f * MassRatio * scale;
            physGrabObject.overrideMinTorqueStrengthTimer = 0.1f;
        }
        DuringHolding?.Invoke();
        if (ThrowWhileHolding) throw new InvalidOperationException("holding fixture failure");
    }
}
namespace REPOJP.StageRoles
{
    internal sealed class Entry<T>(T value) { internal T Value = value; }
    internal sealed class StageRolesConfig
    {
        internal Entry<bool> Enabled = new(true);
    internal Entry<float> LifterHeavyGripMultiplier { get; } = new(1f);
    internal Entry<float> LifterHeavyRotationMultiplier { get; } = new(1f);
    internal Entry<int> LifterLightItemStrengthLevel { get; } = new(1);
    }
    internal sealed partial class StageRoleController
    {
        internal StageRolesConfig _config = new();
        internal bool Ready = true, Authority = true;
        internal bool RoleAssignmentsReady => Ready && Authority;
        internal bool PlayerHasRole(PlayerAvatar player, StageRole role) => player.Role == role || player.Role == StageRole.Superbot;
    }
    internal static class PlayerState { internal static bool IsLiving(PlayerAvatar player) => player.Living; }
    internal sealed class StageRolesPlugin
    {
        internal static StageRolesPlugin Instance = new();
        internal static Logger ModLogger = new();
        internal StageRoleController Controller = new();
    }
    internal sealed class Logger
    {
        internal void LogWarning(object message) => Console.WriteLine(message);
        internal void LogDebug(object message) { }
    }
}
