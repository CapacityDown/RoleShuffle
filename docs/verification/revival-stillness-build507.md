# Rescuer and Phoenix revival conditions — v4.5.4 build507

Date: 2026-10-08 (Asia/Tokyo)

## Result

- Rescuer and Phoenix require a continuously still Death Head before requesting
  revival. The host observes linear and angular velocity plus position and
  rotation drift for 0.5 seconds; tiny physics jitter is tolerated.
- Motion, head replacement, a missing/inactive head or Rigidbody, living-player
  state and observation gaps reset the stillness observation. Physics is not
  modified. Both roles recheck eligibility immediately before requesting revival.
- Rescuer selects the nearest eligible stationary head. Waiting consumes no use.
- Phoenix keeps a failed-stage transition pending without a timeout while an
  unused revival or its acknowledgement is outstanding. A queued failure after
  successful revival is also rejected. A later death with no remaining use is
  not protected. Player removal and stage cleanup release the wait.
- Removed the obsolete Phoenix FailureGraceSeconds binding and public setting
  description. Existing user configuration files were not edited.
- Stage Flux Second Chance retains priority. The common death-head revival
  method is unchanged, so Bodyguard corrective revivals have no new stillness gate.
- Updated the role guide in all 14 supported languages and the public README.

## Verification

- Production-method regression harness: 6,579 assertions passed. It covers moving,
  rotating, drifting and replaced heads, 30/60/144 FPS sampling, nearest eligible
  rescue selection, preserved uses, five-minute Phoenix waits, delayed remote
  acknowledgement, duplicate requests, Stage Flux priority and cleanup.
- Localization checks: 12,999 passed.
- Release build: zero warnings and errors. Game field access: 515 references
  checked against the installed, non-publicized game assembly.
- README: 99,284 characters; CHANGELOG: 17,485 characters; UTF-8 without BOM.
- Build, package and RSO_TEST DLL SHA-256:
  `0c30aa524ed9e43f8777433a816adedea25fee6bf8363622af5bb21997709e09`.
- RSO_TEST deployment completed with the game closed; all seven package files
  matched their source hashes. Previous profile files are preserved under
  `tmp/deployments/before-build507-20261007-181556`.
- The prior v4.5.3 release ZIP remains unchanged. No new release ZIP was created.

The regression harness uses deterministic physics and transport stand-ins.
No fresh game or host-only multiplayer test was performed.
