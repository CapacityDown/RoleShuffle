# Negotiator HUD after role changes — v5.0.0 build572

Date: 2026-11-02 (JST). Source base: b02e82d0fdc3079e48dd3c398c2a9adeb84b0349.

## Cause and change

The stage ability publisher added Negotiation whenever a retained shop right
existed, even after the effective role changed. Because the new snapshot used
the new role identity, normal stale-role filtering correctly accepted it.

Stage negotiation status now also requires the current effective role to have
the Negotiator capability. Changing to an unrelated role removes that resource
on the next status/HUD refresh. Effective copies and inherited capabilities
follow the existing RoleCatalog rules. Earned rights remain in NegotiatorState;
the independent shop publisher still displays unused 1/1 and consumed 0/1
rights until that shop ends.

## Verification

- Source review: the stage metric checks the current capability before reading
  the retained right; shop publication and entitlement mutations are unchanged.
- Release build572: zero warnings / errors.
- Installed game fields: 622; v0.5.0 API/Harmony/reflection: 605.
- Negotiator runtime, retained rights, shop display and installed-game patches:
  56 existing checks, using lightweight native/Unity adapters.
- Overhaul/resource selection/codec/synchronization suite: 84,371 checks,
  including stale-role rejection and cleanup.
- Seven ZIP entries match sources; 47 pinned GitHub icons verified.
- README: 99,950 decoded characters, UTF-8/LF.
- No live Unity role-change, HUD rendering or multiplayer test was performed.

## Delivery

All seven package files were deployed to RSO_TEST while the game was stopped.
Configuration was unchanged. Previous package/profile/configuration backup:

D:/Documents/R.E.P.O. MOD/RoleShuffle/tmp/deployments/before-build572-20261101-190641

DLL SHA-256: aba756ec300f40f985ebd699b55aa7e3fb890311cd58aaf88d89e9b7a4c15f8a

ZIP SHA-256: e210b9056883352f1a1795faf5b85330486e84b77443670e185b13ba5a532b4b; 5,618,739 bytes.

RoleShuffle.zip and output/release/RoleShuffle-5.0.0.zip are identical.
No remote release or Thunderstore publication was performed.
