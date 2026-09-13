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
var service = new RoleSelectionSettings(config, file);
StageRolesPlugin.Instance.RoleSettings = service;
var roles = Enum.GetValues<StageRole>();
Check(roles.Length == 42 && (int)StageRole.Superbot == 1001 && (int)StageRole.Disaster == 1002, "Existing role identifiers are retained");
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
