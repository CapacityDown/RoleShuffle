namespace REPOJP.StageRoles;

// Both views share the same visibility rules; detail values are supplied by the host.
internal static class RoleGuidePresentation
{
    internal static string Describe(StageRole role, RoleGuideLanguage language, bool details,
        string? configuredDetails, bool secretRevealed, string? influenzaDetails = null)
    {
        if (role is StageRole.Superbot or StageRole.Disaster)
        {
            if (!secretRevealed) return "???";
            if (details) return RoleGuideCatalog.RevealedSecretDescription(role, language, influenzaDetails);
            return RoleText.Get(role == StageRole.Superbot
                ? "Combines many roles' upgrades and abilities."
                : "Combines Bomber, Stinker, Tuna and Influenza, including their hazards.", language,
                role == StageRole.Superbot ? "多くの役職のアップグレードと能力を併せ持ちます。"
                    : "Bomber・Stinker・Tuna・Influenzaの能力と危険性を併せ持ちます。");
        }
        return details && !string.IsNullOrEmpty(configuredDetails)
            ? configuredDetails! : RoleGuideCatalog.GenericDescription(role, language);
    }
}
