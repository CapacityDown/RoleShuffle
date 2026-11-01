namespace UnityEngine
{
    public class MonoBehaviour { }
    public static class Time { public static float unscaledTime; }
    public static class Random { public static int Range(int min, int max) => min; }
}
public class PlayerAvatar
{
    public string Id = "a", Name = "Player";
    public bool Alive = true;
    public void PlayerDeathRPC(int id) { }
}
public class PhysGrabObject
{
    public ItemAttributes Item = new();
    public T? GetComponent<T>() where T : class => Item as T;
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
    internal class RoleAssignment(string id, StageRole role) { internal string SteamId = id; internal StageRole Role = role; }
    internal static class RoleCatalog { internal static bool HasCapability(StageRole role, StageRole capability) => role == capability || role == StageRole.Superbot; }
    internal class Entry<T>(T value) { internal T Value = value; }
    internal class StageRolesConfig { internal Entry<bool> Enabled = new(true); }
    internal static class PlayerState { internal static bool IsLiving(PlayerAvatar? p) => p?.Alive == true; }
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
