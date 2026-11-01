using REPOJP.StageRoles;
using TMPro;
using UnityEngine;
int checks = 0;
void Check(bool ok, string label) { checks++; if (!ok) throw new Exception(label); }
var cases = new Dictionary<string,RoleGuideLanguage> {
    ["en-US"] = RoleGuideLanguage.English, ["ja-JP"] = RoleGuideLanguage.Japanese,
    ["ko-KR"] = RoleGuideLanguage.Korean, ["zh-CN"] = RoleGuideLanguage.ChineseSimplified,
    ["zh-TW"] = RoleGuideLanguage.ChineseTraditional, ["zh-Hant-HK"] = RoleGuideLanguage.ChineseTraditional,
    ["zh-Hans"] = RoleGuideLanguage.ChineseSimplified, ["fr-FR"] = RoleGuideLanguage.French,
    ["de-DE"] = RoleGuideLanguage.German, ["es-ES"] = RoleGuideLanguage.Spanish,
    ["es-419"] = RoleGuideLanguage.SpanishLatinAmerica, ["pt-BR"] = RoleGuideLanguage.PortugueseBrazilian,
    ["pt-PT"] = RoleGuideLanguage.PortuguesePortugal, ["it-IT"] = RoleGuideLanguage.Italian,
    ["ru-RU"] = RoleGuideLanguage.Russian, ["pl-PL"] = RoleGuideLanguage.Polish,
    ["tr-TR"] = RoleGuideLanguage.Turkish, ["uk-UA"] = RoleGuideLanguage.Ukrainian,
    ["th-TH"] = RoleGuideLanguage.Thai, ["ar-SA"] = RoleGuideLanguage.Arabic,
    ["sv-SE"] = RoleGuideLanguage.Swedish, ["fi-FI"] = RoleGuideLanguage.Finnish,
    ["da-DK"] = RoleGuideLanguage.Danish, ["ar-001"] = RoleGuideLanguage.Arabic,
    ["es-MX"] = RoleGuideLanguage.SpanishLatinAmerica, ["hu-HU"] = RoleGuideLanguage.English
};
foreach (var entry in cases)
{
    Check(RoleLanguage.FromLocaleCode(entry.Key) == entry.Value, "Native locale mapping " + entry.Key);
    Check(RoleLanguage.FromLocaleCode(entry.Key.ToUpperInvariant().Replace('-', '_')) == entry.Value, "Case and separator " + entry.Key);
}
var nativeCodes = System.Text.Json.JsonSerializer.Deserialize<string[]>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "repo050-locales.json")))!;
Check(nativeCodes.Length == 21 && nativeCodes.Select(RoleLanguage.FromLocaleCode).Distinct().Count() == 21,
    "Each installed v0.5.0 locale maps to its own catalog, with no English fallback");
