using Photon.Pun;
using REPOJP.StageRoles;
using UnityEngine;

int checks = 0;
void Check(bool condition, string message) { if (!condition) throw new Exception(message); checks++; }
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
}

foreach (bool remote in new[] { false, true })
{
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
    Check(!MageBeamCarrierRecoilPatch.Prefix(root.GetComponent<ValuableWizardStaff>()!), "Only generated carrier skips native beam recoil");
    // Simulate native initialization/timer expiry, grab/impact displacement,
    // and visual refreshes throughout and beyond the 4.25-second lifetime.
    for (int frame = 0; frame < 300; frame++)
    {
        body.isKinematic = false; body.useGravity = true;
        body.velocity = new(1, -8, 3); body.angularVelocity = new(0, 4, 0);
        body.constraints = RigidbodyConstraints.None;
        phys.KinematicSeconds = phys.GravitySeconds = phys.GrabDisableSeconds = 0;
        root.transform.position = body.position = new(1, -10, 9);
        root.transform.rotation = body.rotation = new(1, 0, 0, 0);
        staffMesh.enabled = staffParticles.enabled = staffLight.enabled = staffCollider.enabled = true;
        marker!.Maintain();
        Check(!staffMesh.enabled && !staffParticles.enabled && !staffLight.enabled && !staffCollider.enabled, "Carrier remains invisible and ungrabbable");
        Check(beamRenderer.enabled && beamHitRenderer.enabled && beamLight.enabled && beamCollider.enabled, "Beam rendering, light and damage stay active");
        Check(body.isKinematic && !body.useGravity && body.constraints == RigidbodyConstraints.FreezeAll && body.velocity == Vector3.zero && body.angularVelocity == Vector3.zero, "Gravity and momentum cannot move carrier");
        Check(body.position == expectedPosition && root.transform.position == expectedPosition && body.rotation == expectedRotation && root.transform.rotation == expectedRotation, "Original firing origin and direction stay fixed");
        Check(phys.KinematicSeconds > 0 && phys.GravitySeconds > 0 && phys.GrabDisableSeconds > 0, "Native overrides are continuously refreshed");
    }
    MageBeamCarrier.Attach(root);
    Check(root.Components.OfType<MageBeamCarrier>().Count() == 1, "Native Start does not duplicate the marker or recapture a moved pose");
}
Console.WriteLine($"PASS: {checks} Mage beam carrier checks using production code with simulated Unity objects.");
