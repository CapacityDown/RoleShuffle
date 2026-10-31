namespace UnityEngine
{
    public static class Time { public static float time, unscaledTime; }
    public static class Mathf
    {
        public static float Clamp(float x, float min, float max) => Math.Clamp(x, min, max);
        public static int Clamp(int x, int min, int max) => Math.Clamp(x, min, max);
    }
    public readonly record struct Vector3(float x, float y, float z)
    {
        public float sqrMagnitude => x*x + y*y + z*z;
        public static Vector3 operator -(Vector3 a, Vector3 b) => new(a.x-b.x,a.y-b.y,a.z-b.z);
    }
    public class Transform { public Vector3 position; }
    public enum FindObjectsInactive { Include }
    public enum FindObjectsSortMode { InstanceID }
    public class Object
    {
        public static object[] Objects = Array.Empty<object>();
        public static T[] FindObjectsByType<T>(FindObjectsInactive _, FindObjectsSortMode __) => Objects.OfType<T>().ToArray();
    }
}
namespace Photon.Pun { public sealed class PhotonView { public bool IsMine = true; } }
public sealed class PlayerAvatar
{
    public Photon.Pun.PhotonView photonView = new();
    public PlayerHealth playerHealth = new();
    public UnityEngine.Transform transform = new();
    public bool Active = true;
}
public sealed class PlayerHealth
{
    public int Health = 50, Maximum = 100, Calls;
    public Action<int>? OnHeal;
    public void HealOther(int amount, bool effect)
    {
        Calls++;
        if (OnHeal != null) OnHeal(amount);
        else Health = Math.Min(Maximum, Health + amount);
    }
}
public static class SemiFunc
{
    public static bool Multiplayer;
    public static bool IsMultiplayer() => Multiplayer;
}
public class MusicalValuableLogic { }
public class ValuableMusicInstrument
{
    public enum Instrument { Harmonica, PanFlute, MouthHarp, Ocarina }
    public enum State { Idle, Grabbed, Attached, Detaching }
    public Instrument instrument;
    public bool isActiveAndEnabled = true;
    public float loudnessThreshold = 0.1f;
}
namespace REPOJP.StageRoles
{
    internal static class PlayerState
    {
        internal static bool IsLiving(PlayerAvatar? p) => p?.Active == true && p.playerHealth.Health > 0;
        internal static bool TryGetCurrentHealth(PlayerAvatar p, out int v) { v=p.playerHealth.Health; return v>=0; }
        internal static bool TryGetMaximumHealth(PlayerAvatar p, out int v) { v=p.playerHealth.Maximum; return v>0; }
    }
    internal class StageRoleController
    {
        internal int Notes, Continuous;
        internal PlayerAvatar? Performer;
        internal void InstrumentNotePlayed(int id) => Notes++;
        internal void InstrumentPlayed(PlayerAvatar p) { Continuous++; Performer=p; }
    }
    internal class StageRolesPlugin
    {
        internal static StageRolesPlugin Instance = new();
        internal StageRoleController Controller = new();
        internal static Logger ModLogger = new();
        internal class Logger { public void LogDebug(string _) { } }
    }
}