var textLayout = new TMP_Text();
foreach (var language in new[] { RoleGuideLanguage.Arabic, RoleGuideLanguage.Thai, RoleGuideLanguage.Japanese, RoleGuideLanguage.English })
{
    RoleTextLayout.Apply(textLayout, language);
    Check(textLayout.useTextShaper && textLayout.isRightToLeftText == (language == RoleGuideLanguage.Arabic), "Shaping and direction reset on locale switch");
}
foreach (string phrase in new[] { "ความเสียหายเพิ่มขึ้น", "เก็บของมีค่าไว้ในรถ", "น้ำ", "เริ่มต้น", "العربية Strength 200 HP", "日本語の説明", "Two words\nNext line" })
{
    var tokens = RoleTextLayout.Tokens(phrase);
    Check(string.Concat(tokens) == phrase, "Wrapping preserves graphemes and logical order: " + phrase);
    Check(tokens.All(token => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(token, 0) != System.Globalization.UnicodeCategory.NonSpacingMark), "No detached Thai or Arabic combining mark");
    Check(tokens.All(token => token[^1] is < '\u0e40' or > '\u0e44'), "No orphaned Thai preposed vowel");
}
Check(RoleTextLayout.Tokens("ความเสียหายเพิ่มขึ้น").Count > 3, "Thai text has wrapping opportunities without spaces");
Check(RoleTextLayout.Tokens("العربية").Count == 1, "Arabic words are not split between connected letters");
Check(RoleLanguage.FromLocaleCode(null) == RoleGuideLanguage.English, "Startup without locale");
Check(GameLocalization.Language == RoleGuideLanguage.English, "No manager during early startup");
LocalizationManager.instance = new() { ActiveLocale = new() };
StatsManager.instance = new();
string[] commands = { "Health", "Stamina", "ExtraJump", "Speed", "Strength", "Range", "Launch", "TumbleClimb", "TumbleWings", "CrouchRest", "MapPlayerCount", "DeathHeadBattery" };
foreach (string command in commands)
{
    var asset = new LocalizedAsset();
    foreach (string code in cases.Keys) asset.Values[code] = code + ":" + command;
    StatsManager.instance.upgradesInfo["playerUpgrade" + command] = new() { displayNameLocalized = asset };
}
foreach (var locale in cases)
{
    SemiFunc.Locale = locale.Key;
    Check(GameLocalization.Language == locale.Value, "Menu follows game setting");
    foreach (string command in commands)
    {
        var asset = StatsManager.instance.upgradesInfo["playerUpgrade" + command].displayNameLocalized!;
        int calls = asset.Calls;
        Check(GameLocalization.UpgradeName(command) == locale.Key + ":" + command, "Native StatsUI translation used for " + command);
        Check(GameLocalization.UpgradeName(command) == locale.Key + ":" + command && asset.Calls == calls + 1, "Translation reused within locale");
    }
    Check(GameLocalization.UpgradeName("AllUpgrades") == RoleText.Get("All Upgrades", locale.Value), "Combined result is locally translated");
}
SemiFunc.Locale = "ja-JP";
Check(GameLocalization.UpgradeName("Strength") == "ja-JP:Strength", "Switching back cannot reuse another locale's text");
Check(GameLocalization.UpgradeName("") == RoleText.Get("None", RoleGuideLanguage.Japanese), "Empty result fallback");
Check(GameLocalization.UpgradeName("FutureUpgrade") == "FutureUpgrade", "Unknown network ID remains readable");
var delayed = new LocalizedAsset { Throw = true }; delayed.Values["ja-JP"] = "遅れて読み込まれた名称";
StatsManager.instance.upgradesInfo["playerUpgradeDelayed"] = new() { displayNameLocalized = delayed };
Check(GameLocalization.UpgradeName("Delayed") == "Delayed", "Temporary native localization failure is safe");
delayed.Throw = false;
Check(GameLocalization.UpgradeName("Delayed") == delayed.Values["ja-JP"], "Failed translation is retried, not cached");
LocalizationManager.instance.ActiveLocale.RightToLeft = true;
Check(GameLocalization.RightToLeft, "Native directionality is respected for translated upgrade names");

// A pre-Unity 6 bundle retains its atlas but deserializes no TMP_Asset material.
var atlas = new Texture2D();
var oldFont = new TMP_FontAsset { atlasTexture = atlas };
Check(RoleFontMaterial.Ensure(oldFont), "Missing legacy material reconstructed");
var repaired = oldFont.material!;
Check(repaired.mainTexture == atlas && repaired.shader == Shader.Available, "Reuses the exact atlas with current shader");
Check(repaired.Values["_TextureWidth"] == 2048 && repaired.Values["_TextureHeight"] == 1024 && repaired.Values["_GradientScale"] == 6,
    "SDF sampling uses the font's actual dimensions and padding");
Check(repaired.Values["_WeightBold"] == .75f && repaired.Values["_ScaleX"] == 1 && repaired.Values["_ScaleY"] == 1, "SDF style and scale initialized");
int created = Material.Created;
Check(RoleFontMaterial.Ensure(oldFont) && Material.Created == created && oldFont.material == repaired && oldFont.Definitions == 1, "Healthy font is reused without material churn");
oldFont.material!.mainTexture = null;
Check(RoleFontMaterial.Ensure(oldFont) && oldFont.material!.mainTexture == atlas, "Missing atlas texture in material is repaired");
Check(!RoleFontMaterial.Ensure(null) && !RoleFontMaterial.Ensure(new TMP_FontAsset()), "Fonts without atlas are never attached as fallback");
Shader.Available = null;
Check(!RoleFontMaterial.Ensure(new TMP_FontAsset { atlasTexture = atlas }), "No shader fails safely");
TMP_Settings.defaultFontAsset = new() { atlasTexture = atlas, material = repaired };
Check(RoleFontMaterial.Ensure(new TMP_FontAsset { atlasTexture = atlas }), "Native default material shader is a valid fallback");
Console.WriteLine($"PASS: {checks} game-language, native upgrade-name and legacy font-material checks. Unity rendering remains a live-game check.");
