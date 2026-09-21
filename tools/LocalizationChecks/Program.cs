using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using BepInEx.Configuration;
using REPOJP.StageRoles;

int checks = 0;
void Check(bool condition, string message)
{
    checks++;
    if (!condition) throw new Exception(message);
}
var languages = Enum.GetValues<RoleGuideLanguage>();
Check(languages.Length == 14, "Expected 14 supported languages");
Check(RoleLanguage.Names.Distinct().Count() == languages.Length, "Native names must be unique");
var choices = new RoleLanguageValues();
Check(choices is AcceptableValueList<string>, "REPOConfig dropdown compatibility");
foreach (var language in languages)
{
    string native = RoleLanguage.NativeName(language);
    Check(RoleLanguage.Parse(native) == language, "Native parsing: " + native);
    Check(RoleLanguage.Parse(language.ToString()) == language, "Legacy parsing: " + language);
    Check((string)choices.Clamp(language.ToString()) == native, "Legacy migration: " + language);
    Check(choices.IsValid(native), "Native acceptable value: " + native);
    Check(RoleLanguage.Parse(Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(native))) == language, "UTF8: " + native);
}
Check((string)choices.Clamp("unsupported") == "English", "Invalid settings default to English");
var current = RoleGuideLanguage.English;
foreach (var _ in languages) current = RoleLanguage.Next(current);
Check(current == RoleGuideLanguage.English, "Toggle wraps through all languages");

var config = new StageRolesConfig();
foreach (var language in languages)
{
    string revealed = RoleGuideCatalog.RevealedSecretDescription(StageRole.Disaster, language,
        RoleGuideCatalog.Description(StageRole.Influenza, config, language));
    string illness = RoleGuideCatalog.Description(StageRole.Influenza, config, language);
    Check(revealed.Contains("Influenza") && revealed.EndsWith(illness, StringComparison.Ordinal),
        "Revealed Disaster includes the complete localized Influenza ability: " + language);
    Check(RoleGuideCatalog.Description(StageRole.Disaster, config, language) == "???",
        "Disaster's additional ability remains concealed before discovery: " + language);
}
var englishCatalog = RoleText.Catalog(RoleGuideLanguage.English);
string[] PlaceholderSet(string text) => Regex.Matches(text, @"\{\d+\}").Select(m => m.Value).Distinct().Order().ToArray();
foreach (var language in languages.Where(RoleLanguage.NeedsTranslation))
{
    var catalog = RoleText.Catalog(language);
    Check(catalog.Count == englishCatalog.Count, "Catalog completeness: " + language);
    foreach (var pair in englishCatalog)
    {
        Check(catalog.ContainsKey(pair.Key), $"Missing {language}: {pair.Key}");
        Check(!string.IsNullOrWhiteSpace(catalog[pair.Key]), "Empty translation");
        Check(PlaceholderSet(pair.Key).SequenceEqual(PlaceholderSet(catalog[pair.Key])), $"Placeholders: {language}: {pair.Key}");
    }
    foreach (var role in Enum.GetValues<StageRole>())
    {
        string english = RoleGuideCatalog.Description(role, config);
        string translated = RoleGuideCatalog.Description(role, config, language);
        if (RoleOverhaulRules.GrowsWithBase(role) || role == StageRole.Lifter)
            Check(RoleText.Description(english, language) == translated,
                "Guest translates the complete growth description and cap exclusion: " + language + "/" + role);
        if (english == "???") Check(translated == "???", "Secret leaked: " + role);
        else
        {
            Check(translated != english, $"Untranslated configured role: {language}/{role}");
            foreach (string value in new[] { "17", "7.25", "200", "Expression_token" })
                Check(Regex.Matches(english, Regex.Escape(value)).Count == Regex.Matches(translated, Regex.Escape(value)).Count,
                    $"Host values changed: {language}/{role}/{value}");
        }
        string generic = RoleGuideCatalog.GenericDescription(role);
        Check(generic == "???" || RoleGuideCatalog.GenericDescription(role, language) != generic, $"Untranslated generic: {language}/{role}");
    }
    foreach (var secret in new[] { StageRole.Superbot, StageRole.Disaster })
        Check(RoleGuideCatalog.RevealedSecretDescription(secret, language) != RoleGuideCatalog.RevealedSecretDescription(secret, RoleGuideLanguage.English), "Revealed secret translation");
    string future = "Unknown future host description 1234.";
    Check(RoleText.Description(future, language) == future, "Unknown text must remain intact");
    string partial = "Increases maximum health, making the player harder to defeat. New host rule.";
    Check(RoleText.Description(partial, language) == partial, "Unknown suffix must not be partially translated");
    Check(RoleText.Description("???", language) == "???", "Secret text unchanged");
}
config.MageAutoRecoveryEnabled.Value = false;
foreach (var language in languages.Where(RoleLanguage.NeedsTranslation))
    Check(RoleGuideCatalog.Description(StageRole.Mage, config, language) != RoleGuideCatalog.Description(StageRole.Mage, config), "Mage disabled branch");

