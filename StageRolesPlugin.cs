using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace REPOJP.StageRoles;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInDependency(RepoConfigGuid, BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency(MenuLibGuid, BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency(StageFluxGuid, BepInDependency.DependencyFlags.SoftDependency)]
public sealed class StageRolesPlugin : BaseUnityPlugin
{
    public const string PluginGuid = "REPOJP.RoleShuffle";
    public const string PluginName = "RoleShuffle";
    public const string PluginVersion = "4.5.0";
    public const string StageFluxGuid = "REPOJP.StagePhysicsEvents";
    public const string RepoConfigGuid = "nickklmao.repoconfig";
    public const string MenuLibGuid = "nickklmao.menulib";

    private Harmony? _harmony;
    private GameObject? _controllerObject;
    private object? _guidePublishedRoom;
    private string _publishedBaseUpgradeSignature = string.Empty;
    private float _nextBaseUpgradePublishAt;
    private bool _settingsPublishPending;
    private float _settingsPublishAt;
    private float _nextPrivacyRefresh;
    internal RoleBugReport BugReport { get; private set; } = null!;
    internal void SaveLocalSettings() => Config.Save();
    internal void CreateBugReport() => BugReport.Create(Config);

    internal static StageRolesPlugin Instance { get; private set; } = null!;
    internal static ManualLogSource ModLogger { get; private set; } = null!;
    internal StageRolesConfig Settings { get; private set; } = null!;
    internal RoleSelectionSettings RoleSettings { get; private set; } = null!;
    internal BaseUpgradeSelectionSettings BaseUpgradeSettings { get; private set; } = null!;
    internal StageRoleController Controller { get; private set; } = null!;
    internal BaseUpgradeDrawRuntime BaseUpgradeDraw { get; private set; } = null!;

    private void Awake()
    {
        Instance = this;
        ModLogger = Logger;
        BugReport = new RoleBugReport();
        BepInEx.Logging.Logger.Listeners.Add(BugReport);
        Settings = new StageRolesConfig(Config);
        RoleSettings = new RoleSelectionSettings(Settings, Config);
        BaseUpgradeSettings = new BaseUpgradeSelectionSettings(Settings, Config);
        Config.SettingChanged += ConfigSettingChanged;

        gameObject.hideFlags = HideFlags.HideAndDontSave;
        DontDestroyOnLoad(gameObject);

        _controllerObject = new GameObject("RoleShuffle_Controller");
        _controllerObject.hideFlags = HideFlags.HideAndDontSave;
        _controllerObject.transform.SetParent(transform, false);
        Controller = _controllerObject.AddComponent<StageRoleController>();
        Controller.Initialize(Settings);
        RoleHud hud = gameObject.AddComponent<RoleHud>();
        hud.Initialize(Settings);
        RoleMenu roleMenu = gameObject.AddComponent<RoleMenu>();
        roleMenu.Initialize(Settings);
        gameObject.AddComponent<RoleSyncStatus>().Initialize(Settings);
        BaseUpgradeDraw = gameObject.AddComponent<BaseUpgradeDrawRuntime>();
        BaseUpgradeDraw.Initialize(Settings);
        RoleTestCommandService testCommands =
            gameObject.AddComponent<RoleTestCommandService>();
        testCommands.Initialize(Settings, Controller);

        _harmony = new Harmony(PluginGuid);
        try
        {
            _harmony.PatchAll(typeof(LifecyclePatches));
            _harmony.PatchAll(typeof(RoleExpressionPatches));
            _harmony.PatchAll(typeof(NinjaVisionPatch));
            _harmony.PatchAll(typeof(NinjaVoiceInvestigationPatch));
            _harmony.PatchAll(typeof(NinjaLandingInvestigationPatch));
            _harmony.PatchAll(typeof(NinjaFootstepInvestigationPatch));
            _harmony.PatchAll(typeof(InfluencerFootstepInvestigationPatch));
            _harmony.PatchAll(typeof(InfluencerLandingInvestigationPatch));
            _harmony.PatchAll(typeof(InfluencerVoiceInvestigationPatch));
            _harmony.PatchAll(typeof(InfluencerInvestigationRadiusPatch));
            _harmony.PatchAll(typeof(EngineerTrapUpdatePatches));
            _harmony.PatchAll(typeof(EngineerEffectActivationPatches));
            _harmony.PatchAll(typeof(ShopPatches));
            _harmony.PatchAll(typeof(NotificationEnemyReactionPatches));
            _harmony.PatchAll(typeof(HunterBatteryPatches));
            _harmony.PatchAll(typeof(MageStaffProjectilePatch));
            _harmony.PatchAll(typeof(MageStaffImpactPatch));
            _harmony.PatchAll(typeof(MageStaffVoidDurationPatch));
            _harmony.PatchAll(typeof(MageStaffBeamDurationPatch));
            _harmony.PatchAll(typeof(RoleGuideScrollPatch));
            LifterStrengthPatch.Install(_harmony);
            SceneManager.activeSceneChanged += ActiveSceneChanged;
            Logger.LogInfo($"{PluginName} {PluginVersion} loaded.");
        }
        catch (Exception exception)
        {
            Logger.LogError($"Failed to apply Harmony patches: {exception}");
            _harmony.UnpatchSelf();
            Controller.enabled = false;
        }
    }

    private void ActiveSceneChanged(Scene previous, Scene current)
    {
        RoleHudTestPreview.Clear();
        RoleHudEditor.Instance?.Close(false);
        BaseUpgradeHistory.ClearDisplayPreview();
        Controller?.StageEnding();
    }

    private void Update()
    {
        RoleGuideFont.InstallNativeNameFallback();
        if (Time.unscaledTime >= _nextPrivacyRefresh)
        { _nextPrivacyRefresh = Time.unscaledTime + 1f; BugReport.RememberPlayers(); }
        if (GameManager.instance == null) return;
        if (_settingsPublishPending && Time.unscaledTime >= _settingsPublishAt)
        {
            _settingsPublishPending = false;
            RoleGuideSync.Publish(Settings);
            BaseUpgradeSync.Publish(Settings);
            _publishedBaseUpgradeSignature = BaseUpgradeSync.LocalPublishSignature(Settings);
        }
        object? currentRoom = PhotonNetwork.CurrentRoom;
        if (!SemiFunc.IsMultiplayer() || !PhotonNetwork.IsMasterClient ||
            currentRoom == null)
        {
            _guidePublishedRoom = null;
            _publishedBaseUpgradeSignature = string.Empty;
            return;
        }
        if (!ReferenceEquals(_guidePublishedRoom, currentRoom))
        {
            RoleGuideSync.Publish(Settings);
            BaseUpgradeSync.Publish(Settings);
            _guidePublishedRoom = currentRoom;
            _publishedBaseUpgradeSignature =
                BaseUpgradeSync.LocalPublishSignature(Settings);
            _nextBaseUpgradePublishAt = Time.unscaledTime + 0.5f;
        }

        if (Time.unscaledTime < _nextBaseUpgradePublishAt)
        {
            return;
        }
        _nextBaseUpgradePublishAt = Time.unscaledTime + 0.5f;
        string currentSignature = BaseUpgradeSync.LocalPublishSignature(Settings);
        if (string.Equals(
                currentSignature,
                _publishedBaseUpgradeSignature,
                StringComparison.Ordinal))
        {
            return;
        }
        BaseUpgradeSync.Publish(Settings);
        _publishedBaseUpgradeSignature = currentSignature;
    }

    private void ConfigSettingChanged(object sender, SettingChangedEventArgs args)
    {
        if (!Settings.SetRoleCommandEnabled.Value)
        {
            BaseUpgradeHistory.ClearDisplayPreview();
            RoleHudTestPreview.Clear();
        }
        if (args.ChangedSetting.Definition.Section is "HUD" or "UI") return;
        RoleGuideSync.Invalidate();
        _settingsPublishPending = true;
        _settingsPublishAt = Time.unscaledTime + 0.15f;
    }

    private void OnDestroy()
    {
        RoleHudTestPreview.Clear();
        RoleHudEditor.Instance?.Close(false);
        BaseUpgradeHistory.ClearDisplayPreview();
        BepInEx.Logging.Logger.Listeners.Remove(BugReport);
        BugReport?.Dispose();
        Config.SettingChanged -= ConfigSettingChanged;
        SceneManager.activeSceneChanged -= ActiveSceneChanged;
        Controller?.Shutdown();
        BaseUpgradeDraw?.Shutdown();
        _harmony?.UnpatchSelf();
        StageFluxCompatibility.ClearNotifications();
        RoleGuideSync.Invalidate();
        RoleAssignmentSync.ResetCache();
        RoleEmblems.Shutdown();
        if (_controllerObject != null)
        {
            Destroy(_controllerObject);
        }
    }
}
