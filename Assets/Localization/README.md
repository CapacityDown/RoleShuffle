# Localization

The UTF-8 JSON catalogs map English UI phrases and role-description templates to
translations. The 12 added locales contain all 170 catalog entries. Japanese role
descriptions remain in `RoleGuideCatalog.cs`; its UI catalog contains 86 entries.
Role names, upgrade identifiers, chat commands and diagnostic reports stay in English.
The English catalog is regenerated with `python tools/collect_localization_strings.py`.
Edit the target JSON directly when revising a translation.

Keep each numbered placeholder (`{0}`, `{1}`, etc.) exactly once unless the source
already repeats it. Word order can change. The guest translates the host's existing
English V2/V1 descriptions, preserving values received from the host. A description
containing an unknown phrase stays entirely in English. No translation service or
network request is used at runtime, and no language data is added to the room payload.

The language setting stores native names in UTF-8. `RoleLanguageValues` migrates
older English enum identifiers while retaining REPOConfig's string dropdown.
Run `dotnet run --project tools/LocalizationChecks/LocalizationChecks.csproj -c Release`
and the existing OptimizationChecks and UtilityRuntimeChecks after edits.

Translations were authored for this mod. Traditional Chinese was derived from
Simplified Chinese with OpenCC's Taiwan vocabulary conversion and checked for
placeholder consistency. Native-speaker proofreading and gameplay UI checks remain
recommended before describing the translations as professionally reviewed.
