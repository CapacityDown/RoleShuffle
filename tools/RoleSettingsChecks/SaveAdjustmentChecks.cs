using BepInEx.Configuration;
using Photon.Pun;
using REPOJP.StageRoles;

internal static class SaveAdjustmentChecks
{
    internal static int Run(StageRolesConfig config, ConfigFile file, BaseUpgradeSelectionSettings service, string directory)
    {
        int checks = 0;
        void Check(bool value, string reason) { if (!value) throw new Exception(reason); checks++; }
        StatsManager.DirectoryPath = directory;
        SemiFunc.Multiplayer = false;
        GameManager.instance = new();
        RunManager.instance = new() { levelsCompleted = 2 };
        var manualSetting = config.BaseUpgradeManualAdjustmentEnabled;
        Check(!manualSetting.Value && Equals(manualSetting.DefaultValue, false), "Manual editing defaults to off");
        Check(manualSetting.Definition.Section == "Base Upgrades" && manualSetting.Definition.Key == "ManualAdjustmentEnabled",
            "Manual editing is a regular REPOConfig boolean setting");
        StatsManager.instance = new();
        StatsManager.instance.Load("manual-toggle.json");
        config.BaseHealthLevels.Value = "1:2";
        string toggleSave = BaseUpgradeManualStore.SaveIdentity;
        int blockedSaves = StatsManager.instance.Saves;
        Check(!service.CanAdjustLevel("Health", 1, toggleSave) && !service.CanAdjustLevel("Health", -1, toggleSave) &&
            !service.TryAdjustLevel("Health", 1, toggleSave) && !service.TryAdjustLevel("Health", -1, toggleSave) &&
            StatsManager.instance.Saves == blockedSaves, "Default-off rejects both valid edits without saving");
        RoleMenu.ShowLevels(config);
        Check(RoleMenu.Entries.Where(e => e.Adjustment != null).All(e => !e.Adjustment!.ShowButtons && e.Adjustment.Increase == null && e.Adjustment.Decrease == null) &&
            RoleMenu.Entries.Any(e => e.Text == "Manual adjustment is OFF in MOD settings."), "Default-off buttons are hidden and explained");
        manualSetting.BoxedValue = true;
        RoleMenu.RefreshLevelsIfChanged();
        Check(RoleMenu.Entries.First(e => e.Adjustment != null).Adjustment is { ShowButtons: true, Increase: not null, Decrease: not null },
            "REPOConfig enable refreshes the open level page even when levels did not change");
        var enabledReload = new StageRolesConfig(new ConfigFile(file.ConfigFilePath, false) { SaveOnConfigSet = false });
        Check(enabledReload.BaseUpgradeManualAdjustmentEnabled.Value, "Enabled preference persists through real configuration reload");
        foreach (var definition in RoleUpgradeScaling.Definitions)
        {
            StatsManager.instance = new();
            StatsManager.instance.Load(definition.Name + ".json");
            string save = BaseUpgradeManualStore.SaveIdentity;
            var entry = config.BaseUpgradeLevelsEntry(definition.Name)!;
            entry.Value = definition.MaximumLevel == 1 ? "1:0,5:1" : "1:2,5:7";
            string expression = entry.Value;
            string before = File.ReadAllText(file.ConfigFilePath);
            var allSettings = file.ToDictionary(p => p.Key, p => p.Value.BoxedValue);
            Check(service.TryAdjustLevel(definition.Name, 1, save), "All 12 upgrades accept a saved increment");
            Check(BaseUpgradeManualStore.Get(definition.DictionaryName) == 1, "Increment stored separately");
            Check(entry.Value == expression && allSettings.All(p => Equals(file[p.Key].BoxedValue, p.Value)) &&
                File.ReadAllText(file.ConfigFilePath) == before, "No REPOConfig entry or file is rewritten");
            StatsManager.instance = new();
            StatsManager.instance.Load(save);
            Check(BaseUpgradeManualStore.Get(definition.DictionaryName) == 1, "Manual adjustment reloads after process state is discarded");
            manualSetting.Value = false;
            Check(RoleCatalog.BaseUpgrades(config).Single(g => g.CommandName == definition.Name).Level ==
                (definition.MaximumLevel == 1 ? 0 : 2), "Off excludes the saved positive amount for every upgrade");
            Check(BaseUpgradeSync.Read(config).Single(g => g.Name == definition.Name).ManualAdjustment == 0 &&
                BaseUpgradeManualStore.Get(definition.DictionaryName) == 1, "Off reports no applied manual value without deleting the saved amount");
            manualSetting.Value = true;
            RunManager.instance.levelsCompleted = 8;
            Check(RoleCatalog.BaseUpgrades(config).Single(g => g.CommandName == definition.Name).Level ==
                (definition.MaximumLevel == 1 ? 1 : 8), "Adjustment survives later configured thresholds");
            Check(BaseUpgradeManualStore.Get(definition.DictionaryName) == 1, "Clamping does not erase the saved adjustment");
            RunManager.instance.levelsCompleted = 2;
            Check(service.TryAdjustLevel(definition.Name, -1, save) && BaseUpgradeManualStore.Get(definition.DictionaryName) == 0,
                "Decrease reverses the manual increment without rewriting rules");
            entry.Value = "1:1";
            Check(service.TryAdjustLevel(definition.Name, -1, save), "Each upgrade accepts a negative manual amount");
            manualSetting.Value = false;
            Check(RoleCatalog.BaseUpgrades(config).Single(g => g.CommandName == definition.Name).Level == 1 &&
                BaseUpgradeManualStore.Get(definition.DictionaryName) == -1, "Off also excludes negative manual amounts without erasing them");
            manualSetting.Value = true;
            Check(RoleCatalog.BaseUpgrades(config).Single(g => g.CommandName == definition.Name).Level == 0,
                "Re-enabling restores negative saved amounts, including the Map cap");
        }

        StatsManager.instance = new();
        StatsManager.instance.Load("coexist.json");
        string identity = BaseUpgradeManualStore.SaveIdentity;
        const string health = "playerUpgradeHealth";
        int Total() => RoleCatalog.BaseUpgrades(config).Single(g => g.CommandName == "Health").Level;
        config.BaseHealthLevels.Value = "1:2,5:7";
        Check(service.TryAdjustLevel("Health", 1, identity) && Total() == 3, "Initial manual increase");
        Check(BaseUpgradeBonusStore.ApplyEffectiveDelta(health, Total(), 2, 2, 200, manualSetting.Value), "Truck result can follow manual adjustment");
        Check(Total() == 5 && BaseUpgradeManualStore.Get(health) == 1 && BaseUpgradeBonusStore.Get(health) == 2,
            "Truck result retains manual amount");
        Check(service.TryAdjustLevel("Health", 1, identity) && Total() == 6 && BaseUpgradeBonusStore.Get(health) == 2,
            "Manual result retains truck amount");
        Check(BaseUpgradeBonusStore.ApplyEffectiveDelta(health, Total(), 2, -1, 200, manualSetting.Value) && Total() == 5 &&
            BaseUpgradeManualStore.Get(health) == 2 && BaseUpgradeBonusStore.Get(health) == 1, "Negative truck result remains separate");
        RoleMenu.ShowLevels(config);
        Action staleEnabledClick = RoleMenu.Entries.First(e => e.Adjustment != null).Adjustment!.Increase!;
        manualSetting.BoxedValue = false;
        RoleMenu.RefreshLevelsIfChanged();
        Check(RoleMenu.Entries.Where(e => e.Adjustment != null).All(e => !e.Adjustment!.ShowButtons && e.Adjustment.Increase == null && e.Adjustment.Decrease == null),
            "REPOConfig disable hides every adjustment control");
        blockedSaves = StatsManager.instance.Saves;
        staleEnabledClick();
        Check(!service.TryAdjustLevel("Health", 1, identity) && !service.TryAdjustLevel("Health", -1, identity) &&
            StatsManager.instance.Saves == blockedSaves, "Disabling also blocks stale callbacks and direct edits");
        Check(Total() == 3 && BaseUpgradeManualStore.Get(health) == 2 && BaseUpgradeBonusStore.Get(health) == 1 &&
            config.BaseHealthLevels.Value == "1:2,5:7", "Off removes the applied manual level while retaining saved amounts, draw bonuses and rules");
        Check(RoleMenu.Entries.Any(e => e.Text == "Health" && e.Adjustment?.CurrentLevel == 3) &&
            RoleMenu.Entries.Any(e => e.Text == "Config 2  Draw +1"), "Off breakdown shows configured and truck values only");
        var disabledReload = new StageRolesConfig(new ConfigFile(file.ConfigFilePath, false) { SaveOnConfigSet = false });
        Check(!disabledReload.BaseUpgradeManualAdjustmentEnabled.Value, "Disabled preference persists through real configuration reload");
        StatsManager.instance.SaveFileSave();
        StatsManager.instance = new();
        StatsManager.instance.Load(identity);
        Check(Total() == 3 && BaseUpgradeManualStore.Get(health) == 2 && BaseUpgradeBonusStore.Get(health) == 1, "Off survives reload and retains the inactive manual amount");
        Check(BaseUpgradeBonusStore.ApplyEffectiveDelta(health, Total(), 2, 1, 200, manualSetting.Value) && Total() == 4 &&
            BaseUpgradeManualStore.Get(health) == 2 && BaseUpgradeBonusStore.Get(health) == 2, "Off truck increment does not compensate for the inactive manual amount");
        Check(BaseUpgradeBonusStore.ApplyEffectiveDelta(health, Total(), 2, -1, 200, manualSetting.Value) && Total() == 3 &&
            BaseUpgradeBonusStore.Get(health) == 1, "Off truck decrement changes only the truck bonus");
        manualSetting.BoxedValue = true;
        Check(Total() == 5 && service.CanAdjustLevel("Health", 1, identity) && service.CanAdjustLevel("Health", -1, identity),
            "Re-enabling restores the saved amount without double counting off-time draws");
        RunManager.instance.levelsCompleted = 5;
        Check(Total() == 10, "Later threshold adds both saved amounts");
        StatsManager.instance.Load("other-save.json");
        Check(Total() == 7 && BaseUpgradeManualStore.Get(health) == 0, "Another save starts without either bonus");
        Check(!service.TryAdjustLevel("Health", 1, identity), "Stale save callback cannot edit a different save");
        StatsManager.instance.Load(identity);
        Check(Total() == 10, "Original save retains its own values");
        StatsManager.instance.runStats.Clear();
        Check(Total() == 7, "Native runStats reset clears per-save adjustments");

        foreach (string name in new[] { "Unknown", "AllUpgrades", "" })
            Check(!service.TryAdjustLevel(name, 1, identity), "Unknown targets refused");
        foreach (int delta in new[] { 0, 2, -2, int.MaxValue, int.MinValue })
            Check(!service.TryAdjustLevel("Health", delta, identity), "Only single steps accepted");
        config.BaseHealthLevels.Value = "1:200";
        Check(!service.TryAdjustLevel("Health", 1, identity) && service.TryAdjustLevel("Health", -1, identity) && Total() == 199,
            "Upper limit with decrease enabled");
        config.BaseHealthLevels.Value = "";
        Check(!service.TryAdjustLevel("Health", -1, identity) && service.TryAdjustLevel("Health", 1, identity) && Total() == 1,
            "Lower limit and clipped total move by one");
        config.BaseMapPlayerCountLevels.Value = "1:1";
        Check(!service.TryAdjustLevel("MapPlayerCount", 1, identity), "Map retains its one-level cap");
        config.BaseHealthLevels.Value = "1:2,broken,5:7";
        int saves = StatsManager.instance.Saves;
        Check(!service.TryAdjustLevel("Health", 1, identity) && StatsManager.instance.Saves == saves, "Malformed rules are preserved without saving");
        config.BaseHealthLevels.Value = "1:2;3:4;3:5;9:8";
        RunManager.instance.levelsCompleted = 2;
        Check(service.TryAdjustLevel("Health", 1, identity) && Total() == 7 && config.BaseHealthLevels.Value == "1:2;3:4;3:5;9:8",
            "Duplicate and semicolon rules stay byte-for-byte unchanged");
        file.SaveOnConfigSet = false;
        Check(service.TryAdjustLevel("Health", 1, identity) && !file.SaveOnConfigSet, "Disabled config autosave preserved");
        file.SaveOnConfigSet = true;
        int old = BaseUpgradeManualStore.Get(health);
        StatsManager.instance.FailSave = true;
        bool failed = false;
        try { service.TryAdjustLevel("Health", 1, identity); } catch (IOException) { failed = true; }
        Check(failed && BaseUpgradeManualStore.Get(health) == old && file.SaveOnConfigSet, "Failed native save rolls back the previous adjustment");
        StatsManager.instance.runStats.Clear();
        try { service.TryAdjustLevel("Health", 1, identity); } catch (IOException) { }
        Check(StatsManager.instance.runStats.Count == 0, "Failed first adjustment restores absence of key");
        StatsManager.instance.FailSave = false;

        config.BaseHealthLevels.Value = "";
        RoleMenu.ShowLevels(config);
        var controls = RoleMenu.Entries.Where(e => e.Adjustment != null).ToArray();
        Check(controls.Length == 12 && controls[0].Text == "Health" && controls[0].Adjustment!.CurrentLevel == 0,
            "Each upgrade heading includes the displayed total and its controls");
        Check(controls[0].Adjustment!.Decrease == null && controls[0].Adjustment!.Increase != null, "Lower-bound appearance follows effective total");
        controls[0].Adjustment!.Increase!();
        Check(config.BaseHealthLevels.Value == "" && Total() == 1 &&
            RoleMenu.Entries.Any(e => e.Text == "Health" && e.Adjustment?.CurrentLevel == 1) &&
            RoleMenu.Entries.Any(e => e.Text == "Config 0  Manual +1  Draw 0"), "Callback persists and displays separate configured, manual and truck values");
        Action oldSaveClick = RoleMenu.Entries.First(e => e.Adjustment != null).Adjustment!.Increase!;
        StatsManager.instance.Load("ui-other.json");
        oldSaveClick();
        Check(BaseUpgradeManualStore.Get(health) == 0, "Rendered callback cannot leak adjustment across saves");
        config.BaseHealthLevels.BoxedValue = "1:200";
        RoleMenu.RefreshLevelsIfChanged();
        var healthControls = RoleMenu.Entries.First(e => e.Adjustment != null).Adjustment!;
        Check(healthControls.Increase == null && healthControls.Decrease != null, "REPOConfig updates refresh effective bounds");
        Action hostClick = healthControls.Decrease!;
        Check(service.TryAdjustLevel("Health", -1, BaseUpgradeManualStore.SaveIdentity), "Prepare nonzero manual host data");
        SemiFunc.Multiplayer = true;
        PhotonNetwork.IsMasterClient = true;
        PhotonNetwork.CurrentRoom = new();
        manualSetting.Value = false;
        BaseUpgradeSync.Publish(config);
        string offLegacy = (string)PhotonNetwork.CurrentRoom.CustomProperties["RS.BaseUpgrades"];
        string offDetail = (string)PhotonNetwork.CurrentRoom.CustomProperties["RS.BaseUpgrades.Detail"];
        string offState = (string)PhotonNetwork.CurrentRoom.CustomProperties[BaseUpgradeSync.ManualStatePropertyKey];
        manualSetting.Value = true;
        BaseUpgradeSync.Publish(config);
        string legacy = (string)PhotonNetwork.CurrentRoom.CustomProperties["RS.BaseUpgrades"];
        string detail = (string)PhotonNetwork.CurrentRoom.CustomProperties["RS.BaseUpgrades.Detail"];
        Check(legacy.Split(';').All(e => e.Split(',').Length == 4), "Old clients retain four-field wire compatibility");
        PhotonNetwork.IsMasterClient = false;
        saves = StatsManager.instance.Saves;
        hostClick();
        Check(StatsManager.instance.Saves == saves && !service.TryAdjustLevel("Health", -1, BaseUpgradeManualStore.SaveIdentity), "Host authority rechecked at invocation");
        config.BaseHealthLevels.Value = "1:3";
        StatsManager.instance.runStats.Clear();
        manualSetting.Value = false;
        RoleMenu.ShowLevels(config);
        Check(RoleMenu.Entries.Any(e => e.Text == "Health" && e.Adjustment?.CurrentLevel == 199) &&
            RoleMenu.Entries.Any(e => e.Text == "Config 200  Manual -1  Draw 0"), "Guest sees host breakdown instead of local data");
        Check(RoleMenu.Entries.Where(e => e.Adjustment != null).All(e => e.Adjustment!.ShowButtons && e.Adjustment.Increase == null && e.Adjustment.Decrease == null),
            "Host-on guest controls remain read-only even when the guest local setting is off");
        PhotonNetwork.CurrentRoom.CustomProperties["RS.BaseUpgrades"] = offLegacy;
        PhotonNetwork.CurrentRoom.CustomProperties["RS.BaseUpgrades.Detail"] = offDetail;
        PhotonNetwork.CurrentRoom.CustomProperties[BaseUpgradeSync.ManualStatePropertyKey] = offState;
        manualSetting.Value = true;
        RoleMenu.RefreshLevelsIfChanged();
        Check(RoleMenu.Entries.Where(e => e.Adjustment != null).All(e => !e.Adjustment!.ShowButtons) &&
            RoleMenu.Entries.Any(e => e.Text == "Health" && e.Adjustment?.CurrentLevel == 200) &&
            RoleMenu.Entries.Any(e => e.Text == "Config 200  Draw 0") &&
            BaseUpgradeSync.Read(config)[0].ManualAdjustment == 0, "Host-off hides guest buttons and syncs totals excluding manual amounts despite guest local on");
        foreach (object invalid in new object[] { "2|1", "1|x", "1|1|extra", 1 })
        {
            PhotonNetwork.CurrentRoom.CustomProperties[BaseUpgradeSync.ManualStatePropertyKey] = invalid;
            Check(!BaseUpgradeSync.ManualAdjustmentEnabled(config), "Malformed or previous-host button state is ignored");
        }
        PhotonNetwork.CurrentRoom.CustomProperties["RS.BaseUpgrades"] = legacy;
        PhotonNetwork.CurrentRoom.CustomProperties["RS.BaseUpgrades.Detail"] = detail;
        PhotonNetwork.CurrentRoom.CustomProperties.Remove(BaseUpgradeSync.ManualStatePropertyKey);
        RoleMenu.RefreshLevelsIfChanged();
        Check(RoleMenu.Entries.Where(e => e.Adjustment != null).All(e => e.Adjustment!.ShowButtons && e.Adjustment.Increase == null),
            "Missing extension preserves older hosts' read-only button display");
        PhotonNetwork.CurrentRoom.CustomProperties.Remove("RS.BaseUpgrades.Detail");
        Check(BaseUpgradeSync.Read(config)[0].ManualAdjustment == 0 && BaseUpgradeSync.Read(config)[0].CurrentLevel == 199, "Older hosts remain readable");
        foreach (string invalid in new[] { "broken", "2|" + detail.Split('|')[1], detail.Replace("Health,199,", "Health,198,"), detail.Replace(",0,-1;", ",0,invalid;") })
        {
            PhotonNetwork.CurrentRoom.CustomProperties["RS.BaseUpgrades.Detail"] = invalid;
            Check(BaseUpgradeSync.Read(config)[0].ManualAdjustment == 0, "Invalid, stale, or previous-host detail ignored");
        }
        PhotonNetwork.CurrentRoom.CustomProperties.Remove("RS.BaseUpgrades");
        RoleMenu.ShowLevels(config);
        Check(RoleMenu.Entries.All(e => e.Adjustment == null), "No host payload does not fabricate values");

        PhotonNetwork.IsMasterClient = true;
        BaseUpgradeSync.Publish(config);
        string unchangedLevels = BaseUpgradeSync.LocalSignature(config);
        string beforeToggle = BaseUpgradeSync.LocalPublishSignature(config);
        manualSetting.Value = false;
        Check(unchangedLevels == BaseUpgradeSync.LocalSignature(config) && beforeToggle != BaseUpgradeSync.LocalPublishSignature(config),
            "An on/off change is published even when all saved manual amounts are zero");
        BaseUpgradeSync.Publish(config);
        PhotonNetwork.IsMasterClient = false;
        RoleMenu.ShowLevels(config);
        Check(RoleMenu.Entries.Where(e => e.Adjustment != null).All(e => !e.Adjustment!.ShowButtons),
            "Zero-manual host toggle still updates guest button visibility");
        manualSetting.Value = true;

        SemiFunc.Multiplayer = false;
        StatsManager.instance.Load("readiness.json");
        identity = BaseUpgradeManualStore.SaveIdentity;
        StatsManager.instance.ReadyForTests = false;
        Check(!service.TryAdjustLevel("Health", 1, identity), "Loading/reset phase cannot save adjustments");
        StatsManager.instance.ReadyForTests = true;
        GameManager.instance.LobbyForTests = GameManager.LobbyTypes.Public;
        Check(!service.TryAdjustLevel("Health", 1, identity), "Native unsaved lobby cannot promise persistent edits");
        RoleMenu.ShowLevels(config);
        Check(RoleMenu.Entries.Any(e => e.Text == "Load a saved game to use +/- adjustments."), "Explains unavailable save controls");
        GameManager.instance.LobbyForTests = GameManager.LobbyTypes.Private;
        GameDirector.instance = null;
        Check(!service.TryAdjustLevel("Health", 1, identity), "No game director cannot call native save");
        GameDirector.instance = new();
        GameManager.instance = null;
        Check(!service.TryAdjustLevel("Health", 1, identity), "No game cannot edit");
        GameManager.instance = new();
        StatsManager.instance = null;
        Check(BaseUpgradeManualStore.Get(health) == 0 && !service.TryAdjustLevel("Health", 1, identity), "Missing StatsManager cannot edit");
        RunManager.instance = new() { levelsCompleted = int.MaxValue };
        Check(BaseUpgradeSelectionSettings.CurrentRunLevel == 999999, "Run level arithmetic does not overflow");
        return checks + ManualAdjustmentLifecycleChecks.Run(config, service);
    }
}
