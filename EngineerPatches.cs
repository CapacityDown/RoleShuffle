using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace REPOJP.StageRoles;

[HarmonyPatch]
internal static class EngineerTrapUpdatePatches
{
    private static readonly string[] FrameMethodNames =
    {
        "Update",
        "FixedUpdate",
        "LateUpdate"
    };

    private static IEnumerable<MethodBase> TargetMethods()
    {
        HashSet<MethodBase> targets = new();
        foreach (Type type in typeof(Trap).Assembly.GetTypes())
        {
            if (!typeof(Trap).IsAssignableFrom(type))
            {
                continue;
            }
            AddFrameMethods(type, targets);
        }

        foreach (string typeName in EngineerEffectCatalog.FrameEffectTypeNames)
        {
            Type? type = AccessTools.TypeByName(typeName);
            if (type != null)
            {
                AddFrameMethods(type, targets);
            }
        }

        foreach (MethodBase target in targets)
        {
            yield return target;
        }
    }

    private static void AddFrameMethods(
        Type type,
        HashSet<MethodBase> targets)
    {
        foreach (string methodName in FrameMethodNames)
        {
            MethodInfo? method = EngineerEffectCatalog.FindDeclaredInstanceMethod(
                type,
                methodName,
                Type.EmptyTypes);
            if (method != null && !method.IsStatic &&
                method.ReturnType == typeof(void))
            {
                targets.Add(method);
            }
        }
    }

    [HarmonyPrefix]
    private static bool EffectFramePrefix(Component __instance) =>
        StageRolesPlugin.Instance?.Controller?
            .ShouldSuppressEngineerEffect(__instance) != true;
}

[HarmonyPatch]
internal static class EngineerEffectActivationPatches
{
    private static readonly (string TypeName, string MethodName)[] Targets =
    {
        ("FireExtinguisherValuable", "GrabTrigger"),
        ("FlamethrowerValuable", "GrabTrigger"),
        ("MusicalValuableLogic", "MusicKeyPressed"),
        ("PhoneValuable", "PickUp"),
        ("PowerCrystalValuable", "Explode"),
        ("ValuableCamera", "Explosion"),
        ("ValuableCauldronBox", "TrapActivate"),
        ("ValuableLevitationPotion", "ActivateSphere"),
        ("ValuableStarWand", "CastSpell"),
        ("ValuableWizardStaff", "StaffLaser"),
        ("ValuableShootingCowboy", "Shoot"),
        ("ValuableShootingCowboy", "FireBullet")
    };

    private static IEnumerable<MethodBase> TargetMethods()
    {
        HashSet<MethodBase> methods = new();
        foreach (Type type in typeof(Trap).Assembly.GetTypes())
        {
            if (!typeof(Trap).IsAssignableFrom(type))
            {
                continue;
            }
            MethodInfo? trapActivate = EngineerEffectCatalog.FindDeclaredInstanceMethod(
                type,
                "TrapActivate",
                Type.EmptyTypes);
            if (trapActivate != null && !trapActivate.IsStatic)
            {
                methods.Add(trapActivate);
            }
        }

        foreach ((string typeName, string methodName) in Targets)
        {
            Type? type = AccessTools.TypeByName(typeName);
            MethodInfo? method = type != null
                ? EngineerEffectCatalog.FindDeclaredInstanceMethod(type, methodName)
                : null;
            if (method != null && !method.IsStatic)
            {
                methods.Add(method);
            }
        }

        foreach (MethodBase method in methods)
        {
            yield return method;
        }
    }

    [HarmonyPrefix]
    private static bool EffectActivationPrefix(Component __instance) =>
        StageRolesPlugin.Instance?.Controller?
            .ShouldSuppressEngineerEffect(__instance) != true;
}

internal static class EngineerEffectCatalog
{
    private static readonly HashSet<string> UnsupportedHostOnlyTypeNames = new(
        new[]
        {
            "ClownTrap",
            "PropaneTankTrap",
            "ValuableArcticSnowBike",
            "ValuableCamera",
            "ValuableFlashlight",
            "ValuableLovePotion"
        },
        StringComparer.Ordinal);

    internal static readonly string[] FrameEffectTypeNames =
    {
        "CrystalBallValuable",
        "FireExtinguisherValuable",
        "FlamethrowerValuable",
        "JackhammerValuable",
        "MapValuable",
        "MusicalValuableLogic",
        "PhoneValuable",
        "PowerCrystalValuable",
        "ScreamDollValuable",
        "UraniumScript",
        "ValuableArcticSnowBike",
        "ValuableCamera",
        "ValuableCauldronBox",
        "ValuableEyeOfOrpigox",
        "ValuableFlashlight",
        "ValuableLevitationPotion",
        "ValuableLovePotion",
        "ValuablePills",
        "ValuableSawBlade",
        "ValuableShootingCowboy",
        "ValuableScale",
        "ValuableSpiderPotion",
        "ValuableStarWand",
        "ValuableTeethBot",
        "ValuableWizardStaff",
        "ValuableWizardTimeGlass"
    };

    private static readonly HashSet<string> EffectTypeNames =
        new(FrameEffectTypeNames, StringComparer.Ordinal);

    internal static bool IsEffectValuable(PhysGrabObject valuable)
    {
        Component[] components = valuable.GetComponentsInChildren<Component>(true);
        foreach (Component component in components)
        {
            if (component != null &&
                UnsupportedHostOnlyTypeNames.Contains(component.GetType().Name))
            {
                return false;
            }
        }

        if (valuable.GetComponent<Trap>() != null ||
            valuable.GetComponentInChildren<Trap>(true) != null)
        {
            return true;
        }

        foreach (Component component in components)
        {
            if (component != null &&
                EffectTypeNames.Contains(component.GetType().Name))
            {
                return true;
            }
        }
        return false;
    }

    internal static MethodInfo? FindDeclaredInstanceMethod(
        Type type,
        string methodName,
        Type[]? parameterTypes = null)
    {
        const BindingFlags flags = BindingFlags.Instance |
                                   BindingFlags.Public |
                                   BindingFlags.NonPublic |
                                   BindingFlags.DeclaredOnly;
        if (parameterTypes != null)
        {
            return type.GetMethod(
                methodName,
                flags,
                binder: null,
                types: parameterTypes,
                modifiers: null);
        }

        foreach (MethodInfo method in type.GetMethods(flags))
        {
            if (string.Equals(method.Name, methodName, StringComparison.Ordinal))
            {
                return method;
            }
        }
        return null;
    }
}
