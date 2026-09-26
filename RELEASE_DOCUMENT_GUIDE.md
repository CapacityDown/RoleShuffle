# RoleShuffle Release Document Guide

This document defines the release-document and package rules for RoleShuffle. It is intended to be the checklist used immediately before creating a Thunderstore release ZIP.

The ZIP and document workflow is inherited from [RoleShuffle-S002](codex://threads/01a091a3-dc1e-7463-bad9-ef2e5a5fd3d9). Later explicit instructions in the current task take precedence; release versions, settings and behavior must come from the current release sources.

## Player-facing document policy

These rules apply to README, CHANGELOG and player-facing PDFs:

- Explain the current released abilities, controls, settings and player-visible limitations in plain language.
- Do not include the development history within a version: experiments, reversals, tuning steps, same-version fixes or document rewrites. README and role guides describe current behavior; CHANGELOG describes only the final differences from the previous published release.
- Do not include implementation details such as code names, networking/RPC mechanics, physical coefficients, internal mass thresholds, caches, migration algorithms, polling counts, build numbers or debugging procedures. Keep the setting names, defaults, ranges, installation steps and game values players need to use the mod.
- README must contain at most 100,000 decoded characters and use UTF-8 without a BOM. Validate the source before packaging and verify that the archived README matches it.
- Never add a CHANGELOG entry for an edit to unpublished release documentation.

## 1. Release sources of truth

The following values must agree in every release. At the start of a new development version, keep the previous CHANGELOG intact until there is a player-facing change to describe; do not create an empty version heading. Preserve the previous release ZIP until a new release package is built and validated.

| Value | Source files | Current value |
|---|---|---|
| Package name | `package/manifest.json` | `RoleShuffle` |
| Display name | `StageRolesPlugin.cs` | `RoleShuffle` |
| Assembly name | `StageRoles.csproj` | `RoleShuffle` |
| Plugin GUID | `StageRolesPlugin.cs` | `REPOJP.RoleShuffle` |
| Version | `StageRoles.csproj`, `StageRolesPlugin.cs`, `package/manifest.json`, latest CHANGELOG heading | `4.5.3` |
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
├── RoleShuffle.dll
└── LICENSES/
    ├── NotoCJK-OFL.txt
    └── NotoSans-OFL.txt
```

The ZIP must not contain an enclosing `package` or `RoleShuffle` directory. It must not contain source files, `bin`, `obj`, PDB files, configuration files, logs, temporary validation files, or another ZIP.

Project policy treats the five root files above as mandatory. Include the two font license notices under `LICENSES/` while those fonts are bundled. `manifest.json`, `README.md`, and `icon.png` are Thunderstore package metadata; `CHANGELOG.md` and `RoleShuffle.dll` are also required for a usable RoleShuffle release.

## 3. `manifest.json`

Use valid UTF-8 JSON without comments or trailing commas. Keep the fields in this order:

```json
{
  "name": "RoleShuffle",
  "version_number": "4.5.3",
  "website_url": "https://github.com/CapacityDown/RoleShuffle/issues",
  "description": "【HostOnlyMOD】Random roles shake up every stage with vanilla upgrades and effects—only the host needs the mod! ホスト導入だけで、ステージごとにランダムな役職とバニラの強化・効果を楽しめます！",
  "dependencies": [
    "BepInEx-BepInExPack-5.4.2305",
    "nickklmao-MenuLib-2.5.2",
    "nickklmao-REPOConfig-1.2.6"
  ]
}
```

Rules:

- `name` remains `RoleShuffle` unless the package is intentionally renamed.
- `version_number` uses three numeric components: `major.minor.patch`.
- `website_url` points to `https://github.com/CapacityDown/RoleShuffle/issues`. README contact links and generated catalog contact URLs use the same Issue page.
- `description` follows the CapackMods format: `【HostOnlyMOD】` + concise English summary + concise Japanese summary.
- Describe the player-facing result, not implementation details.
- BepInEx, MenuLib and REPOConfig remain hard dependencies.
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
### Role command
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
### 役職確認コマンド
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
- Include concise warnings for roles that can damage players or create hazards, including Bomber, Courier, Tuna, and Mage.
- Explain Medic self-exclusion, Phoenix and Rescuer limits, and any multiplayer-only restrictions that materially affect users.
- Explain when Base and role upgrades apply or end using the released gameplay rules; omit the update algorithm and internal checks.

### Style rules

- Write for mod users, not developers.
- Use in-game English names consistently in both language sections. Role-name columns in role lists must contain only the English role name, without Japanese names or parenthetical translations.
- Keep README and CHANGELOG within Thunderstore's 100,000-character limit, encoded as UTF-8 without a BOM. The limit counts decoded characters, not UTF-8 bytes. Prefer shared explanations for repeated settings while retaining every key, default, range, effect, and control scope.
- Keep internal class names, Harmony patches, RPC names, reflection, Prefab resolution, hashes, and historical implementation details out of the README.
- Use backticks for configuration keys, literal role names where needed, and numeric examples.
- Use tables for roles and configuration because they contain repeated fields.
- Do not claim that a feature is synchronized to vanilla participants unless it has been tested through the host-only path.
- Do not describe unreleased work, planned roles, or local test commands that are not included in the packaged DLL.

## 5. `CHANGELOG.md`

Use English only and preserve the established RoleShuffle format:

```markdown
## 4.5.3
- Added ...
- Changed ...
- Fixed ...
```

Rules:

- Put the newest version first.
- Start the first change on the line immediately after each version heading, without a blank line. Keep a blank line between version sections.
- Use the exact release version without a leading `v`, matching the current RoleShuffle convention.
- Preserve earlier published release history. Correct factual errors or reword technical explanations into player-facing effects when requested; do not add unpublished development history.
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

- Build the `Release` configuration from `StageRoles.csproj` in the active release checkout. For document-only changes or repackaging, reuse the verified Release DLL when its source has not changed; do not rebuild just to create a ZIP.
- Package only `bin/Release/netstandard2.1/RoleShuffle.dll`.
- Copy the verified Release DLL to `package/RoleShuffle.dll` immediately before creating the ZIP.
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
- [ ] `python tools/check_release_markdown.py` passes before creating the ZIP; check the archived Markdown against the validated source files.
- [ ] Every released role is listed once in each role catalog.
- [ ] Role counts agree everywhere, and internal testing commands are not documented.
- [ ] Every visible REPOConfig entry has a default and explanation.
- [ ] CHANGELOG is English-only, newest-first, and user-facing, with no empty line after a version heading.
- [ ] Player-facing documents contain no within-version development history or implementation details.
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

- [ ] The ZIP contains exactly one copy of each required root file and each bundled font license.
- [ ] Every archived entry matches its validated source by SHA-256; record the ZIP checksum outside the ZIP.
- [ ] Earlier release ZIPs are retained before replacing the current `RoleShuffle.zip`.
- [ ] Opening the ZIP shows `manifest.json` at the root, not inside another folder.
- [ ] The ZIP contains no source, PDB, config, log, temporary, or nested archive files.
- [ ] The final ZIP installs and launches from a clean profile.

## 10. Readiness tracking

Use the validation checklist above against the current build and package. Do not keep release-specific hashes, role counts, or temporary blockers in this reusable guide because they become stale.


## 11. Document handoff and PDF workflow

- Keep README in English followed by Japanese; keep CHANGELOG in English only. Both must describe the packaged release and pass `tools/check_release_markdown.py` before packaging.
- Compare the newest CHANGELOG section against the previous **published release**, not an intermediate build or unused development version. Remove empty, unpublished version headings from the release history. Preserve published release facts. Do not list same-version trial changes, reversals, description edits or repairs as separate release changes; summarize the resulting feature once.
- Include newly added language names when a release adds language support. Keep contact and manifest links on the repository's Issues page.
- Deliver the upload archive as `RoleShuffle.zip`. A versioned copy may be retained locally with an external checksum and release record. Update ZIP contents whenever release Markdown changes.
- Build specification PDFs from the target release ZIP and matching implementation. Extract configuration keys, defaults and ranges from that implementation; do not carry historical counts or values into a new version.
- Player-facing PDFs cover roles, settings, UI/HUD, controls, who can change settings, saved preferences, compatibility and gameplay limitations. Omit technical explanations and development or verification history. Identify the target release, embed fonts, check links and page flow, and render and visually inspect all pages before delivery.
- Keep the detailed role-list PDF and the public PDF in parallel. Current instructions require the public copy to mask secret role names, descriptions and artwork; do not inherit the earlier session's decision to expose secrets in a development specification as the policy for public role lists.
- If remote document delivery is requested, publish the requested artifact and verify an unauthenticated download against the source SHA-256. Preserve binary PDF bytes when committing. Do not treat creating a local ZIP or PDF as a request to publish a release.
- Record build/static checks separately from actual game and multiplayer checks. Do not claim a fresh in-game test when only archived files, source or automated checks were inspected.
