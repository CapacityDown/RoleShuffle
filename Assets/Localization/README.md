# Localization

Menus follow the local R.E.P.O. language. All 21 locales shipped in v0.5.0 have
catalogs: ar-001, da-DK, de-DE, en-US, es-419, es-ES, fi-FI, fr-FR, it-IT,
ja-JP, ko-KR, pl-PL, pt-BR, pt-PT, ru-RU, sv-SE, th-TH, tr-TR, uk-UA, zh-CN,
and zh-TW. Spanish and Portuguese have separate regional catalogs.

The UTF-8 JSON files map English UI phrases and role-description templates to
translations. Every catalog other than Japanese contains every English entry.
Japanese role descriptions remain in RoleGuideCatalog.cs; its catalog covers UI
phrases. Role names, upgrade identifiers, chat commands, compact spoken notices
and diagnostic reports retain their existing canonical text. Roulette upgrade
names use the game's localized names. UI.GuideLanguage is retired.

Keep every numbered placeholder and its occurrence count; word order may change.
Guests translate host descriptions without changing the host's configuration
values. Unknown future descriptions remain entirely in English. Secret-role
content stays concealed until revealed. No runtime translation service, font
download or additional language data in the room payload is required.

Edit JSON translations directly. tools/collect_localization_strings.py adds
source phrases while retaining older host templates. The English catalog is the
stable key set; never delete legacy templates merely because the current host no
longer emits them. Validate with LocalizationChecks, GameUiChecks, GuideSyncChecks
and tools/check_localization_resources.py (fontTools required).

Arabic uses the game's TMP shaper for joining and bidi; Thai uses the same shaper
for marks and grapheme-aware wrapping. Fonts are local MOD fallbacks, not global
TMP changes. Font sources and licenses are recorded under Assets/Fonts/Localization.

Translations were authored for this mod. Traditional Chinese was derived from
Simplified Chinese with OpenCC's Taiwan vocabulary conversion. Regional Portuguese
and Spanish retain shared translations where appropriate. Automated checks cover
catalog completeness, placeholders, configured descriptions and glyph coverage;
native-speaker proofreading and actual in-game visual checks are separate.
