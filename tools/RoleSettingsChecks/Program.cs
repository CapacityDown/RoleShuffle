using BepInEx.Configuration;
using Photon.Pun;
using REPOJP.StageRoles;

int checks = 0;
void Check(bool condition, string label) { if (!condition) throw new Exception(label); checks++; }
string directory = Path.Combine(AppContext.BaseDirectory, "test-data", Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(directory);
string path = Path.Combine(directory, "RoleShuffle.cfg");
var file = new ConfigFile(path, false) { SaveOnConfigSet = false };
var config = new StageRolesConfig(file);
Console.WriteLine($"Ability configuration checks passed: {AbilityConfigChecks.Run(directory)}");
Check(!config.KeepUpgradeItems.Value && Equals(config.KeepUpgradeItems.DefaultValue, false), "Consumed upgrade retention defaults off");
Check(config.KeepUpgradeItems.Definition.Section == "Base Upgrades" && config.KeepUpgradeItems.Definition.Key == "KeepUpgradeItems", "Retention is bound in REPOConfig");
Check(config.UpgradeItemScope.Value == UpgradeItemScope.Player && Enum.GetValues<UpgradeItemScope>().Length == 2, "Personal and shared scopes available; personal is default");
config.KeepUpgradeItems.Value = true;
config.UpgradeItemScope.Value = UpgradeItemScope.AllPlayers;
file.Save();
var retentionReload = new StageRolesConfig(new ConfigFile(path, false) { SaveOnConfigSet = false });
Check(retentionReload.KeepUpgradeItems.Value && retentionReload.UpgradeItemScope.Value == UpgradeItemScope.AllPlayers, "Retention and scope persist in real BepInEx configuration");
config.KeepUpgradeItems.Value = false;
config.UpgradeItemScope.Value = UpgradeItemScope.Player;

Check(config.TankHealthMultiplier.Value == 1.5f && config.RunnerSpeedMultiplier.Value == 1.5f &&
    config.RunnerStaminaMultiplier.Value == 1.5f,
    "Effective-value multiplier defaults");
Check(config.TankMaximumHealth.Value == 4100 && config.RunnerMaximumSpeed.Value == 205 &&
    config.RunnerMaximumStamina.Value == 2040,
    "Caps are ceilings of vanilla maxima across levels 0-200");
string oldConfigPath = Path.Combine(directory, "before-multipliers.cfg");
File.WriteAllText(oldConfigPath, "[Migration]\nConfigVersion = 32\n[Tank]\nBaseHealthBonus = 17\nHealthUpgradeLevels = 30\nHealthMultiplier = 1.8\n[Runner]\nBaseSpeedBonus = 2\nBaseStaminaBonus = 10\n[Lifter]\nBaseStrengthBonus = 5\nStrengthUpgradeLevels = 40\n");
var migratedFile = new ConfigFile(oldConfigPath, false) { SaveOnConfigSet = false };
var migrated = new StageRolesConfig(migratedFile);
migratedFile.Save();
string migratedText = File.ReadAllText(oldConfigPath);
Check(migrated.TankHealthLevels.Value == 30 && migrated.TankHealthMultiplier.Value == 1.8f,
    "Migration preserves configured minimums and existing multiplier choice");
Check(new[] { "BaseHealthBonus", "BaseSpeedBonus", "BaseStaminaBonus", "BaseStrengthBonus" }.All(key => !migratedText.Contains(key)),
    "Migration removes obsolete additive settings");
Check(File.ReadAllText(oldConfigPath + ".pre-v4.5.0-multipliers.bak").Contains("BaseHealthBonus = 17"),
    "Migration backs up the previous configuration");
Check(config.KingSpeedBonus.Value == 2 && config.KingRangeBonus.Value == 2 && config.KingStrengthBonus.Value == 5 && config.KingUpgradeRadius.Value == 12, "Stronger King support defaults");
foreach (string variant in new[] { "defaults", "decimal-radius", "custom", "mixed", "disabled-bonuses", "current-schema" })
{
    string supportPath = Path.Combine(directory, "king-support-" + variant + ".cfg");
    bool custom = variant == "custom", mixed = variant == "mixed", zero = variant == "disabled-bonuses", current = variant == "current-schema";
    int speed = custom ? 4 : zero ? 0 : 1, range = custom || mixed ? 3 : zero ? 0 : 1, strength = custom ? 9 : zero ? 0 : 1;
    string radius = custom ? "20" : variant == "decimal-radius" ? "8.00" : "8";
    string before = $"[Migration]\nConfigVersion = {(current ? 40 : 39)}\n[King]\nSpeedBonusLevels = {speed}\nRangeBonusLevels = {range}\nStrengthBonusLevels = {strength}\nUpgradeRadius = {radius}\nEnabled = false\nWeight = 75\n";
    File.WriteAllText(supportPath, before);
    var supportFile = new ConfigFile(supportPath, false) { SaveOnConfigSet = false };
    var support = new StageRolesConfig(supportFile); supportFile.Save();
    Check(support.KingSpeedBonus.Value == (current || custom || zero ? speed : 2) &&
        support.KingRangeBonus.Value == (current || custom || mixed || zero ? range : 2) &&
        support.KingStrengthBonus.Value == (current || custom || zero ? strength : 5) &&
        support.KingUpgradeRadius.Value == (custom ? 20 : current ? 8 : 12),
        "King defaults migrate per key while custom/current-schema values survive: " + variant);
    Check(!support.KingEnabled.Value && support.KingWeight.Value == 75, "King selection preferences survive: " + variant);
    string supportBackup = supportPath + ".pre-v4.5.1-king-support.bak";
    Check(current ? !File.Exists(supportBackup) : File.ReadAllText(supportBackup) == before, "Exact King rollback backup: " + variant);
    string after = File.ReadAllText(supportPath);
    Check(after.Contains("ConfigVersion = 40"), "King migration uses schema 40: " + variant);
    RoleConfigMigration.Apply(supportFile);
    Check(File.ReadAllText(supportPath) == after && (current || File.ReadAllText(supportBackup) == before),
        "King migration is repeatable without replacing the backup: " + variant);
}
string oldKingPath = Path.Combine(directory, "before-king.cfg");
File.WriteAllText(oldKingPath, "[Migration]\nConfigVersion = 33\n[King]\nHealRadius = 12\nHealAmount = 9\nHealIntervalSeconds = 2\nTotalHealingLimit = 90\nStrengthBonusLevels = 3\n");
var kingFile = new ConfigFile(oldKingPath, false) { SaveOnConfigSet = false };
var kingConfig = new StageRolesConfig(kingFile); kingFile.Save();
Check(kingConfig.KingUpgradeRadius.Value == 12 && kingConfig.KingStrengthBonus.Value == 3, "King migration preserves radius and explicit upgrade configuration");
string kingText = File.ReadAllText(oldKingPath).Split("[King]")[1].Split("\n[")[0];
Check(!kingText.Contains("HealAmount") && !kingText.Contains("HealRadius") && !kingText.Contains("TotalHealingLimit") && !kingText.Contains("HealIntervalSeconds"), "King migration removes healing settings");
Check(File.ReadAllText(oldKingPath + ".pre-v4.5.0-king-upgrades.bak").Contains("HealAmount = 9"), "King migration retains exact old config backup");
string oldLifterPath = Path.Combine(directory, "before-lifter-200.cfg");
const string oldLifterText = "[Migration]\nConfigVersion = 34\n[Lifter]\nStrengthMultiplier = 2.5\nMaximumEffectiveStrength = 4\nStrengthUpgradeLevels = 40\nEnabled = false\nWeight = 75\n";
File.WriteAllText(oldLifterPath, oldLifterText);
var lifterFile = new ConfigFile(oldLifterPath, false) { SaveOnConfigSet = false };
var lifterConfig = new StageRolesConfig(lifterFile); lifterFile.Save();
string lifterText = File.ReadAllText(oldLifterPath);
Check(!lifterText.Contains("StrengthMultiplier") && !lifterText.Contains("MaximumEffectiveStrength"), "Remove unused Lifter growth settings");
Check(!lifterText.Split("[Lifter]")[1].Split("\n[")[0].Contains("StrengthUpgradeLevels") && !lifterConfig.LifterEnabled.Value && lifterConfig.LifterWeight.Value == 75,
    "Fixed Lifter migration removes the unused level setting and preserves selection preferences");
Check(File.ReadAllText(oldLifterPath + ".pre-v4.5.0-lifter-200.bak") == oldLifterText, "Back up exact schema 34 config");
Check(lifterText.Contains("ConfigVersion = 40"), "Config uses the current v4.5 role schema");
RoleConfigMigration.Apply(lifterFile);
Check(File.ReadAllText(oldLifterPath) == lifterText && File.ReadAllText(oldLifterPath + ".pre-v4.5.0-lifter-200.bak") == oldLifterText,
    "Repeated migration preserves settings and rollback backup");
Check(config.HudResourceOffsetX.Value == 0 && config.HudResourceOffsetY.Value == 0 && config.HudResourceScale.Value == 100,
    "New resource HUD uses native alignment and scale");
foreach (bool custom in new[] { false, true })
{
    string hudPath = Path.Combine(directory, custom ? "custom-hud.cfg" : "default-hud.cfg");
    string before = "[Migration]\nConfigVersion = 35\n[HUD]\nResourceHudOffsetX = " + (custom ? "40" : "16") +
        "\nResourceHudOffsetY = " + (custom ? "60" : "24") + "\nResourceHudScalePercent = 125\nResourceHudEnabled = false\n";
    File.WriteAllText(hudPath, before);
    var hudFile = new ConfigFile(hudPath, false) { SaveOnConfigSet = false };
    var hudConfig = new StageRolesConfig(hudFile); hudFile.Save();
    Check(hudConfig.HudResourceOffsetX.Value == (custom ? 40 : 0) && hudConfig.HudResourceOffsetY.Value == (custom ? 60 : 0),
        "Native HUD migration replaces old defaults and preserves custom offsets");
    Check(hudConfig.HudResourceScale.Value == 125 && !hudConfig.HudResourcesEnabled.Value, "Preserve HUD scale and visibility preferences");
    Check(File.ReadAllText(hudPath + ".pre-v4.5.0-native-hud.bak") == before, "Exact pre-migration HUD backup");
    string after = File.ReadAllText(hudPath); RoleConfigMigration.Apply(hudFile);
    Check(File.ReadAllText(hudPath) == after, "Native HUD migration is idempotent");
}
foreach (bool wasEnabled in new[] { false, true })
{
    string standardPath = Path.Combine(directory, $"standard-roles-{wasEnabled}.cfg");
    string before = "[Migration]\nConfigVersion = 36\n[General]\nEnabled = false\nOverhaulEnabled = " + wasEnabled +
        "\n[Tank]\nHealthUpgradeLevels = 30\nHealthMultiplier = 2.3\nMaximumHealth = 2000\n" +
        "[Runner]\nSpeedMultiplier = 1.7\nMaximumSprintSpeed = 99\nStaminaMultiplier = 1.8\nMaximumStamina = 1200\n" +
        "[Lifter]\nStrengthUpgradeLevels = 17\nEnabled = false\nWeight = 75\n" +
        "[Jobless]\nContractDistance = 7\nContractGraceSeconds = 45\nContractsPerStage = 5\nContractHeal = 12\n" +
        "[King]\nUpgradeRadius = 12\nStrengthBonusLevels = 2\n" +
        "[HUD]\nAbilityStatusEnabled = true\nResourceHudEnabled = false\nResourceHudOffsetX = 40\nResourceHudScalePercent = 125\n";
    File.WriteAllText(standardPath, before);
    var standardFile = new ConfigFile(standardPath, false) { SaveOnConfigSet = false };
    var standard = new StageRolesConfig(standardFile); standardFile.Save();
    string after = File.ReadAllText(standardPath);
    Check(!after.Contains("OverhaulEnabled") && !after.Split("[Lifter]")[1].Split("\n[")[0].Contains("StrengthUpgradeLevels") && !after.Contains("AbilityStatusEnabled"),
        "Both saved toggle values migrate without obsolete mode, Lifter or status settings");
    Check(!standardFile.Any(entry => entry.Key.Key is "OverhaulEnabled" or "AbilityStatusEnabled" ||
        (entry.Key.Section == "Lifter" && entry.Key.Key == "StrengthUpgradeLevels")),
        "Obsolete entries are not exposed to the settings UI");
    Check(!standard.Enabled.Value && !standard.LifterEnabled.Value && standard.LifterWeight.Value == 75,
        "Standard rules preserve the mod and per-role selection switches");
    Check(standard.TankHealthLevels.Value == 30 && standard.TankHealthMultiplier.Value == 2.3f && standard.TankMaximumHealth.Value == 2000 &&
        standard.RunnerSpeedMultiplier.Value == 1.7f && standard.RunnerMaximumSpeed.Value == 99 &&
        standard.RunnerStaminaMultiplier.Value == 1.8f && standard.RunnerMaximumStamina.Value == 1200,
        "Standard rules preserve growth tuning");
    Check(standard.JoblessContractDistance.Value == 7 && standard.JoblessInitialGrace.Value == 45 &&
        !after.Contains("ContractsPerStage") && !after.Contains("ContractHeal") &&
        standard.KingUpgradeRadius.Value == 12 && standard.KingStrengthBonus.Value == 2,
        "Standard rules preserve contract and King tuning");
    Check(!standard.HudResourcesEnabled.Value && standard.HudResourceOffsetX.Value == 40 && standard.HudResourceScale.Value == 125,
        "Standard rules preserve resource HUD preferences");
    string backup = standardPath + ".pre-v4.5.0-standard-roles.bak";
    Check(File.ReadAllText(backup) == before && after.Contains("ConfigVersion = 40"), "Exact rollback backup and schema upgrade");
    RoleConfigMigration.Apply(standardFile);
    Check(File.ReadAllText(standardPath) == after && File.ReadAllText(backup) == before, "Standard migration is idempotent");
}
foreach (int oldHeal in new[] { 0, 10, 100 })
{
    string joblessPath = Path.Combine(directory, $"jobless-full-{oldHeal}.cfg");
    string before = $"[Migration]\nConfigVersion = 37\n[Jobless]\nContractHeal = {oldHeal}\nContractDistance = 7\nContractGraceSeconds = 45\nContractsPerStage = 5\nDamage = 2\nEnabled = false\n";
    File.WriteAllText(joblessPath, before);
    var joblessFile = new ConfigFile(joblessPath, false) { SaveOnConfigSet = false };
    var jobless = new StageRolesConfig(joblessFile); joblessFile.Save();
    string after = File.ReadAllText(joblessPath);
    Check(!after.Contains("ContractHeal") && !joblessFile.Any(e => e.Key.Section == "Jobless" && e.Key.Key == "ContractHeal"),
        "Full healing removes fixed amounts from disk and settings UI, including zero");
    Check(jobless.JoblessContractDistance.Value == 7 && jobless.JoblessInitialGrace.Value == 45 &&
        !after.Contains("ContractsPerStage") && jobless.JoblessDamage.Value == 2 && !jobless.JoblessEnabled.Value,
        "Full healing preserves other contract, damage and selection settings");
    string backup = joblessPath + ".pre-v4.5.0-jobless-full-heal.bak";
    Check(File.ReadAllText(backup) == before && after.Contains("ConfigVersion = 40"), "Exact pre-full-heal config backup");
    RoleConfigMigration.Apply(joblessFile);
    Check(File.ReadAllText(joblessPath) == after && File.ReadAllText(backup) == before, "Full-heal migration is idempotent");
}
var service = new RoleSelectionSettings(config, file);
Check(config.JoblessTinyGrace.Value == 30 && config.JoblessSmallGrace.Value == 30 && config.JoblessMediumGrace.Value == 60 &&
    config.JoblessBigGrace.Value == 90 && config.JoblessWideGrace.Value == 90 && config.JoblessTallGrace.Value == 90 && config.JoblessVeryTallGrace.Value == 120,
    "Courier category grace defaults use the longer requested durations");
Check(config.JoblessTinyHeal.Value == 10 && config.JoblessSmallHeal.Value == 25 && config.JoblessMediumHeal.Value == 50 &&
    config.JoblessBigHeal.Value == 100 && config.JoblessWideHeal.Value == 100 && config.JoblessTallHeal.Value == 100 && config.JoblessVeryTallHeal.Value == 100,
    "Courier healing uses requested fixed HP amounts");
string courierPath = Path.Combine(directory, "courier-rename.cfg");
string oldCourier = "[Migration]\nConfigVersion = 38\n[Jobless]\nEnabled = false\nWeight = 75\nContractDistance = 7\nContractGraceSeconds = 45\nContractsPerStage = 5\nTinyHealAmount = 19\nSmallGraceSeconds = 44\n[Courier]\nSmallGraceSeconds = 55\n";
File.WriteAllText(courierPath, oldCourier);
var courierFile = new ConfigFile(courierPath, false) { SaveOnConfigSet = false };
var courierConfig = new StageRolesConfig(courierFile); courierFile.Save();
string courierText = File.ReadAllText(courierPath);
Check(!courierText.Contains("[Jobless]") && !courierText.Contains("ContractsPerStage") && !courierText.Contains("ContractGraceSeconds"),
    "Legacy role section, count cap and shared duration are removed");
Check(!courierConfig.JoblessEnabled.Value && courierConfig.JoblessWeight.Value == 75 && courierConfig.JoblessContractDistance.Value == 7 &&
    courierConfig.JoblessInitialGrace.Value == 45 && courierConfig.JoblessTinyHeal.Value == 19 && courierConfig.JoblessSmallGrace.Value == 55,
    "Courier migration preserves legacy settings and gives explicit new-section values priority");
Check(File.ReadAllText(courierPath + ".pre-v4.5.0-delivery-grace.bak") == oldCourier, "Exact rollback backup before Courier migration");
RoleConfigMigration.Apply(courierFile);
Check(File.ReadAllText(courierPath) == courierText, "Courier migration is idempotent");
StageRolesPlugin.Instance.RoleSettings = service;
var roles = Enum.GetValues<StageRole>();
Check(roles.Length == 43 && (int)StageRole.Superbot == 1001 && (int)StageRole.Disaster == 1002, "Existing role identifiers are retained");
foreach (StageRole role in roles)
{
    var entry = config.RoleEnabledEntry(role);
    Check(entry != null && entry.Definition.Section == RoleCatalog.DisplayName(role) && entry.Definition.Key == "Enabled", "Maps each role to the existing REPOConfig entry");
    Check(Equals(entry!.DefaultValue, RolePresets.Find(RolePreset.Standard)!.Includes(role)), "Standard matches the real config default");
}
Check(config.RoleEnabledEntry((StageRole)999) == null && !service.TrySetEnabled((StageRole)999, true), "Reject unknown role");
Check(!service.TryApplyPreset((RolePreset)999), "Reject unknown preset");

config.MageWeight.Value = 0;
config.MageStarHealthCost.Value = 27;
config.UniqueRoles.Value = false;
config.RiskCombinationLimitsEnabled.Value = false;
var preserved = file.Where(p => p.Key.Key != "Enabled" || !roles.Any(r => ReferenceEquals(config.RoleEnabledEntry(r), p.Value)))
    .ToDictionary(p => p.Key, p => p.Value.BoxedValue);
foreach (var preset in RolePresets.All)
{
    Check(service.TryApplyPreset(preset.Id), "Host/solo applies preset");
    Check(roles.All(r => config.RoleIsEnabled(r) == preset.Includes(r)), "Preset replaces every role switch");
    Check(roles.Count(config.RoleIsEnabled) == preset.Count, "Displayed preset count matches actual settings");
    Check(preserved.All(p => Equals(file[p.Key].BoxedValue, p.Value)), "Presets preserve weights, ability values and all unrelated config");
    Check(RolePresets.MatchingName(config.RoleIsEnabled) == preset.Name, "Current preset is derived from stored toggles");
    var reloaded = new StageRolesConfig(new ConfigFile(path, false) { SaveOnConfigSet = false });
    Check(roles.All(r => reloaded.RoleIsEnabled(r) == preset.Includes(r)), "Preset survives configuration reload");
    Check(!file.SaveOnConfigSet, "Preserves disabled autosave flag");
}
foreach (RolePreset id in new[] { RolePreset.Beginner, RolePreset.Cooperative })
foreach (StageRole danger in new[] { StageRole.Bomber, StageRole.Stinker, StageRole.Werewolf, StageRole.Jobless, StageRole.Tuna, StageRole.Disaster })
    Check(!RolePresets.Find(id)!.Includes(danger), "Beginner/cooperative exclude harmful passive roles");
Check(RolePresets.Find(RolePreset.Standard)!.Count == roles.Length, "Standard restores every default role switch");
Check(RolePresets.Find(RolePreset.Challenge)!.Includes(StageRole.Jobless) && !RolePresets.Find(RolePreset.Challenge)!.Includes(StageRole.Phoenix), "Challenge contains hardship without revival");

service.TryApplyPreset(RolePreset.Standard);
file.SaveOnConfigSet = true;
RoleMenu.Show(config);
Check(RoleMenu.Entries.Count(e => e.Role.HasValue) == roles.Length, "Settings lists all roles");
var mage = RoleMenu.Entries.Single(e => e.Role == StageRole.Mage);
Check(mage.Text.Contains("Weight 0"), "Zero weight is distinguished from the ON switch");
mage.OnClick!();
Check(!config.MageEnabled.Value && RoleMenu.Entries.Single(e => e.Role == StageRole.Mage).Text.StartsWith("[OFF]"), "UI click changes the existing config and refreshes status");
Check(file.SaveOnConfigSet, "Preserves enabled autosave flag");
Check(RoleMenu.Entries.Count(e => e.Role.HasValue) == roles.Length, "Disabled roles remain available to re-enable");
Check(RoleMenu.Entries.Any(e => e.Text == "Selection: Custom"), "Manual edits show Custom");
RoleMenu.Entries.Single(e => e.Role == StageRole.Mage).OnClick!();
Check(config.MageEnabled.Value, "Disabled role can be re-enabled through UI");
RoleMenu.Entries.Single(e => e.Text == "PRESETS").OnClick!();
Check(RoleMenu.Entries.Count(e => e.Text.StartsWith("Apply: ") && e.OnClick != null) == 5, "All five presets have apply controls");
RoleMenu.Entries.Single(e => e.Text == "Apply: Beginner").OnClick!();
Check(RoleMenu.Entries.Any(e => e.Text == "Selection: Beginner"), "Applying preset returns to current role settings");
Check(!config.BomberEnabled.Value && config.MedicEnabled.Value, "Preset UI applies intended role selection");
var beforeFailedSave = roles.ToDictionary(r => r, config.RoleIsEnabled);
bool failedSave = false;
using (var locked = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None))
{
    try { service.TryApplyPreset(RolePreset.Chaos); }
    catch (IOException) { failedSave = true; }
}
Check(failedSave && beforeFailedSave.All(p => config.RoleIsEnabled(p.Key) == p.Value), "Save failure restores the original role selection");
Check(file.SaveOnConfigSet, "Save failure restores autosave behavior");

