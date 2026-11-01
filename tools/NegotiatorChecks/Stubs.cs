namespace UnityEngine
{
    public class MonoBehaviour { }
    public enum FindObjectsSortMode { InstanceID }
    public static class Object
    {
        public static PhysGrabObject[] Items = Array.Empty<PhysGrabObject>();
        public static T[] FindObjectsByType<T>(FindObjectsSortMode mode) => Items.OfType<T>().ToArray();
    }
    public struct Vector3(float x, float y, float z)
    {
        public float x = x, y = y, z = z;
        public float magnitude => MathF.Sqrt(x*x+y*y+z*z);
        public static Vector3 operator -(Vector3 a, Vector3 b) => new(a.x-b.x,a.y-b.y,a.z-b.z);
    }
    public class Transform { public Vector3 position; }
    public static class Time { public static float unscaledTime, time; }
    public static class Random { public static int Range(int min, int max) => min; }
}
public class PlayerAvatar
{
    public string Id = "a", Name = "Player";
    public bool Alive = true, InTruck;
    public UnityEngine.Transform transform = new();
    public void PlayerDeathRPC(int id) { }
}
public class PhysGrabObject
{
    public ItemAttributes Item = new();
    public ValuableObject? Valuable;
    public RoomVolumeCheck Rooms = new();
    public PlayerAvatar? Holder;
    public T? GetComponent<T>() where T : class => Item as T ?? Valuable as T ?? Rooms as T;
    public T? GetComponentInChildren<T>() where T : class => null;
    public T? GetComponentInParent<T>() where T : class => null;
}
public class ValuableVolume { public enum Type { Tiny, Small, Medium, Big, Wide, Tall, VeryTall } }
public class ValuableObject { public ValuableVolume.Type volumeType; public float dollarValueCurrent = 100; }
public class RoomVolume { public bool Truck, Extraction; }
public class RoomVolumeCheck
{
    public List<RoomVolume> CurrentRooms = new();
    public Action? Refresh;
    public void CheckSet() => Refresh?.Invoke();
}
public class ItemAttributes { public int value = 30; }
public class ShopKeeper
{
    public enum State { NoticeItem, ScanObject }
    public static ShopKeeper? instance;
    public int itemDiscountAttempts;
    public PhysGrabObject? itemReactObject;
}
public class StatsManager { public void ResetAllStats() { } }
public class GameManager { public static GameManager? instance = new(); }
public class RunManager { public static RunManager? instance = new(); }
public class LevelGenerator { public static LevelGenerator? Instance = new(); }
public static class SemiFunc
{
    public static bool Authority = true, Shop, MainMenu, LobbyMenu, Arena, Generated = true;
    public static List<PlayerAvatar> Players = new();
    public static bool IsMasterClientOrSingleplayer() => Authority;
    public static bool IsMainMenu() => MainMenu;
    public static bool RunIsLobbyMenu() => LobbyMenu;
    public static bool RunIsArena() => Arena;
    public static bool RunIsShop() => Shop;
    public static bool LevelGenDone() => Generated;
    public static List<PlayerAvatar> PlayerGetList() => Players;
}
namespace REPOJP.StageRoles
{
    internal enum StageRole { Negotiator, Runner, Superbot, Influenza }
    internal enum AbilityMetric { Negotiation }
    internal readonly record struct AbilityValue(AbilityMetric Metric, int Remaining, int Limit);
    internal record AbilitySnapshot(string SteamId, StageRole Assigned, StageRole Effective, IReadOnlyList<AbilityValue> Values);
    internal readonly record struct RoleSnapshot(string SteamId, string PlayerName, StageRole Role, StageRole EffectiveRole);
    internal class RoleAssignment(string id, StageRole role) { internal string SteamId = id; internal StageRole Role = role; internal PlayerAvatar Player = SemiFunc.Players.First(p => p.Id == id); }
    internal static class RoleCatalog { internal static bool HasCapability(StageRole role, StageRole capability) => role == capability || role == StageRole.Superbot; }
    internal class Entry<T>(T value) { internal T Value = value; }
    internal class StageRolesConfig
    {
        internal Entry<bool> Enabled = new(true);
        internal Entry<float> NegotiatorDeliveryCarryDistance = new(20f);
        internal Entry<string> NegotiatorDeliveryAllowedSizes = new("Big,Wide,Tall,VeryTall");
        internal Entry<int> NegotiatorEnemyTier = new(3);
    }
    internal static class UtilityRoleRuntime { internal static bool IsHeldBy(PhysGrabObject item, RoleAssignment a) => item.Holder == a.Player; }
    internal static class PlayerState { internal static bool IsInTruck(PlayerAvatar p) => p.InTruck; internal static bool IsLiving(PlayerAvatar? p) => p?.Alive == true; }
    internal static class PlayerIdentity
    {
        internal static string SteamId(PlayerAvatar? p) => p?.Id ?? "";
        internal static string Name(PlayerAvatar p) => p.Name;
    }
    internal static class RoleAssignmentSync
    {
        internal static IReadOnlyList<RoleSnapshot> Last = Array.Empty<RoleSnapshot>();
        internal static void PublishSnapshots(IReadOnlyList<RoleSnapshot> roles) => Last = roles;
    }
    internal static class RoleAbilitySync
    {
        internal static IReadOnlyList<AbilitySnapshot> Last = Array.Empty<AbilitySnapshot>();
        internal static void Publish(IReadOnlyList<AbilitySnapshot> values) => Last = values;
    }
    internal class RoleNotifier
    {
        internal static bool Ready = true;
        internal static bool CanNotify() => Ready;
        internal readonly List<(PlayerAvatar Player, string Message)> Sent = new();
        internal void NotifyConditional(PlayerAvatar player, string message, Func<bool> valid)
        { if (valid()) Sent.Add((player, message)); }
    }
    internal sealed partial class StageRoleController { internal RoleNotifier _notifier = new(); }
    internal class StageRolesPlugin
    {
        internal static StageRolesPlugin Instance = new();
        internal NegotiatorRuntime Negotiator = new();
        internal StageRoleController Controller = new();
    }
}
