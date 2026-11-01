# Negotiator entitlement revocation — v5.0.0 build575

Date: 2026-11-02 (JST). Source base: 9ae903467296efd8353d4005f4085d84773484c6.

## Behavior

- Changing to an effective role without Negotiator removes the player's ticket.
  The common role-change callback covers test commands/menu, rerolls, Imitator
  changes and Influenza infection. Assignment capture reconciles active roles.
- A confirmed PlayerDeathRPC revokes the ticket in both stages and shops.
  The patch checks the native deadSet field and the runtime checks host/solo
  authority. Rejected death RPCs do not revoke rights.
- Revocation removes the ticket entirely, cancels its reserved guarantee and
  refreshes shop publication. The player no longer appears as a shop Negotiator
  and has no negotiation HUD row.
- Revocation preserves the one-grant-per-stage marker. Revival and changing back
  do not restore a revoked ticket; a later stage can grant a new one.
- Other players' rights remain intact. Normal stage completion carries surviving
  rights into the next shop; missing/disconnected assignments alone do not
  revoke tickets. Shop exit and run-reset expiry remain unchanged.
- Current guide details, all 21 translations and English/Japanese release
  documents explain death and role-change revocation. The prior published
  description key remains for older-host translation compatibility.

## Verification

- Final Release build575: zero warnings / errors.
- Negotiator runtime, death/role-change revocation, revival, shop publication,
  reservations, authority and installed-game patch checks: 80 passed.
- Source review confirmed all effective-role mutation paths use the shared
  callback; normal stage teardown does not invoke role-change revocation.
- Installed game fields: 622; v0.5.0 API/Harmony/reflection: 608.
- Guide sync: 3,237; localization: 25,216.
- Catalog/font coverage: 6,680 phrases plus Japanese key/placeholder checks.
- Actual installed TMP shaped all 668 Arabic/Thai catalog values; bundled fonts
  contain every resulting glyph.
- README: 99,942 decoded characters, UTF-8/LF.
- Seven ZIP entries match sources; 47 pinned GitHub icons verified.
- Runtime tests use game/Unity adapters. No live death, shop, role-change or
  host-only multiplayer session was performed.

## Delivery

RSO_TEST was stopped; all seven package files deployed and hash-verified.
Configuration was unchanged. Previous package/profile/configuration backup:

D:/Documents/R.E.P.O. MOD/RoleShuffle/tmp/deployments/before-build575-20261101-191855

DLL SHA-256: b191438b26da8f5d5892496367e0858771b8829113a0e6c799dcfd69e054e32f

ZIP SHA-256: 1234a8f444303ee18d8e8a5a57219d4b86b9b14abede65b36fd0d99eb524910e; 5,620,071 bytes.

RoleShuffle.zip and output/release/RoleShuffle-5.0.0.zip are identical.
No remote release or Thunderstore publication was performed.
