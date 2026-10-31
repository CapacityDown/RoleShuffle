using System;
using System.Collections.Generic;

namespace REPOJP.StageRoles;

// Display is local to each client. Network packets keep canonical upgrade IDs.
internal static class GameLocalization
{
    private static string _cachedCode = string.Empty;
    private static readonly Dictionary<string, string> UpgradeNames = new(StringComparer.Ordinal);

    internal static string Code => LocalizationManager.instance != null ? SemiFunc.LocaleGetCode() : "en-US";
    internal static RoleGuideLanguage Language => RoleLanguage.FromLocaleCode(Code);
    internal static bool RightToLeft => LocalizationManager.instance?.ActiveLocale?.RightToLeft ?? false;

    internal static string UpgradeName(string command)
    {
        string code = Code;
        if (_cachedCode != code) { UpgradeNames.Clear(); _cachedCode = code; }
        RoleGuideLanguage language = RoleLanguage.FromLocaleCode(code);
        if (string.IsNullOrEmpty(command)) return RoleText.Get("None", language);
        if (command == "AllUpgrades") return RoleText.Get("All Upgrades", language);
        if (UpgradeNames.TryGetValue(command, out string? cached)) return cached;
        try
        {
            if (LocalizationManager.instance != null && StatsManager.instance != null &&
                StatsManager.instance.upgradesInfo.TryGetValue("playerUpgrade" + command, out var info))
            {
                // Same source as vanilla StatsUI, with a cache per local locale.
                string? translated = info.displayNameLocalized?.GetLocalizedString();
                if (!string.IsNullOrWhiteSpace(translated))
                    return UpgradeNames[command] = translated!;
            }
        }
        catch (Exception)
        {
            // Localization can be temporarily unavailable during scene startup.
            // Do not cache the fallback; retry when native tables are ready.
        }
        string fallback = command switch
        {
            "ExtraJump" => "Extra Jump", "TumbleClimb" => "Tumble Climb",
            "TumbleWings" => "Tumble Wings", "CrouchRest" => "Crouch Rest",
            "MapPlayerCount" => "Map Player Count", "DeathHeadBattery" => "Death Head Battery",
            _ => command
        };
        return RoleText.Get(fallback, language);
    }
}
