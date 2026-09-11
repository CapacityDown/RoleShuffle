# RoleShuffle Release Document Guide

This document defines the release-document and package rules for RoleShuffle. It is intended to be the checklist used immediately before creating a Thunderstore release ZIP.

## 1. Release sources of truth

The following values must agree in every release.

| Value | Source files | Current value |
|---|---|---|
| Package name | `package/manifest.json` | `RoleShuffle` |
| Display name | `StageRolesPlugin.cs` | `RoleShuffle` |
| Assembly name | `StageRoles.csproj` | `RoleShuffle` |
| Plugin GUID | `StageRolesPlugin.cs` | `REPOJP.RoleShuffle` |
| Version | `StageRoles.csproj`, `StageRolesPlugin.cs`, `package/manifest.json`, latest CHANGELOG heading | `4.4.6` |
| DLL name | project output and package root | `RoleShuffle.dll` |

Do not publish when any version or name differs. The ZIP filename is not used as a source of truth; use `RoleShuffle.zip` for the release artifact unless a versioned archive is specifically required for local retention.

## 2. Required release package

The ZIP must contain these files directly at its root:

```text
RoleShuffle.zip
├── manifest.json
├── README.md
├── CHANGELOG.md
├── icon.png
└── RoleShuffle.dll
```

The ZIP must not contain an enclosing `package` or `RoleShuffle` directory. It must not contain source files, `bin`, `obj`, PDB files, configuration files, logs, temporary validation files, or another ZIP.

Project policy treats all five files above as mandatory. `manifest.json`, `README.md`, and `icon.png` are Thunderstore package metadata; `CHANGELOG.md` and `RoleShuffle.dll` are also required for a usable RoleShuffle release.

## 3. `manifest.json`

Use valid UTF-8 JSON without comments or trailing commas. Keep the fields in this order:

```json
{
  "name": "RoleShuffle",
  "version_number": "4.4.6",
  "website_url": "https://thunderstore.io/c/repo/p/CapackMods/RoleShuffle/",
  "description": "【HostOnlyMOD】Random roles shake up every stage with vanilla upgrades and effects—only the host needs the mod! ホスト導入だけで、ステージごとにランダムな役職とバニラの強化・効果を楽しめます！",
  "dependencies": [
    "BepInEx-BepInExPack-5.4.2305",
    "nickklmao-REPOConfig-1.2.6"
  ]
}
```

Rules:

- `name` remains `RoleShuffle` unless the package is intentionally renamed.
- `version_number` uses three numeric components: `major.minor.patch`.
- `website_url` points to the canonical Thunderstore package page once it exists. An empty value is acceptable only before that URL is available.
- `description` follows the CapackMods format: `【HostOnlyMOD】` + concise English summary + concise Japanese summary.
- Describe the player-facing result, not implementation details.
- BepInEx and REPOConfig remain hard dependencies.
- Stage Flux is a soft, optional integration and must not be added to `dependencies` unless it becomes mandatory.
- Do not add development-only libraries or game assemblies to `dependencies`.

## 4. `README.md`

### Required structure

Use English first and Japanese second. The two sections must describe the same released behavior.

```text
# RoleShuffle

## English
### Overview
### Requirements
### Installation
### Multiplayer
### How roles are assigned
### Roles
### Testing commands
### Configuration
### Notifications and HUD
### Compatibility
### Gameplay notes

## 日本語
### 概要
### 必須MOD
### 導入方法
### マルチプレイ
### 役職の抽選
### 役職一覧
### テストコマンド
### 設定
### 通知とHUD
### 互換性
### ゲームプレイ上の注意
```

### Content requirements

- Open with the result: every player receives a random role at stage start and the role is cleared at stage end.
- State prominently that only the host needs the mod for gameplay effects and vanilla participants are supported.
- Explain that participants who also install RoleShuffle can use the synchronized role HUD.
- List every released role. Keep the role count, role table, and implementation in agreement.
- Use a role table with these columns:
  - Role
  - Default upgrade or effect
  - Important limitation or risk
  - Configurable values
- Do not document internal testing commands in release documentation.
- Document every REPOConfig entry, including its exact key, default, allowed range or choices, effect, and whether it is host-controlled or local UI configuration.
- State that upgrade items are removed from the shop by default when that remains the released behavior.
- Describe Stage Flux as optional compatibility. Include the deterministic revival priority: Stage Flux Second Chance is evaluated before RoleShuffle Phoenix; if Second Chance activates, Phoenix is preserved.
- Include concise warnings for roles that can damage players or create hazards, including Bomber, Jobless, Tuna, and Mage.
- Explain Medic self-exclusion, Phoenix and Rescuer limits, and any multiplayer-only restrictions that materially affect users.
- State that role Health upgrades persist outside stages until the next role assignment, change by level difference without resetting to zero, and are not continuously monitored.

### Style rules

