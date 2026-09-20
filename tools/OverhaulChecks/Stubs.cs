using System.Reflection;
namespace UnityEngine
{
    public static class Time { public static float time; public static float unscaledTime; }
    public class Object
    {
        public static PhysGrabObject[] Items = Array.Empty<PhysGrabObject>();
        public static int Scans;
        public static T[] FindObjectsOfType<T>() { Scans++; return Items.Cast<T>().ToArray(); }
    }
    public readonly record struct Vector3(float x, float y, float z)
    {
        public float sqrMagnitude => x*x + y*y + z*z;
        public static Vector3 operator -(Vector3 a, Vector3 b) => new(a.x-b.x,a.y-b.y,a.z-b.z);
        public static float Distance(Vector3 a, Vector3 b) => MathF.Sqrt((a-b).sqrMagnitude);
    }
    public sealed class Transform { public Vector3 position; }
}
namespace HarmonyLib
{
    public static class AccessTools
    {
        public static FieldInfo? Field(Type type, string name) => type.GetField(name, BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public);
    }
}
namespace ExitGames.Client.Photon { public class Hashtable : Dictionary<object, object?> { } }
namespace Photon.Pun
{
    public enum RpcTarget { All, Others }
    public class PhotonView
    {
        public bool IsMine = true;
        public static bool FailSend;
        public static int Sends;
        public void RPC(string method, RpcTarget target, params object[] args)
        {
            Sends++;
            if (FailSend) throw new InvalidOperationException("Simulated peer send failure");
            if (target == RpcTarget.All) PunManager.instance.TesterUpgradeCommandRPC((string)args[0], (string)args[1], (int)args[2]);
        }
    }
    public class Room
    {
        public ExitGames.Client.Photon.Hashtable CustomProperties = new();
        public int Publications;
        public void SetCustomProperties(ExitGames.Client.Photon.Hashtable values)
        { Publications++; foreach (var pair in values) { if (pair.Value == null) CustomProperties.Remove(pair.Key); else CustomProperties[pair.Key] = pair.Value; } }
    }
    public static class PhotonNetwork
    {
        private static bool _master = true;
        private static Room? _room;
        private static double _time;
        public static bool IsMasterClient { get { PhotonAccess.Read(); return _master; } set => _master = value; }
        public static Room? CurrentRoom { get { PhotonAccess.Read(); return _room; } set => _room = value; }
        public static double Time { get { PhotonAccess.Read(); return _time; } set => _time = value; }
    }
}
public static class PhotonAccess
{
    public static bool FailOnRead;
    public static int Reads;
    public static void Read()
    {
        Reads++;
        if (FailOnRead) throw new InvalidOperationException("Cleanup initialized Photon before gameplay.");
    }
}
public static class SemiFunc
{
    public static bool Multiplayer;
    public static bool GameReady = true;
    public static bool IsMultiplayer() => GameReady ? Multiplayer : throw new InvalidOperationException("GameManager was destroyed");
    public static bool IsMasterClientOrSingleplayer() => !Multiplayer || Photon.Pun.PhotonNetwork.IsMasterClient;
    public static PlayerAvatar? PlayerAvatarGetFromSteamID(string id) => null;
}
public sealed class PhysGrabber { public float grabStrength = 1; }
public sealed class PlayerAvatar
{
    public PhysGrabber physGrabber = new();
    public string Id = "p";
    public bool Living = true;
    public bool InTruck;
    public UnityEngine.Transform transform = new();
    public PlayerHealth playerHealth = new();
    public Photon.Pun.PhotonView photonView = new();
}
public sealed class PlayerHealth
{
    public int Health = 50;
    public int Maximum = 100;
    public int Requests;
    public Action<int>? OnHeal;
    public void HealOther(int amount, bool effect)
    { Requests++; if (OnHeal != null) OnHeal(amount); else Health = Math.Min(Maximum, Health + amount); }
}
public sealed class ValuableObject
{
    private float dollarValueCurrent = 100;
    public float Value { get => dollarValueCurrent; set => dollarValueCurrent = value; }
}
public sealed class PhysGrabObject
{
    public int Id;
    public ValuableObject? Valuable = new();
    public string HeldBy = "p";
    public UnityEngine.Vector3 centerPoint;
    public int GetInstanceID() => Id;
    public T? GetComponent<T>() where T:class => Valuable as T;
    public T? GetComponentInChildren<T>() where T:class => null;
    public T? GetComponentInParent<T>() where T:class => null;
}
namespace REPOJP.StageRoles
{
    internal sealed class Entry<T>(T value) { internal T Value { get; set; } = value; }
    internal sealed class StageRolesConfig
    {
        internal Entry<float> JoblessContractGrace = new(30);
        internal Entry<int> JoblessContractLimit = new(3);
        internal Entry<float> JoblessContractDistance = new(5);
        internal Entry<int> JoblessContractHeal = new(10);
        internal Entry<float> KingUpgradeRadius = new(8);
        internal Entry<int> KingSpeedBonus = new(1);
        internal Entry<int> KingRangeBonus = new(1);
        internal Entry<int> KingStrengthBonus = new(1);
    }
    internal sealed class RoleAssignment
    {
        internal PlayerAvatar Player = new();
        internal string SteamId => Player.Id;
        internal StageRole Role = StageRole.Jobless;
        internal RoleOverhaulState Overhaul = new();
        internal float JoblessDamageTimer;
    }
    internal readonly record struct RoleSnapshot(string SteamId, StageRole Role, StageRole EffectiveRole);
    internal static class PlayerState
    {
        internal static bool IsLiving(PlayerAvatar player) => player.Living && player.playerHealth.Health > 0;
        internal static bool IsInTruck(PlayerAvatar player) => player.InTruck;
        internal static bool TryGetCurrentHealth(PlayerAvatar player, out int health) { health = player.playerHealth.Health; return true; }
        internal static bool TryGetMaximumHealth(PlayerAvatar player, out int health) { health = player.playerHealth.Maximum; return true; }
    }
    internal sealed class RoleNotifier
    {
        internal int Notifications;
        internal void NotifyResponse(PlayerAvatar player, string message) => Notifications++;
    }
    internal static class UtilityRoleRuntime
    { internal static bool IsHeldBy(PhysGrabObject item, RoleAssignment assignment) => item.HeldBy == assignment.SteamId; }
    internal sealed class TestLog { internal void LogDebug(string message) { } internal void LogWarning(string message) { } }
    internal static class StageRolesPlugin { internal static TestLog ModLogger = new(); }
}

