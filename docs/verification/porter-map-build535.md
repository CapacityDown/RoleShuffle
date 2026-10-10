# Porter map-marker storage correction — build535

Date: 2026-10-10. Version 4.6.0, branch version/4.6.0.

## Report and confirmed mechanism

The user clarified that invisible valuables appear on the standard map after
Porter stores cargo. The live RSO_TEST log showed successful storage, spilling
and unloading, without a corresponding exception.

Installed game IL confirms:
- AssetManager.physDisabledPosition defaults to (0, 3000, 0).
- PhysGrabObject.OverrideDeactivate moves stored objects to that position.
- OverrideTimersTick also moves an object there when becoming inactive.
- Map.CustomPositionSet projects target X/Z onto the map and discards height.
- MapValuable.Logic keeps displaying a marker while its target still exists.

Thus storage created a real map marker at the entrance X/Z, although the
original valuable was parked 3000 units above the playable level.

## Correction

PorterStorage enters native storage once, preserving its drone-release effects.
It then keeps the original object at (10000, 3000, 10000), outside the playable
map projection, using native synchronized Teleport. No asset-wide parking
position is changed and no custom RPC is required by vanilla guests.

The existing per-object native deactivation timer and inactive flag are refreshed
through validated reflection, with kinematic and collision state maintained.
This avoids a repeated return to the vanilla origin or repeated teleports.
Normal release uses OverrideDeactivateReset and the original object's position
and rotation, restoring map tracking at the drop/unload location.

## Verification

- Release build535: 0 warnings, 0 errors.
- PorterChecks: 12,834 checks, including native-style origin parking,
  120 timer/maintenance cycles, no repeated parking teleports, cargo values,
  spill/unload/role cleanup and map-position restoration.
- Installed-game field access: 582 checks.
- Porter native API/storage/network contracts: 29 checks.
- Prior test fixture's deactivation position was corrected to the actual
  vanilla (0, 3000, 0); its old off-map placeholder masked this regression.
- DLL SHA-256: ee7d64e141a8c526521197f2fbba047aa05b604aea521d3e7c3736e7c5362d65.

Game appearance and unmodded-peer behavior have not been re-tested in gameplay.
Existing package/profile backups and earlier release ZIPs are preserved.

## Deployment status

The package DLL has been updated and its hash verified. RSO_TEST still uses
build534 because R.E.P.O. was running during preparation. Deployment is pending
game shutdown; the running process was not interrupted.
