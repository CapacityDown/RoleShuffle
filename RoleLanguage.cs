using System;

namespace REPOJP.StageRoles;

internal enum RoleGuideLanguage
{
    English,
    Japanese,
    Korean,
    ChineseSimplified,
    ChineseTraditional,
    French,
    German,
    Spanish,
    PortugueseBrazilian,
    Italian,
    Russian,
    Polish,
    Turkish,
    Ukrainian
}

internal static class RoleLanguage
{
    internal static readonly string[] Names = Array.ConvertAll(
        (RoleGuideLanguage[])Enum.GetValues(typeof(RoleGuideLanguage)), NativeName);
    internal static RoleGuideLanguage Parse(string value)
    {
        int index = Array.IndexOf(Names, value);
        if (index >= 0) return (RoleGuideLanguage)index;
        // Preserve settings written before native language names were introduced.
        return Enum.TryParse(value, out RoleGuideLanguage language) && Enum.IsDefined(typeof(RoleGuideLanguage), language)
            ? language : RoleGuideLanguage.English;
    }
    internal static RoleGuideLanguage Next(RoleGuideLanguage current) =>
        (RoleGuideLanguage)(((int)current + 1) % Names.Length);
    internal static string NativeName(RoleGuideLanguage language) => language switch
    {
        RoleGuideLanguage.Japanese => "日本語",
        RoleGuideLanguage.Korean => "한국어",
        RoleGuideLanguage.ChineseSimplified => "简体中文",
        RoleGuideLanguage.ChineseTraditional => "繁體中文",
        RoleGuideLanguage.French => "Français",
        RoleGuideLanguage.German => "Deutsch",
        RoleGuideLanguage.Spanish => "Español",
        RoleGuideLanguage.PortugueseBrazilian => "Português (Brasil)",
        RoleGuideLanguage.Italian => "Italiano",
        RoleGuideLanguage.Russian => "Русский",
        RoleGuideLanguage.Polish => "Polski",
        RoleGuideLanguage.Turkish => "Türkçe",
        RoleGuideLanguage.Ukrainian => "Українська",
        _ => "English"
    };
    internal static bool NeedsTranslation(RoleGuideLanguage language) =>
        language is not RoleGuideLanguage.English and not RoleGuideLanguage.Japanese;
}
