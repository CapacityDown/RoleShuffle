using System;
using System.Collections.Generic;
using HarmonyLib;
using Photon.Pun;
using UnityEngine;

namespace REPOJP.StageRoles;

// Only the short-lived staff spawned by Mage receives this marker. Ordinary
// valuables retain their mesh, gravity, grabbing and native firing recoil.
[DefaultExecutionOrder(10000)]
internal sealed class MageBeamCarrier : MonoBehaviour
{
    internal const string SpawnMarker = "RoleShuffle.MageBeam.v1";
    internal float BeamDurationSeconds { get; set; } = 7f;
    private const float OverrideSeconds = 1f;
    private bool _initialized;
    private Vector3 _position;
    private Quaternion _rotation;
    private Rigidbody? _body;
    private PhysGrabObject? _physics;
    private readonly List<Renderer> _hiddenRenderers = new();
    private readonly List<Light> _hiddenLights = new();
    private readonly List<Collider> _disabledColliders = new();

    internal static bool HasSpawnMarker(object[]? data) =>
        data is { Length: 1 } && data[0] is string marker && marker == SpawnMarker;

    internal static MageBeamCarrier Attach(GameObject carrier)
    {
        MageBeamCarrier marker = carrier.GetComponent<MageBeamCarrier>() ??
            carrier.AddComponent<MageBeamCarrier>();
        marker.Initialize();
        return marker;
    }

    private void Initialize()
    {
        if (_initialized) return;
        _position = transform.position;
        _rotation = transform.rotation;
        _body = GetComponent<Rigidbody>();
        _physics = GetComponent<PhysGrabObject>();
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
            if (renderer.GetComponentInParent<SemiLaser>(true) == null)
                _hiddenRenderers.Add(renderer);
        foreach (Light light in GetComponentsInChildren<Light>(true))
            if (light.GetComponentInParent<SemiLaser>(true) == null)
                _hiddenLights.Add(light);
        foreach (Collider collider in GetComponentsInChildren<Collider>(true))
            if (collider.GetComponentInParent<SemiLaser>(true) == null &&
                collider.GetComponentInParent<HurtCollider>(true) == null)
                _disabledColliders.Add(collider);
        _initialized = true;
        Maintain();
    }

    private void FixedUpdate() => Maintain();
    private void LateUpdate() => Maintain();

    internal void Maintain()
    {
        if (!_initialized) return;
        // Keep the laser's renderers, lights, particles and hurt collider live.
        foreach (Renderer renderer in _hiddenRenderers)
            if (renderer != null) renderer.enabled = false;
        foreach (Light light in _hiddenLights)
            if (light != null) light.enabled = false;
        foreach (Collider collider in _disabledColliders)
            if (collider != null) collider.enabled = false;

        if (_body != null)
        {
            if (!_body.isKinematic)
            {
                _body.velocity = Vector3.zero;
                _body.angularVelocity = Vector3.zero;
            }
            _body.useGravity = false;
            _body.constraints = RigidbodyConstraints.FreezeAll;
            _body.isKinematic = true;
            _body.position = _position;
            _body.rotation = _rotation;
        }
        if (_physics != null)
        {
            // PhysGrabObject resets direct rigidbody changes as its overrides
            // expire. Refresh its native overrides for the carrier's lifetime.
            // Native PhotonTransformView publishes the host's kinematic pose
            // to unmodded guests; mesh visibility itself is local-only.
            _physics.OverrideKinematic(OverrideSeconds);
            _physics.OverrideZeroGravity(OverrideSeconds);
            _physics.OverrideGrabDisable(OverrideSeconds);
        }
        transform.SetPositionAndRotation(_position, _rotation);
    }
}

[HarmonyPatch(typeof(ValuableWizardStaff), "Start")]
internal static class MageBeamCarrierStartPatch
{
    [HarmonyPostfix]
    internal static void Postfix(ValuableWizardStaff __instance)
    {
        try
        {
            PhotonView? view = __instance.GetComponent<PhotonView>();
            if (MageBeamCarrier.HasSpawnMarker(view?.InstantiationData))
                MageBeamCarrier.Attach(__instance.gameObject);
        }
        catch (Exception exception)
        {
            StageRolesPlugin.ModLogger.LogWarning($"Mage beam carrier setup failed: {exception.Message}");
        }
    }
}

[HarmonyPatch(typeof(ValuableWizardStaff), "FixedUpdate")]
internal static class MageBeamCarrierRecoilPatch
{
    [HarmonyPrefix]
    internal static bool Prefix(ValuableWizardStaff __instance) =>
        __instance.GetComponent<MageBeamCarrier>() == null;
}
