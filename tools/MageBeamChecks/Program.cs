using Photon.Pun;
using REPOJP.StageRoles;
using UnityEngine;

int checks = 0;
void Check(bool condition, string message) { if (!condition) throw new Exception(message); checks++; }
void Near(Vector3 actual, Vector3 expected, string message) => Check((actual - expected).sqrMagnitude < 0.00001f, message);
GameObject Staff()
{
    var root = new GameObject();
    root.AddComponent<ValuableWizardStaff>();
    root.AddComponent<Rigidbody>();
    root.AddComponent<PhysGrabObject>();
    root.AddComponent<PhotonView>();
    return root;
}

foreach (object[]? data in new object[]?[] { null, Array.Empty<object>(), new object[] { 1 }, new object[] { "other" }, new object[] { MageBeamCarrier.SpawnMarker, 1 } })
{
    var ordinary = Staff();
    ordinary.GetComponent<PhotonView>()!.InstantiationData = data;
    var ordinaryMesh = ordinary.Child().AddComponent<Renderer>();
    MageBeamCarrierStartPatch.Postfix(ordinary.GetComponent<ValuableWizardStaff>()!);
    Check(ordinary.GetComponent<MageBeamCarrier>() == null && ordinaryMesh.enabled && ordinary.GetComponent<Rigidbody>()!.useGravity,
        "Unmarked real valuables keep their appearance and physics");
    Check(MageBeamCarrierRecoilPatch.Prefix(ordinary.GetComponent<ValuableWizardStaff>()!), "Real staff recoil remains enabled");
    MageBeamCarrierAimPatch.Prefix(ordinary.GetComponent<ValuableWizardStaff>()!);
    Check(ordinary.GetComponent<MageBeamCarrier>() == null, "Ordinary staff aiming remains native");
}

