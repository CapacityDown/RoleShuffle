using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace REPOJP.StageRoles;

internal static class EngineerCowboySuppression
{
    // Update also expires the bullet hitbox and fades loops. Skipping it alone
    // would leave a previously fired bullet live while the Engineer holds it.
    private static readonly FieldInfo[] ZeroFloatFields = Fields(
        "armRotationTimer", "reloadTimer", "shootTimer", "afterShootTimer",
        "coolDownTimer", "hurtColliderTimer", "lineRendererLerp", "animPhase", "animWeight");
    private static readonly FieldInfo[] FalseBoolFields = Fields(
        "grabbed", "shot", "reloadSound", "shakeSound", "lineRendererActive");
    private static readonly FieldInfo CurrentArmAngle = Field("currentArmAngle");
    private static readonly FieldInfo ArmReturnTimer = Field("armReturnTimer");
    private static readonly FieldInfo ArmReturnTime = Field("armReturnTime");

    internal static void Reset(ValuableShootingCowboy cowboy)
    {
        foreach (FieldInfo field in ZeroFloatFields) field.SetValue(cowboy, 0f);
        foreach (FieldInfo field in FalseBoolFields) field.SetValue(cowboy, false);
        // This is the native resting pose. On release/role loss, a new grab
        // starts the ordinary full wind-up instead of resuming a loaded shot.
        CurrentArmAngle.SetValue(cowboy, 100f);
        ArmReturnTimer.SetValue(cowboy, ArmReturnTime.GetValue(cowboy));
        if (cowboy.arm != null) cowboy.arm.localRotation = Quaternion.Euler(100f, 0f, 0f);
        Rest(cowboy.armShakeTransform);
        Rest(cowboy.cowboyNeck);
        Rest(cowboy.scarf1);
        Rest(cowboy.scarf2);
        if (cowboy.hurtCollider != null) cowboy.hurtCollider.gameObject.SetActive(false);
        if (cowboy.lineRenderer != null) cowboy.lineRenderer.gameObject.SetActive(false);
        foreach (ParticleSystem effect in cowboy.shootEffects)
            if (effect != null && effect.isPlaying)
                effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        cowboy.armRotationLoop?.PlayLoop(false, 1f, 1f);
        cowboy.mechanicWhirringLoop?.PlayLoop(false, 0.7f, 1f);
    }

    private static void Rest(Transform? transform)
    {
        if (transform != null) transform.localRotation = Quaternion.identity;
    }

    private static FieldInfo Field(string name) =>
        AccessTools.Field(typeof(ValuableShootingCowboy), name) ??
        throw new MissingFieldException(typeof(ValuableShootingCowboy).FullName, name);

    private static FieldInfo[] Fields(params string[] names) => Array.ConvertAll(names, Field);
}
