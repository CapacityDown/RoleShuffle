using BepInEx.Configuration;

namespace REPOJP.StageRoles;

// REPOConfig recognizes this as a normal string dropdown. Clamp migrates the
// old English identifiers before BepInEx would otherwise replace them with English.
internal sealed class RoleLanguageValues : AcceptableValueList<string>
{
    internal RoleLanguageValues() : base(RoleLanguage.Names) { }
    public override object Clamp(object value) =>
        RoleLanguage.NativeName(RoleLanguage.Parse(value as string ?? string.Empty));
}