foreach (bool remote in new[] { false, true })
{
    SemiFunc.Authority = !remote;
    var root = Staff();
    root.transform.position = new(12, 7, -4);
    root.transform.rotation = new(0, 0.707f, 0, 0.707f);
    var expectedPosition = root.transform.position;
    var expectedRotation = root.transform.rotation;
    var mesh = root.Child();
    var staffMesh = mesh.AddComponent<Renderer>();
    var staffParticles = mesh.Child().AddComponent<Renderer>();
    var staffLight = mesh.Child().AddComponent<Light>();
    var staffCollider = root.Child().AddComponent<Collider>();
    var laser = root.Child(); laser.AddComponent<SemiLaser>();
    var beamRenderer = laser.Child().AddComponent<Renderer>();
    var beamHitRenderer = laser.Child().AddComponent<Renderer>();
    var beamLight = laser.Child().AddComponent<Light>();
    var hurt = laser.Child(); hurt.AddComponent<HurtCollider>();
    var beamCollider = hurt.AddComponent<Collider>();
    var body = root.GetComponent<Rigidbody>()!;
    var phys = root.GetComponent<PhysGrabObject>()!;
    if (remote)
    {
        root.GetComponent<PhotonView>()!.InstantiationData = new object[] { MageBeamCarrier.SpawnMarker };
        MageBeamCarrierStartPatch.Postfix(root.GetComponent<ValuableWizardStaff>()!);
    }
    else MageBeamCarrier.Attach(root);
    var marker = root.GetComponent<MageBeamCarrier>()!;
    Check(marker != null, "Host and modded guests identify the temporary beam carrier");
    Check(marker!.BeamDurationSeconds == 5f, "Carrier defaults to five seconds");
    Check(!MageBeamCarrierRecoilPatch.Prefix(root.GetComponent<ValuableWizardStaff>()!), "Only generated carrier skips native beam recoil");
    // Simulate native initialization/timer expiry, grab/impact displacement,
    // and visual refreshes beyond the maximum configurable 30-second beam.
    for (int frame = 0; frame < 1600; frame++)
    {
        body.isKinematic = false; body.useGravity = true;
        body.velocity = new(1, -8, 3); body.angularVelocity = new(0, 4, 0);
        body.constraints = RigidbodyConstraints.None;
        phys.KinematicSeconds = phys.GravitySeconds = phys.GrabDisableSeconds = 0;
        root.transform.position = body.position = new(1, -10, 9);
        root.transform.rotation = body.rotation = new(1, 0, 0, 0);
        if (remote)
        {
            // Model successive native network poses rather than freezing guests
            // at the initial cast position as the old implementation did.
            expectedPosition = root.transform.position = body.position = new(frame * 0.1f, 2, -frame * 0.02f);
            expectedRotation = root.transform.rotation = body.rotation = Quaternion.LookRotation(new(1, frame * 0.001f, 1));
        }
        staffMesh.enabled = staffParticles.enabled = staffLight.enabled = staffCollider.enabled = true;
        marker!.Maintain();
        Check(!staffMesh.enabled && !staffParticles.enabled && !staffLight.enabled && !staffCollider.enabled, "Carrier remains invisible and ungrabbable");
        Check(beamRenderer.enabled && beamHitRenderer.enabled && beamLight.enabled && beamCollider.enabled, "Beam rendering, light and damage stay active");
        Check(body.isKinematic && !body.useGravity && body.constraints == RigidbodyConstraints.FreezeAll && body.velocity == Vector3.zero && body.angularVelocity == Vector3.zero, "Gravity and momentum cannot move carrier");
        Check(body.position == expectedPosition && root.transform.position == expectedPosition && body.rotation == expectedRotation && root.transform.rotation == expectedRotation,
            remote ? "Guests preserve received origin and direction" : "Unbound host carrier resists physics displacement");
        Check(phys.KinematicSeconds > 0 && phys.GravitySeconds > 0 && phys.GrabDisableSeconds > 0, "Native overrides are continuously refreshed");
    }
    MageBeamCarrier.Attach(root);
    Check(root.Components.OfType<MageBeamCarrier>().Count() == 1, "Native Start does not duplicate the marker or recapture a moved pose");
}
SemiFunc.Authority = true;
var caster = new GameObject().AddComponent<PlayerAvatar>();
caster.localCamera = new();
caster.playerAvatarVisuals = new() { headLookAtTransform = new GameObject().transform };
caster.playerTransform = caster.transform;
caster.transform.rotation = Quaternion.LookRotation(new(-1, 0, 0));
var aimingRoot = Staff();
var aimingMarker = MageBeamCarrier.Attach(aimingRoot);
Vector3 laserOffset = new(0.3f, 1.2f, -0.4f);
Quaternion laserRotation = Quaternion.LookRotation(new(1, 0.25f, 0.1f));
aimingMarker.Follow(caster, laserOffset, laserRotation);
for (int frame = 0; frame < 300; frame++)
{
    // Turn, pitch and walk while the avatar body's direction remains unrelated.
    Vector3 direction = new Vector3(MathF.Sin(frame * 0.1f), MathF.Sin(frame * 0.06f), MathF.Cos(frame * 0.1f)).normalized;
    Vector3 head = new(frame * 0.01f, 3 + frame * 0.02f, -frame * 0.03f);
    caster.localCamera.Aim.rotation = Quaternion.LookRotation(direction);
    caster.playerAvatarVisuals.headLookAtTransform!.position = head;
    MageBeamCarrierAimPatch.Prefix(aimingRoot.GetComponent<ValuableWizardStaff>()!);
    Near(aimingRoot.transform.position + aimingRoot.transform.rotation * laserOffset, head + direction * 3,
        "Laser origin follows the caster while compensating the staff's local muzzle offset");
    Near((aimingRoot.transform.rotation * laserRotation) * Vector3.forward, direction,
        "Native staff raycast receives this frame's view direction before Update");
    Near(aimingRoot.GetComponent<Rigidbody>()!.position, aimingRoot.transform.position, "Rigidbody and displayed carrier agree");
    aimingMarker.Maintain();
    Near((aimingRoot.transform.rotation * laserRotation) * Vector3.forward, direction, "Physics maintenance preserves the new aim");
}
var lastPosition = aimingRoot.transform.position;
var lastRotation = aimingRoot.transform.rotation;
caster.Living = false;
caster.localCamera.Aim.rotation = Quaternion.identity;
caster.playerAvatarVisuals.headLookAtTransform!.position = new(200, 200, 200);
aimingMarker.Maintain();
Check(aimingRoot.transform.position == lastPosition && aimingRoot.transform.rotation == lastRotation,
    "Dead caster leaves the last pose stable until scheduled cleanup");
caster.Living = true;
SemiFunc.Authority = false;
aimingRoot.transform.position = new(50, 60, 70);
MageBeamCarrierAimPatch.Prefix(aimingRoot.GetComponent<ValuableWizardStaff>()!);
Check(aimingRoot.transform.position == new Vector3(50, 60, 70), "Even a bound caster cannot overwrite a guest's network pose");
SemiFunc.Authority = true;
caster.localCamera.Throw = true;
Near(MageSpellAim.GetDirection(caster), new(-1, 0, 0), "Unavailable camera falls back to this caster's body");
caster.localCamera = null;
caster.playerTransform = null;
caster.playerAvatarVisuals = null;
caster.transform.position = new(4, 5, 6);
Near(MageSpellAim.GetOrigin(caster, Vector3.forward, 3), new(4, 6, 9), "Missing visuals retain an origin above the caster");
Near(MageSpellAim.GetDirection(caster), new(-1, 0, 0), "Missing camera and player transform fall back to the avatar");
Console.WriteLine($"PASS: {checks} Mage beam carrier checks using production code with simulated Unity objects.");
