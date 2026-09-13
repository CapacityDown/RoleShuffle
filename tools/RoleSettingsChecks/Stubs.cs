using BepInEx.Configuration;
namespace UnityEngine
{
    public static class Mathf
    {
        public static int Clamp(int value, int min, int max) => Math.Clamp(value, min, max);
        public static float Clamp(float value, float min, float max) => Math.Clamp(value, min, max);
        public static int Min(int a, int b) => Math.Min(a, b);
        public static int Max(int a, int b) => Math.Max(a, b);
    }
}
public sealed class GameManager { public static GameManager? instance = new(); }
public static class SemiFunc { public static bool Multiplayer; public static bool IsMultiplayer() => Multiplayer; }
namespace ExitGames.Client.Photon { public sealed class Hashtable : Dictionary<string, object> { } }
namespace Photon.Pun
{
    public static class PhotonNetwork { public static bool IsMasterClient; public static TestRoom? CurrentRoom; }
    public sealed class TestRoom
    {
        public Properties CustomProperties = new();
        public void SetCustomProperties(ExitGames.Client.Photon.Hashtable values)
        { foreach (var pair in values) CustomProperties[pair.Key] = pair.Value; }
    }
    public sealed class Properties : Dictionary<string, object>
    { public new bool TryGetValue(string key, out object value) => base.TryGetValue(key, out value!); }
}
namespace TMPro { public enum FontStyles { Normal, Bold } }
namespace MenuLib.MonoBehaviors { public sealed class REPOPopupPage { } }
namespace REPOJP.StageRoles
{
    internal sealed class TestLogger
    {
        internal void LogInfo(object value) { }
        internal void LogWarning(object value) { }
        internal void LogError(object value) { }
        internal void LogDebug(object value) { }
    }
    internal sealed class StageRolesPlugin
    {
        internal static TestLogger ModLogger = new();
        internal static StageRolesPlugin Instance = new();
        internal RoleSelectionSettings RoleSettings = null!;
        internal BaseUpgradeSelectionSettings BaseUpgradeSettings = null!;
    }
    internal static class RoleCatalog
    {
        internal static IReadOnlyList<UpgradeGrant> ConfiguredBaseUpgrades(StageRolesConfig config) => new[] { new UpgradeGrant("Health", "Health", 1) };
        internal static IReadOnlyList<UpgradeGrant> BaseUpgrades(StageRolesConfig config) => new[] { new UpgradeGrant("Health", "Health", 4) };
        internal static IReadOnlyList<StageRole> AllRoles = Enum.GetValues<StageRole>();
        internal static bool IsSecretRole(StageRole role) => role is StageRole.Superbot or StageRole.Disaster;
        internal static string DisplayName(StageRole role) => role switch
        { StageRole.Superbot => "???1", StageRole.Disaster => "???2", _ => role.ToString() };
    }
    internal readonly record struct UpgradeGrant(string CommandName, string DictionaryName, int Level);
    internal static class BaseUpgradeBonusStore { internal static int Get(string name) => 3; }
    internal static class RoleGuideSync
    {
        internal static HashSet<StageRole> Remote = new();
        internal static bool Available = true;
        internal static string VisibilitySignature(StageRolesConfig config) => RoleSelectionSettings.CanEdit
            ? string.Join(',', RoleCatalog.AllRoles.Where(config.RoleIsEnabled)) : Available ? string.Join(',', Remote) : "*";
        internal static bool IsVisible(StageRole role, StageRolesConfig config) => Remote.Contains(role);
    }
    internal sealed partial class RoleMenu
    {
        private enum RoleMenuView { Settings, Presets, BaseUpgrades, BaseUpgradeSettings, BaseUpgradePresets }
        private static StageRolesConfig _config = null!;
        private static MenuLib.MonoBehaviors.REPOPopupPage _openPage = new();
        private static RoleMenuView _activeView;
        private static RoleGuideLanguage _guideLanguage;
        private static string _utilityMessage = "";
        private static string _openSignature = "";
        private const float GuideFontSize = 18, GuideLineHeight = 25;
        private static bool UseLanguageFont => _guideLanguage != RoleGuideLanguage.English;
        private static object MeasurementText(object page) => page;
        private static float ContentWidth(object page) => 480;
        private static string[] WrapGuideText(object measurement, string value, float width, RoleGuideLanguage language) => new[] { value };
        private static string Localized(string text) => RoleText.Get(text, _guideLanguage);
        private static void SwitchView(MenuLib.MonoBehaviors.REPOPopupPage page, RoleMenuView view)
        { _activeView = view; if (view is RoleMenuView.BaseUpgradeSettings or RoleMenuView.BaseUpgradePresets) RefreshBaseUpgradeSettingsRows(page); else RefreshRoleSettingsRows(page); }
        private static string DisplayUpgradeName(string name) => name;
        internal record RoleMenuEntry(string Text, float Size, TMPro.FontStyles Style, float Height, bool Wrap,
            bool Language, Action? OnClick = null, StageRole? Role = null, bool Secret = false);
        internal static IReadOnlyList<RoleMenuEntry> Entries = Array.Empty<RoleMenuEntry>();
        private static void ApplyEntries(object page, IReadOnlyList<RoleMenuEntry> entries) => Entries = entries;
        internal static void Show(StageRolesConfig config, bool presets = false, RoleGuideLanguage language = RoleGuideLanguage.English)
        { _config = config; _guideLanguage = language; _activeView = presets ? RoleMenuView.Presets : RoleMenuView.Settings; RefreshRoleSettingsRows(_openPage); }
        internal static void ShowBase(StageRolesConfig config, bool presets = false, RoleGuideLanguage language = RoleGuideLanguage.English)
        { _config = config; _guideLanguage = language; _activeView = presets ? RoleMenuView.BaseUpgradePresets : RoleMenuView.BaseUpgradeSettings; RefreshBaseUpgradeSettingsRows(_openPage); }
        internal static void RefreshBaseIfChanged()
        { if (_openSignature != BaseUpgradeSettingsSignature()) RefreshBaseUpgradeSettingsRows(_openPage); }
    }
}
