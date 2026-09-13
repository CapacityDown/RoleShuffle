using Photon.Pun;
using REPOJP.StageRoles;

internal static class ManualAdjustmentLifecycleChecks
{
    internal static int Run(StageRolesConfig config, BaseUpgradeSelectionSettings service)
    {
        int checks = 0;
        void Check(bool value, string reason) { if (!value) throw new Exception(reason); checks++; }
        SemiFunc.Multiplayer = false;
        GameManager.instance = new();
        GameDirector.instance = new();
        RunManager.instance = new();
        StatsManager.instance = new();
        var stats = StatsManager.instance;
        const string health = "playerUpgradeHealth";
        config.BaseUpgradeManualAdjustmentEnabled.Value = true;
        config.BaseHealthLevels.Value = "1:2";
        stats.Load("defeated-run.json");
        string defeated = BaseUpgradeManualStore.SaveIdentity;
        Check(service.TryAdjustLevel("Health", 1, defeated), "Prepare a saved adjustment in the previous run");
        BaseUpgradeBonusStore.ApplyEffectiveDelta(health, 3, 2, 2, 200, true);
        stats.SaveFileSave();
        string defeatedPath = Path.Combine(StatsManager.DirectoryPath, defeated);
        string defeatedBytes = File.ReadAllText(defeatedPath);
        RoleMenu.ShowLevels(config);
        Action defeatedClick = RoleMenu.Entries.First(e => e.Adjustment != null).Adjustment!.Increase!;

        // Native ResetAllStats clears runStats and readiness but retains the old name.
        RunManager.instance.levelCurrent = TestScene.Arena;
        stats.ResetAllStats();
        Check(GameSaveState.CurrentName == defeated && BaseUpgradeManualStore.SaveIdentity.Length == 0 &&
            BaseUpgradeManualStore.Get(health) == 0 && BaseUpgradeBonusStore.Get(health) == 0,
            "Defeat clears manual/draw amounts without allowing edits in the arena");
        RunManager.instance.levelCurrent = TestScene.LobbyMenu;
        string pending = BaseUpgradeManualStore.SaveIdentity;
        Check(pending.Length > 0 && pending != defeated, "Restart lobby has a fresh adjustment identity");
        int saves = stats.Saves;
        defeatedClick();
        Check(stats.Creates == 0 && stats.Saves == saves && BaseUpgradeManualStore.Get(health) == 0,
            "A stale callback from the defeated run cannot initialize or edit its successor");
        RoleMenu.RefreshLevelsIfChanged();
        Check(RoleMenu.Entries.First(e => e.Adjustment != null).Adjustment!.Increase != null &&
            RoleMenu.Entries.All(e => e.Text != "Load a saved game to use +/- adjustments."),
            "Restart lobby refreshes enabled controls without the missing-save message");
        Check(service.CanAdjustLevel("Health", 1, pending) && stats.Creates == 0 && stats.Saves == saves,
            "Rendering and eligibility checks do not create a save");
        config.BaseUpgradeManualAdjustmentEnabled.Value = false;
        Check(!service.TryAdjustLevel("Health", 1, pending) && stats.Creates == 0, "Disabled manual editing does not create a save");
        config.BaseUpgradeManualAdjustmentEnabled.Value = true;
        config.BaseHealthLevels.Value = "1:200";
        Check(!service.TryAdjustLevel("Health", 1, pending) && stats.Creates == 0, "Rejected limit edits do not create a save");
        config.BaseHealthLevels.Value = "1:2";
        stats.FailCreate = true;
        bool failed = false;
        try { service.TryAdjustLevel("Health", 1, pending); } catch (IOException) { failed = true; }
        Check(failed && BaseUpgradeManualStore.Get(health) == 0 && stats.Saves == saves,
            "Failed native initialization leaves the adjustment untouched");
        stats.FailCreate = false;
        Check(service.TryAdjustLevel("Health", 1, pending) && stats.Creates == 1 && stats.Saves == saves + 1,
            "First restart-lobby edit initializes and saves exactly once");
        string fresh = BaseUpgradeManualStore.SaveIdentity;
        Check(fresh != defeated && fresh != pending && File.ReadAllText(defeatedPath) == defeatedBytes,
            "Fresh save does not overwrite the previous run");
        Check(BaseUpgradeManualStore.Get(health) == 1 && BaseUpgradeBonusStore.Get(health) == 0 &&
            config.BaseHealthLevels.Value == "1:2", "Fresh run preserves only its new adjustment and configuration");
        Check(!service.TryAdjustLevel("Health", 1, pending), "Pending callback expires after initialization");
        stats.Load(fresh);
        Check(BaseUpgradeManualStore.Get(health) == 1, "Restart-lobby adjustment survives a disk reload");
        Check(service.TryAdjustLevel("Health", -1, fresh) && stats.Creates == 1,
            "Later edits reuse the initialized save");

        RoleMenu.ShowLevels(config);
        foreach (TestScene scene in new[] { TestScene.Stage, TestScene.Arena, TestScene.Tutorial, TestScene.MainMenu })
        {
            RunManager.instance.levelCurrent = TestScene.LobbyMenu;
            RoleMenu.RefreshLevelsIfChanged();
            Action click = RoleMenu.Entries.First(e => e.Adjustment != null).Adjustment!.Increase!;
            saves = stats.Saves;
            RunManager.instance.levelCurrent = scene;
            click();
            Check(!service.CanAdjustLevel("Health", 1, fresh) && !service.CanAdjustLevel("Health", -1, fresh) &&
                !service.TryAdjustLevel("Health", 1, fresh) && !service.TryAdjustLevel("Health", -1, fresh) && stats.Saves == saves,
                "Stage/arena/other scenes reject both directions, including already-rendered callbacks");
            RoleMenu.RefreshLevelsIfChanged();
            Check(RoleMenu.Entries.Where(e => e.Adjustment != null).All(e => e.Adjustment!.ShowButtons &&
                e.Adjustment.Increase == null && e.Adjustment.Decrease == null) &&
                RoleMenu.Entries.Any(e => e.Text == "+/- is available only in the lobby, truck, or shop."),
                "Scene change disables controls and explains the restriction even when levels are unchanged");
        }
        foreach (TestScene scene in new[] { TestScene.LobbyMenu, TestScene.Truck, TestScene.Shop })
        {
            RunManager.instance.levelCurrent = scene;
            RoleMenu.RefreshLevelsIfChanged();
            Check(RoleMenu.Entries.First(e => e.Adjustment != null).Adjustment!.Increase != null &&
                service.TryAdjustLevel("Health", 1, fresh) && service.TryAdjustLevel("Health", -1, fresh),
                "Lobby, truck and shop restore both adjustment directions");
        }
        RunManager.instance.levelCurrent = null;
        Check(!service.TryAdjustLevel("Health", 1, fresh), "Missing scene cannot edit");

        RunManager.instance.levelCurrent = TestScene.LobbyMenu;
        stats.ResetAllStats();
        pending = BaseUpgradeManualStore.SaveIdentity;
        int creates = stats.Creates;
        SemiFunc.Multiplayer = true;
        PhotonNetwork.IsMasterClient = false;
        Check(!service.TryAdjustLevel("Health", 1, pending) && stats.Creates == creates,
            "Guests cannot initialize a post-reset save");
        PhotonNetwork.IsMasterClient = true;
        GameManager.instance.LobbyForTests = GameManager.LobbyTypes.Public;
        Check(BaseUpgradeManualStore.SaveIdentity.Length == 0 && !service.TryAdjustLevel("Health", 1, pending) && stats.Creates == creates,
            "Unsaved lobby types do not gain persistent edits through reset recovery");
        GameManager.instance.LobbyForTests = GameManager.LobbyTypes.Private;
        stats.Load(fresh);
        stats.ReadyForTests = false;
        Check(BaseUpgradeManualStore.SaveIdentity.Length == 0 && !service.TryAdjustLevel("Health", 1, pending),
            "Selecting a save clears reset eligibility; a later loading phase cannot initialize over it");
        stats.ReadyForTests = true;
        stats.ResetAllStats();
        pending = BaseUpgradeManualStore.SaveIdentity;
        stats.FailSave = true;
        failed = false;
        try { service.TryAdjustLevel("Health", 1, pending); } catch (IOException) { failed = true; }
        Check(failed && BaseUpgradeManualStore.Get(health) == 0, "First post-reset disk failure rolls back the manual amount");
        stats.FailSave = false;
        Check(service.TryAdjustLevel("Health", 1, BaseUpgradeManualStore.SaveIdentity), "Post-reset save failure remains retryable");
        SemiFunc.Multiplayer = false;
        return checks;
    }
}