SemiFunc.Multiplayer = true;
PhotonNetwork.IsMasterClient = true;
RoleMenu.Show(config);
Action staleHostClick = RoleMenu.Entries.Single(e => e.Role == StageRole.Tank).OnClick!;
string saved = File.ReadAllText(path);
PhotonNetwork.IsMasterClient = false;
Check(!service.TrySetEnabled(StageRole.Tank, false) && !service.TryApplyPreset(RolePreset.Chaos), "Guests cannot change config through service");
staleHostClick();
Check(File.ReadAllText(path) == saved && config.TankEnabled.Value, "Losing host authority blocks already rendered controls");
RoleGuideSync.Remote = new() { StageRole.Bomber };
RoleMenu.Show(config);
Check(RoleMenu.Entries.Where(e => e.Role.HasValue).All(e => e.OnClick == null), "Guest role switches are read-only");
Check(RoleMenu.Entries.Single(e => e.Role == StageRole.Bomber).Text.StartsWith("[ON]") && RoleMenu.Entries.Single(e => e.Role == StageRole.Tank).Text.StartsWith("[OFF]"), "Guests display host state instead of local defaults");
RoleMenu.Show(config, presets: true);
Check(RoleMenu.Entries.Where(e => e.Text.StartsWith("Apply: ")).All(e => e.OnClick == null), "Guests cannot apply preset from UI");
RoleGuideSync.Available = false;
RoleMenu.Show(config);
Check(RoleMenu.Entries.All(e => !e.Role.HasValue), "Older hosts do not display fabricated toggle state");
PhotonNetwork.IsMasterClient = true;
GameManager.instance = null;
Check(!service.TryApplyPreset(RolePreset.Standard), "No active game authority cannot change settings");
GameManager.instance = new();
service.TryApplyPreset(RolePreset.Standard);
foreach (StageRole role in roles) service.TrySetEnabled(role, false);
RoleMenu.Show(config);
Check(RoleMenu.Entries.Any(e => e.Text.StartsWith("No regular roles")), "Empty candidate pool is visible");
config.Enabled.Value = false;
RoleMenu.Show(config);
Check(RoleMenu.Entries.Any(e => e.Text == "Role assignment is disabled in MOD settings."), "Global disabled setting is visible");
foreach (RoleGuideLanguage language in Enum.GetValues<RoleGuideLanguage>())
{
    var catalog = RoleText.Catalog(language);
    foreach (RolePresetDefinition preset in RolePresets.All)
    {
        Check(catalog.ContainsKey(preset.Name) && catalog.ContainsKey(preset.Description), "Preset labels and descriptions exist in every language");
    }
    RoleMenu.Show(config, presets: true, language);
    Check(RoleMenu.Entries.Count(e => e.OnClick != null) == 6, "Localized preset page keeps navigation and all apply actions");
}
Console.WriteLine($"Role settings checks passed: {checks}");
int baseStart = checks;
SemiFunc.Multiplayer = false;
config.Enabled.Value = true;
var baseService = new BaseUpgradeSelectionSettings(config, file);
StageRolesPlugin.Instance.BaseUpgradeSettings = baseService;
var names = BaseUpgradeDrawSelection.UpgradeNames;
Check(names.SequenceEqual(new[] { "Health", "Stamina", "ExtraJump", "Speed", "Strength", "Range", "Launch",
    "TumbleClimb", "TumbleWings", "CrouchRest", "MapPlayerCount", "DeathHeadBattery", "AllUpgrades" }),
    "Twelve upgrade types plus All Upgrades retain the existing network flag order");
