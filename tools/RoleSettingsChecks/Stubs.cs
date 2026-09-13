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
public sealed class GameManager
{
    public enum LobbyTypes { Private, Public }
    public static GameManager? instance = new();
    private LobbyTypes lobbyType = LobbyTypes.Private;
    public LobbyTypes LobbyForTests { get => lobbyType; set => lobbyType = value; }
}
public sealed class GameDirector { public static GameDirector? instance = new(); }
public sealed class StatsManager
{
    public static StatsManager? instance = new();
    public Dictionary<string, int> runStats = new();
    public List<GameManager.LobbyTypes> savedLobbyTypes = new() { GameManager.LobbyTypes.Private };
    private string saveFileCurrent = "save-a.json";
    private bool saveFileReady = true;
    public bool ReadyForTests { get => saveFileReady; set => saveFileReady = value; }
    public static string DirectoryPath = "";
    public bool FailSave;
    public bool FailCreate;
    public int Creates;
    public int Saves;
    public void ResetAllStats()
    {
        saveFileReady = false;
        runStats.Clear();
        runStats["level"] = 0;
        REPOJP.StageRoles.GameSaveState.RecordRunReset(this);
    }
    public void SaveFileCreate(string saveFileName = "", bool saveIsDebug = false)
    {
        if (FailCreate) throw new IOException("Simulated native initialization failure");
        saveFileCurrent = saveFileName.Length > 0 ? saveFileName : $"fresh-run-{++Creates}.json";
        saveFileReady = true;
        REPOJP.StageRoles.GameSaveState.RecordSaveReady(this);
    }
    public void SaveFileSave()
    {
        if (FailSave) throw new IOException("Simulated disk failure");
        if (savedLobbyTypes.Contains(GameManager.instance!.LobbyForTests))
            File.WriteAllText(Path.Combine(DirectoryPath, saveFileCurrent), System.Text.Json.JsonSerializer.Serialize(runStats));
        Saves++;
        saveFileReady = true;
        REPOJP.StageRoles.GameSaveState.RecordSaveReady(this);
    }
    public void Load(string name)
    {
        saveFileReady = false;
        runStats.Clear();
        saveFileCurrent = name;
        string path = Path.Combine(DirectoryPath, name);
        if (File.Exists(path))
            foreach (var pair in System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, int>>(File.ReadAllText(path))!)
                runStats[pair.Key] = pair.Value;
        saveFileReady = true;
        REPOJP.StageRoles.GameSaveState.RecordSaveReady(this);
    }
}
public enum TestScene { LobbyMenu, Truck, Shop, Stage, Arena, Tutorial, MainMenu }
public sealed class RunManager
{
    public static RunManager? instance = new();
    public int levelsCompleted;
    public TestScene? levelCurrent = TestScene.LobbyMenu;
}
public static class SemiFunc
{
    public static bool Multiplayer;
    public static bool IsMultiplayer() => Multiplayer;
    public static bool RunIsLobbyMenu() => RunManager.instance?.levelCurrent == TestScene.LobbyMenu;
    public static bool RunIsLobby() => RunManager.instance?.levelCurrent == TestScene.Truck;
    public static bool RunIsShop() => RunManager.instance?.levelCurrent == TestScene.Shop;
}
namespace ExitGames.Client.Photon { public sealed class Hashtable : Dictionary<string, object> { } }
namespace Photon.Pun
{
    public static class PhotonNetwork
    {
        public static bool IsMasterClient;
        public static TestRoom? CurrentRoom;
        public static TestPlayer LocalPlayer = new();
        public static TestPlayer MasterClient = LocalPlayer;
    }
    public sealed class TestPlayer { public int ActorNumber = 1; }
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
        internal static IReadOnlyList<UpgradeGrant> ConfiguredBaseUpgrades(StageRolesConfig config) => RoleUpgradeScaling.Definitions.Select(definition =>
        {
            RoleUpgradeScaling.TryParse(config.BaseUpgradeLevelsEntry(definition.Name)!.Value, 1, 999999,
                definition.MaximumLevel, true, out var rules, out _);
            int level = rules.Where(r => r.Condition <= BaseUpgradeSelectionSettings.CurrentRunLevel).Select(r => r.Level).LastOrDefault();
            return new UpgradeGrant(definition.Name, definition.DictionaryName, level);
        }).ToArray();
        internal static IReadOnlyList<UpgradeGrant> BaseUpgrades(StageRolesConfig config) => ConfiguredBaseUpgrades(config)
            .Select(g => new UpgradeGrant(g.CommandName, g.DictionaryName,
                BaseUpgradeManualStore.EffectiveLevel(g.DictionaryName, g.Level, g.CommandName == "MapPlayerCount" ? 1 : 200,
                    config.BaseUpgradeManualAdjustmentEnabled.Value))).ToArray();
        internal static IReadOnlyList<StageRole> AllRoles = Enum.GetValues<StageRole>();
        internal static bool IsSecretRole(StageRole role) => role is StageRole.Superbot or StageRole.Disaster;
        internal static string DisplayName(StageRole role) => role switch
        { StageRole.Superbot => "???1", StageRole.Disaster => "???2", _ => role.ToString() };
    }
    internal readonly record struct UpgradeGrant(string CommandName, string DictionaryName, int Level);
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
        private enum RoleMenuView { Settings, Presets, BaseUpgrades, BaseUpgradeSettings }
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
        { _activeView = view; if (view == RoleMenuView.BaseUpgrades) RefreshBaseUpgradeRows(page); else if (view == RoleMenuView.BaseUpgradeSettings) RefreshBaseUpgradeSettingsRows(page); else RefreshRoleSettingsRows(page); }
        private static string DisplayUpgradeName(string name) => name;
        private static string SignedValue(int value) => value.ToString("+0;-0;0", System.Globalization.CultureInfo.InvariantCulture);
        internal record RoleMenuEntry(string Text, float Size, TMPro.FontStyles Style, float Height, bool Wrap,
            bool Language = false, Action? OnClick = null, StageRole? Role = null, bool Secret = false, bool isControl = false,
            bool emblemGrayedOut = false, RoleMenuAdjustment? adjustment = null, bool isOff = false, bool separatorBefore = false)
        { internal RoleMenuAdjustment? Adjustment => adjustment; }
        internal static IReadOnlyList<RoleMenuEntry> Entries = Array.Empty<RoleMenuEntry>();
        private static void ApplyEntries(object page, IReadOnlyList<RoleMenuEntry> entries) => Entries = entries;
        internal static void Show(StageRolesConfig config, bool presets = false, RoleGuideLanguage language = RoleGuideLanguage.English)
        { _config = config; _guideLanguage = language; _activeView = presets ? RoleMenuView.Presets : RoleMenuView.Settings; RefreshRoleSettingsRows(_openPage); }
        internal static void ShowBase(StageRolesConfig config, RoleGuideLanguage language = RoleGuideLanguage.English)
        { _config = config; _guideLanguage = language; _activeView = RoleMenuView.BaseUpgradeSettings; RefreshBaseUpgradeSettingsRows(_openPage); }
        internal static void RefreshBaseIfChanged()
        { if (_openSignature != BaseUpgradeSettingsSignature()) RefreshBaseUpgradeSettingsRows(_openPage); }
        internal static void ShowLevels(StageRolesConfig config)
        { _config = config; _guideLanguage = RoleGuideLanguage.English; _activeView = RoleMenuView.BaseUpgrades; RefreshBaseUpgradeRows(_openPage); }
        internal static void RefreshLevelsIfChanged()
        { if (_openSignature != BaseUpgradePageSignature()) RefreshBaseUpgradeRows(_openPage); }
    }
}