- Write for mod users, not developers.
- Use in-game English names consistently in both language sections.
- Keep internal class names, Harmony patches, RPC names, reflection, Prefab resolution, hashes, and historical implementation details out of the README.
- Use backticks for configuration keys, literal role names where needed, and numeric examples.
- Use tables for roles and configuration because they contain repeated fields.
- Do not claim that a feature is synchronized to vanilla participants unless it has been tested through the host-only path.
- Do not describe unreleased work, planned roles, or local test commands that are not included in the packaged DLL.

## 5. `CHANGELOG.md`

Use English only and preserve the established RoleShuffle format:

```markdown
## 4.4.6

- Added ...
- Changed ...
- Fixed ...
```

Rules:

- Put the newest version first.
- Use the exact release version without a leading `v`, matching the current RoleShuffle convention.
- Keep earlier published sections unchanged except to correct a factual error.
- Use short, user-visible `Added`, `Changed`, `Fixed`, `Improved`, or `Removed` statements.
- Consolidate related implementation work into one user-facing bullet.
- Describe each version's final net changes relative to the previous released version. Do not list intermediate changes, reversals, or fixes to features introduced within the same version; incorporate their final behavior into the feature summary.
- Do not include Japanese, development diary entries, debugging steps, private field names, method names, raw stack traces, or build hashes.
- Mention compatibility changes when they alter observable behavior.
- Ensure role counts and configuration ranges match the release.

## 6. `icon.png`

- File name must be exactly `icon.png` and use PNG format.
- Use a square 256 × 256 image suitable for Thunderstore.
- Keep the important subject readable at small sizes.
- Avoid fine text, transparent empty margins, accidental UI frames, or imagery that suggests a feature not present in RoleShuffle.
- Prefer a visual that communicates shuffled player roles and fits the R.E.P.O. world.
- Keep a high-quality editable source outside the release ZIP if one exists; package only the final PNG.

## 7. `RoleShuffle.dll`

- Build the `Release` configuration from `StageRoles/StageRoles.csproj`.
- Package only `bin/Release/netstandard2.1/RoleShuffle.dll`.
- Copy the newly built DLL to `StageRoles/package/RoleShuffle.dll` immediately before creating the ZIP.
- Compare SHA-256 hashes after copying; both files must match.
- Do not package a DLL copied from the Default test profile because its origin can be ambiguous.
- Do not include PDB, XML documentation, game DLLs, BepInEx, REPOConfig, or Stage Flux DLLs.

## 8. Version-change procedure

For every release, update these locations to the same version:

1. `<Version>` in `StageRoles.csproj`.
2. `PluginVersion` in `StageRolesPlugin.cs`.
3. `version_number` in `package/manifest.json`.
4. The newest version heading in `package/CHANGELOG.md`.

The README normally does not display the package version. Avoid adding a version badge that can become stale.

`RoleMenu.RoleUiBuildNumber` is a separate Roles UI-only counter. Increment it immediately before every `StageRoles.csproj` build, including a build that later fails. Do not append this counter to `PluginVersion`, DLL metadata, startup logs, `manifest.json`, README, or CHANGELOG.

## 9. Release validation checklist

### Documents and metadata

- [ ] The four version locations match.
- [ ] `manifest.json` parses as JSON and contains only released dependencies.
- [ ] The host-only description is present in English and Japanese.
- [ ] README English and Japanese sections describe the same behavior.
- [ ] Every released role is listed once in each role catalog.
- [ ] Role counts agree everywhere, and internal testing commands are not documented.
- [ ] Every visible REPOConfig entry has a default and explanation.
- [ ] CHANGELOG is English-only, newest-first, and user-facing.
- [ ] `icon.png` exists and is exactly 256 × 256.

### Build and behavior

- [ ] R.E.P.O. is closed before replacing a deployed DLL.
- [ ] Release build completes with zero errors and zero warnings.
- [ ] Packaged DLL hash matches the Release build DLL hash.
- [ ] A clean Default profile loads the packaged DLL without errors or warnings attributable to RoleShuffle.
- [ ] Host-only multiplayer is tested with at least one vanilla participant.
- [ ] A participant with RoleShuffle installed is tested for synchronized HUD behavior.
- [ ] Stage start assignment, stage end cleanup, and shop filtering are tested.
- [ ] Notification TTS does not attract enemies while ordinary voice and world sounds remain detectable.
- [ ] Stage Flux is tested both absent and present.
- [ ] With Stage Flux present, Second Chance, Phoenix, and ordinary failed-stage transitions follow the documented priority and preserve or clear both mods correctly.

### ZIP

- [ ] The ZIP contains exactly one copy of each required root file.
- [ ] Opening the ZIP shows `manifest.json` at the root, not inside another folder.
- [ ] The ZIP contains no source, PDB, config, log, temporary, or nested archive files.
- [ ] The final ZIP installs and launches from a clean profile.

## 10. Readiness tracking

Use the validation checklist above against the current build and package. Do not keep release-specific hashes, role counts, or temporary blockers in this reusable guide because they become stale.
