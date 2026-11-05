using System;
using System.Collections.Generic;

namespace REPOJP.StageRoles;

// Display is local to each client. Network packets keep canonical upgrade IDs.
internal static class GameLocalization
{
    private static string _cachedCode = string.Empty;
    private static readonly Dictionary<string, string> UpgradeNames = new(StringComparer.Ordinal);

    private static LocalizationManager? _manager;
    private static StatsManager? _stats;
    private static int _revision;

    internal static string Code { get { SynchronizeSources(); return _cachedCode; } }
    internal static int Revision { get { SynchronizeSources(); return _revision; } }

    private static void Invalidate()
    {
        UpgradeNames.Clear();
        unchecked { _revision++; }
    }

    private static void SynchronizeSources()
    {
        LocalizationManager? manager = LocalizationManager.instance;
        if (!ReferenceEquals(_manager, manager))
        {
            if (_manager != null) _manager.RemoveLocalizationChangedCallback(Invalidate);
            _manager = manager;
            if (_manager != null) _manager.AddLocalizationChangedCallback(Invalidate);
            Invalidate();
        }
        string code = manager != null ? SemiFunc.LocaleGetCode() : "en-US";
        StatsManager? stats = StatsManager.instance;
        if (_cachedCode != code || !ReferenceEquals(_stats, stats))
        {
            _cachedCode = code;
            _stats = stats;
            Invalidate();
        }
    }

    internal static void Reset()
    {
        if (_manager != null) _manager.RemoveLocalizationChangedCallback(Invalidate);
        _manager = null;
        _stats = null;
        _cachedCode = string.Empty;
        Invalidate();
    }
    internal static RoleGuideLanguage Language => RoleLanguage.FromLocaleCode(Code);
    internal static bool RightToLeft => LocalizationManager.instance?.ActiveLocale?.RightToLeft ?? false;

    internal static string UpgradeName(string command)
    {
        string code = Code;
        RoleGuideLanguage language = RoleLanguage.FromLocaleCode(code);
        if (string.IsNullOrEmpty(command)) return RoleText.Get("None", language);
        if (command == "AllUpgrades") return RoleText.Get("All Upgrades", language);
        if (UpgradeNames.TryGetValue(command, out string? cached)) return cached;
        try
        {
            if (LocalizationManager.instance != null && StatsManager.instance != null &&
                StatsManager.instance.upgradesInfo.TryGetValue("playerUpgrade" + command, out var info))
            {
                // Same source as vanilla StatsUI; native callbacks also refresh same-locale overrides.
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