public sealed class StatsManager
{
    public static StatsManager instance = new();
    public Dictionary<string, Dictionary<string, int>> Players = new();
    public Dictionary<string, int> FetchPlayerUpgrades(string id) => Players.TryGetValue(id, out var levels) ? new(levels) : new();
    public int Add(string id, string command, int delta)
    {
        if (!Players.TryGetValue(id, out var levels)) Players[id] = levels = new();
        string key = "playerUpgrade" + command;
        return levels[key] = Math.Max(0, levels.GetValueOrDefault(key, 0) + delta);
    }
}
public sealed class PunManager
{
    public static PunManager instance = new();
    public T GetComponent<T>() where T : new() => new();
    public void TesterUpgradeCommandRPC(string id, string command, int delta) => StatsManager.instance.Add(id, command, delta);
    public int UpgradePlayerCrouchRest(string id, int delta) => StatsManager.instance.Add(id, "CrouchRest", delta);
    public int UpgradePlayerExtraJump(string id, int delta) => StatsManager.instance.Add(id, "ExtraJump", delta);
    public int UpgradePlayerHealth(string id, int delta) => StatsManager.instance.Add(id, "Health", delta);
    public int UpgradePlayerTumbleLaunch(string id, int delta) => StatsManager.instance.Add(id, "Launch", delta);
    public int UpgradeMapPlayerCount(string id, int delta) => StatsManager.instance.Add(id, "MapPlayerCount", delta);
    public int UpgradePlayerGrabRange(string id, int delta) => StatsManager.instance.Add(id, "Range", delta);
    public int UpgradePlayerSprintSpeed(string id, int delta) => StatsManager.instance.Add(id, "Speed", delta);
    public int UpgradePlayerEnergy(string id, int delta) => StatsManager.instance.Add(id, "Stamina", delta);
    public int UpgradePlayerGrabStrength(string id, int delta) => StatsManager.instance.Add(id, "Strength", delta);
    public int UpgradePlayerThrowStrength(string id, int delta) => StatsManager.instance.Add(id, "Throw", delta);
    public int UpgradePlayerTumbleWings(string id, int delta) => StatsManager.instance.Add(id, "TumbleWings", delta);
    public int UpgradePlayerTumbleClimb(string id, int delta) => StatsManager.instance.Add(id, "TumbleClimb", delta);
    public int UpgradeDeathHeadBattery(string id, int delta) => StatsManager.instance.Add(id, "DeathHeadBattery", delta);
}
namespace REPOJP.StageRoles { internal readonly record struct UpgradeGrant(string CommandName, string DictionaryName, int Level); }
