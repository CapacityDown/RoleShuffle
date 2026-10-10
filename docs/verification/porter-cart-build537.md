# Porter cart unloading — build537

Date: 2026-10-10. Version 4.6.0, branch version/4.6.0.

## Behavior

Standing on an upright cargo cart starts the same proportional unloading wait
as the truck/extraction area (default: 10 seconds for weight 15, 5 seconds for 7.5).
The original stored objects return inside the ridden cart's current oriented
cargo volume and inherit its point velocity. Cart movement does not reset the wait.
Leaving or changing carts resets the wait. Existing start/completion notifications,
unloading HUD, value accounting and restoration of Speed remain in use.
Enemy damage still scatters cargo around the carrier; death/role cleanup still
returns cargo immediately. No storage is started while aboard a cart.

## Host-only implementation evidence

Installed native IL confirms that PlayerAvatar.OnPhotonSerializeView receives
clientPhysRiding and clientPhysRidingID from vanilla guests. These identify the
ridden PhotonView without a custom guest RPC. Local/solo detection uses vanilla's
current foot-contact sphere while physRiding is true, so it does not depend on a
nonzero PhotonView ID in solo play. The non-public Collider field is reflected;
compiled field-access validation caught and corrected an initial direct access.
PhysGrabCart.Start selects the child named In Cart, whose position, rotation and
localScale are used by ObjectsInCart's native OverlapBox. The release points use
that same volume, including rotated/moving carts. Inactive, overturned or missing
cargo areas cannot become cart destinations. Prior build535 map storage fix is
included. Configuration and previous release artifacts are preserved.

## Verification

- Release build537: 0 warnings, 0 errors.
- PorterChecks: 13,053, including guest/solo cart selection, proximity rejection,
  exact wait, stepping off, switching carts, moving/rotated release volume,
  velocity, empty-cargo notices, damage, role removal and invalid cart cancellation.
- Installed non-publicized game field access: 589 checks.
- Porter native contracts: 37 checks, 13 game method calls.
- GuideSyncChecks: 676 checks; native Photon Protocol18 payload 36,447 bytes.
- LocalizationChecks: 13,698; Porter translation/placeholder/glyph checks: 195.
- Guide updated in Japanese plus all 13 JSON locales; required glyphs bundled.
- README: 99,958 decoded characters, below 100,000.
- DLL SHA-256: 916b2412a70188ad0a95e10eca9cd6c880065276ebaff0d0482a088e0b74ebf8.

These are automated and installed-assembly checks. Actual cart placement, cargo
settling and host-only multiplayer need gameplay verification.

## Deployment

Build537 was deployed to RSO_TEST with R.E.P.O. stopped at 2026-10-10T10:29:37Z.
The preceding package/profile files were backed up. Seven deployed files were
hash-checked, and the user configuration was retained. Public documents were
also normalized to LF and verified against the actual decoded character count.
