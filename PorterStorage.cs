using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace REPOJP.StageRoles;

internal static class PorterStorage
{
    // Vanilla maps discard altitude, so (0, 3000, 0) still marks the entrance.
    // Move the original network object outside the playable map in X/Z too.
    internal static readonly Vector3 ParkingPosition = new(10000, 3000, 10000);
    private static readonly FieldInfo? DeactivateTimer = AccessTools.Field(typeof(PhysGrabObject), "timerAlterDeactivate");
    private static readonly FieldInfo? Active = AccessTools.Field(typeof(PhysGrabObject), "isActive");

    internal static void Begin(PhysGrabObject physics)
    {
        CheckContract();
        // Preserve native entry effects, including releasing an attached drone.
        physics.OverrideDeactivate(1f);
        Maintain(physics);
    }

    internal static void Maintain(PhysGrabObject physics)
    {
        CheckContract();
        // Refresh the per-object native inactive state without OverrideDeactivate,
        // which would teleport back to the shared entrance X/Z every frame.
        // OverrideTimersTick also parks newly inactive objects there; setting the
        // state now prevents that first-tick move. Reset uses the native API.
        DeactivateTimer!.SetValue(physics, 1f);
        Active!.SetValue(physics, false);
        if (physics.rb != null)
        {
            physics.rb.isKinematic = true;
            physics.rb.detectCollisions = false;
        }
        if ((physics.transform.position - ParkingPosition).sqrMagnitude > 0.0001f)
            physics.Teleport(ParkingPosition, Quaternion.identity);
    }

    private static void CheckContract()
    {
        if (DeactivateTimer?.FieldType != typeof(float) || Active?.FieldType != typeof(bool))
            throw new NotSupportedException("Porter storage requires compatible native deactivation fields.");
    }
}