foreach (string name in names)
{
    var entry = config.BaseUpgradeDrawEnabledEntry(name)!;
    Check(entry.Definition.Section == "Base Upgrade Draw Selection" && entry.Definition.Key == name && Equals(entry.DefaultValue, true), "REPOConfig-compatible bool entries preserve defaults");
}
Check(!baseService.TrySetEnabled("Unknown", true), "Unknown upgrade rejected");
config.BaseHealthLevels.Value = "1:3,5:7";
config.TruckUpgradeDrawDeltaWeights.Value = "-2:4,3:8";
config.TruckUpgradeDrawMaximumLevel.Value = 37;
config.TruckUpgradeDrawEnabled.Value = false;
var untouched = file.Where(p => p.Key.Section != "Base Upgrade Draw Selection").ToDictionary(p => p.Key, p => p.Value.BoxedValue);
var previousBaseLevels = BaseUpgradeSync.LocalSignature(config);
foreach (string name in names)
{
    Check(baseService.TrySetEnabled(name, false), "Host can disable each draw selection independently");
    Check(names.All(n => config.BaseUpgradeDrawIsEnabled(n) == (n != name)), "Only the selected upgrade switch changes");
    Check(untouched.All(p => Equals(file[p.Key].BoxedValue, p.Value)), "Preserve existing REPOConfig settings, roles and draw activation");
    var reload = new StageRolesConfig(new ConfigFile(path, false) { SaveOnConfigSet = false });
    Check(names.All(n => reload.BaseUpgradeDrawIsEnabled(n) == (n != name)), "Individual selection persisted in REPOConfig file");
    Check(BaseUpgradeSync.LocalSignature(config) == previousBaseLevels, "Selections do not alter level snapshot");
    Check(baseService.TrySetEnabled(name, true), "Host can restore each selection independently");
}
RoleMenu.ShowBase(config);
RoleMenu.Entries.Single(e => e.Text == "[ON] Health").OnClick!();
Check(!config.BaseUpgradeDrawEnabledEntry("Health")!.Value, "UI updates same REPOConfig ConfigEntry");
Check(RoleMenu.Entries.Any(e => e.Text == "[OFF] Health" && e.OnClick != null), "OFF upgrade remains available");
Check(RoleMenu.Entries.All(e => e.Text != "PRESETS" && !e.Text.StartsWith("Selection: ")), "Base Upgrade settings have no preset controls or matching label");
// REPOConfig applies its pending edits through ConfigEntryBase.BoxedValue.
config.BaseUpgradeDrawEnabledEntry("Health")!.BoxedValue = true;
config.TruckUpgradeDrawEnabled.BoxedValue = true;
RoleMenu.RefreshBaseIfChanged();
Check(RoleMenu.Entries.Any(e => e.Text == "[ON] Health") && RoleMenu.Entries.Any(e => e.Text == "Truck draw: ON"), "REPOConfig edits refresh open UI");
RoleMenu.Entries.Single(e => e.Text == "Truck draw: ON").OnClick!();
Check(!config.TruckUpgradeDrawEnabled.Value, "Draw activation shares existing REPOConfig entry");
var baseBeforeFailure = names.ToDictionary(n => n, config.BaseUpgradeDrawIsEnabled);
failedSave = false;
using (var locked = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None))
{ try { baseService.TrySetEnabled("Health", false); } catch (IOException) { failedSave = true; } }
Check(failedSave && baseBeforeFailure.All(p => config.BaseUpgradeDrawIsEnabled(p.Key) == p.Value) && file.SaveOnConfigSet, "Failed selection save rolls back the switch and autosave");
SemiFunc.Multiplayer = true;
PhotonNetwork.IsMasterClient = true;
PhotonNetwork.CurrentRoom = new();
BaseUpgradeSync.Publish(config);
string published = BaseUpgradeSettingsSync.CurrentSignature(config);
Check(PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("RS.BaseUpgrades"), "Legacy level payload remains published");
RoleMenu.ShowBase(config);
Action oldBaseClick = RoleMenu.Entries.Single(e => e.Text == "[ON] Health").OnClick!;
PhotonNetwork.IsMasterClient = false;
Check(!baseService.TrySetEnabled("Health", false) && !baseService.TrySetDrawEnabled(true), "Guests cannot mutate any Base Upgrade setting");
saved = File.ReadAllText(path);
oldBaseClick();
Check(saved == File.ReadAllText(path), "Authority checked again on stale host callback");
config.BaseUpgradeDrawEnabledEntry("Health")!.Value = false;
RoleMenu.ShowBase(config);
Check(RoleMenu.Entries.Any(e => e.Text == "[ON] Health" && e.OnClick == null), "Guest shows host state instead of conflicting local config");
Check(RoleMenu.Entries.Where(e => e.Text.StartsWith("[") || e.Text.StartsWith("Truck draw: ")).All(e => e.OnClick == null), "Guest selection controls read-only");
foreach (string malformed in new[] { "", "unavailable", "11", new string('1', 14) + "x", new string('1', 16) })
{
    PhotonNetwork.CurrentRoom.CustomProperties[BaseUpgradeSettingsSync.PropertyKey] = malformed;
    Check(BaseUpgradeSettingsSync.Read(config) == null, "Malformed host selection rejected");
}
PhotonNetwork.CurrentRoom.CustomProperties.Remove(BaseUpgradeSettingsSync.PropertyKey);
RoleMenu.ShowBase(config);
Check(RoleMenu.Entries.Any(e => e.Text == "Host Base Upgrade settings are not available yet.") && RoleMenu.Entries.All(e => !e.Text.StartsWith("[")), "Old host does not fabricate selection");
PhotonNetwork.IsMasterClient = true;
string beforePublish = BaseUpgradeSync.LocalPublishSignature(config);
config.BaseUpgradeDrawEnabledEntry("Health")!.Value = true;
Check(beforePublish != BaseUpgradeSync.LocalPublishSignature(config), "Selection-only REPOConfig changes alter publish signature");
BaseUpgradeSync.Publish(config);
Check(BaseUpgradeSettingsSync.CurrentSignature(config) == published, "Updated selection published together with levels");
GameManager.instance = null;
Check(!baseService.TrySetDrawEnabled(true), "No active game cannot change draw state");
GameManager.instance = new();
foreach (string name in names) baseService.TrySetEnabled(name, false);
RoleMenu.ShowBase(config);
Check(RoleMenu.Entries.Any(e => e.Text.StartsWith("No enabled upgrade")), "Empty selection explained");
foreach (var language in Enum.GetValues<RoleGuideLanguage>())
{
    RoleMenu.ShowBase(config, language);
    Check(RoleMenu.Entries.Count(e => e.OnClick != null) == 15, "Localized settings retain navigation, draw switch and all individual switches");
}
Console.WriteLine($"Base Upgrade settings checks passed: {checks - baseStart}");

Console.WriteLine($"Base Upgrade save adjustment checks passed: {SaveAdjustmentChecks.Run(config, file, baseService, directory)}");
