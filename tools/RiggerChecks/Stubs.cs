namespace UnityEngine
{
    public static class Time { public static float deltaTime { get; set; } }
    public static class Mathf { public static float Clamp(float value, float min, float max) => Math.Clamp(value, min, max); public static int RoundToInt(float value) => (int)Math.Round(value); }
}
public class PlayerAvatar { public bool Alive = true; internal REPOJP.StageRoles.StageRole Role = REPOJP.StageRoles.StageRole.Rigger; }
public static class SemiFunc { public static bool Authority = true; public static bool IsMasterClientOrSingleplayer() => Authority; }
public class HurtCollider
{
    public int enemyDamage = 20, playerDamage = 10;
    public float applyForce;
    public PlayerAvatar? playerCausingHurtOverride;
}
public class ItemMeleeWhip
{
    public bool isActiveAndEnabled = true;
    public HurtCollider crackHurtCollider = new();
    public PlayerAvatar? holder;
    public float durabilityPerFling = 5;
    public class CrackHitWindow { public float durability = 5; public PlayerAvatar? holder; }
}
namespace REPOJP.StageRoles
{
    internal enum StageRole { Rigger, Runner, Superbot, Imitator }
    internal sealed class Entry<T>(T value) { internal T Value = value; }
    internal sealed class StageRolesConfig
    {
        internal Entry<bool> Enabled = new(true);
        internal Entry<float> RiggerEnemyHoldMultiplier = new(1.5f), RiggerDurabilityMultiplier = new(.5f), RiggerEnemyDamageMultiplier = new(2f);
    }
    internal static class PlayerState { internal static bool IsLiving(PlayerAvatar? player) => player?.Alive == true; }
    internal sealed partial class StageRoleController
    {
        internal readonly StageRolesConfig _config = new();
        internal bool Ready = true;
        private bool IsAuthority() => SemiFunc.Authority;
        internal bool PlayerHasRole(PlayerAvatar? player, StageRole role) => Ready && player?.Role is StageRole.Rigger or StageRole.Superbot;
    }
    internal sealed class StageRolesPlugin
    {
        internal static StageRolesPlugin? Instance = new();
        internal StageRoleController? Controller = new();
    }
}