// Exercise BepInEx's real serializer and reload path, not a substitute parser.
string temp = Path.Combine(Path.GetTempPath(), "RoleShuffle-LanguageCheck-" + Guid.NewGuid() + ".cfg");
try
{
    foreach (var language in languages)
    {
        File.WriteAllText(temp, "[UI]\nGuideLanguage = " + language + "\n", new UTF8Encoding(false));
        var file = new ConfigFile(temp, false);
        var entry = file.Bind("UI", "GuideLanguage", "English", new ConfigDescription("Language", new RoleLanguageValues()));
        Check(entry.Value == RoleLanguage.NativeName(language), "Config migration: " + language);
        file.Save();
        var reloaded = new ConfigFile(temp, false);
        var saved = reloaded.Bind("UI", "GuideLanguage", "English", new ConfigDescription("Language", new RoleLanguageValues()));
        Check(saved.Value == entry.Value, "Config persistence: " + language);
    }
}
finally { if (File.Exists(temp)) File.Delete(temp); }
config.LifterHeavyGripMultiplier.Value = 2.5f;
config.LifterHeavyRotationMultiplier.Value = 0.5f;
config.InfluenzaIncubationSeconds.Value = 17;
config.InfluenzaMaximumHealth.Value = 123;
config.InfluenzaSneezeMinimumSeconds.Value = 19;
config.InfluenzaSneezeMaximumSeconds.Value = 7;
config.InfluenzaSneezeRange.Value = 7.25f;
config.InfluenzaSneezeAngle.Value = 90;
config.InfluenzaSneezeChance.Value = 80;
config.InfluenzaSneezeNoiseRadius.Value = 13;
config.InfluenzaSpeechRange.Value = 9;
config.InfluenzaSpeechAngle.Value = 110;
config.InfluenzaSpeechChance.Value = 15;
config.StinkerBreakGraceSeconds.Value = 2.5f;
foreach (var language in languages)
{
    foreach (var role in new[] { StageRole.Lifter, StageRole.Influenza, StageRole.Stinker })
    {
        string english = RoleGuideCatalog.Description(role, config);
        string translated = RoleGuideCatalog.Description(role, config, language);
        Check(!translated.Contains("{") && !translated.Contains("}"), "No unresolved configured values: " + language + "/" + role);
        if (RoleLanguage.NeedsTranslation(language))
            Check(RoleText.Description(english, language) == translated, "Host custom values translated: " + language + "/" + role);
        foreach (string value in new[] { "17", "123", "7.25", "2.5", "0.5" })
            Check(Regex.Matches(english, Regex.Escape(value)).Count == Regex.Matches(translated, Regex.Escape(value)).Count,
                "Custom value preserved: " + language + "/" + role + "/" + value);
    }
    string illness = RoleGuideCatalog.Description(StageRole.Influenza, config, language);
    Check(RoleGuideCatalog.RevealedSecretDescription(StageRole.Disaster, language, illness).EndsWith(illness), "Revealed Disaster follows host illness settings");
    Check(!RoleGuideCatalog.Description(StageRole.Lifter, config, language).Contains("shop"), "Removed Lifter item note stays absent");
}

Console.WriteLine($"Localization checks passed: {checks}");
