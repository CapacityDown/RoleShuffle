using System;
using System.Collections.Generic;
using UnityEngine;

namespace REPOJP.StageRoles;

internal readonly struct ResolvedRolePrefab
{
    internal ResolvedRolePrefab(
        GameObject prefab,
        string resourcePath,
        VanillaGrenadeKind grenadeKind = VanillaGrenadeKind.None)
    {
        Prefab = prefab;
        ResourcePath = resourcePath;
        GrenadeKind = grenadeKind;
    }

    internal GameObject Prefab { get; }
    internal string ResourcePath { get; }
    internal VanillaGrenadeKind GrenadeKind { get; }
}

internal enum VanillaGrenadeKind
{
    None,
    Explosive,
    Stun,
    Shockwave,
    DuctTaped
}

internal sealed class VanillaRolePrefabResolver
{
    private const int MaximumResolveAttempts = 5;
    private readonly List<ResolvedRolePrefab> _grenades = new();
    private ResolvedRolePrefab _starProjectile;
    private ResolvedRolePrefab _rollProjectile;
    private ResolvedRolePrefab _zeroGravityProjectile;
    private ResolvedRolePrefab _voidProjectile;
    private PrefabRef? _voidImpactPrefab;
    private ResolvedRolePrefab _wizardStaff;
    private ResolvedRolePrefab _screamDoll;
    private readonly List<ResolvedRolePrefab> _uraniumValuables = new();
    private bool _grenadesResolved;
    private int _starProjectileResolveAttempts;
    private bool _starProjectileResolveFailed;
    private int _rollProjectileResolveAttempts;
    private bool _rollProjectileResolveFailed;
    private int _zeroGravityProjectileResolveAttempts;
    private bool _zeroGravityProjectileResolveFailed;
    private int _voidProjectileResolveAttempts;
    private bool _voidProjectileResolveFailed;
    private int _wizardStaffResolveAttempts;
    private bool _wizardStaffResolveFailed;
    private int _screamDollResolveAttempts;
    private bool _screamDollResolveFailed;
    private int _uraniumResolveAttempts;
    private bool _uraniumResolveFailed;

    internal bool TryGetRandomUraniumValuable(out ResolvedRolePrefab resolved)
    {
        if (_uraniumValuables.Count == 0 && !_uraniumResolveFailed)
        {
            ResolveUraniumValuables();
        }
        if (_uraniumValuables.Count > 0)
        {
            resolved = _uraniumValuables[
                UnityEngine.Random.Range(0, _uraniumValuables.Count)];
            return true;
        }

        _uraniumResolveAttempts++;
        if (_uraniumResolveAttempts >= MaximumResolveAttempts)
        {
            _uraniumResolveFailed = true;
            StageRolesPlugin.ModLogger.LogWarning(
                "Vanilla uranium valuable could not be resolved; Stinker is unavailable.");
        }
        resolved = default;
        return false;
    }

    internal bool TryGetScreamDoll(out ResolvedRolePrefab resolved)
    {
        if (IsResolved(_screamDoll))
        {
            resolved = _screamDoll;
            return true;
        }
        if (_screamDollResolveFailed)
        {
            resolved = default;
            return false;
        }

        foreach (LevelValuables preset in Resources.FindObjectsOfTypeAll<LevelValuables>())
        {
            foreach (PrefabRef prefabRef in EnumerateValuableRefs(preset))
            {
                if (!TryGetVanillaPrefab(prefabRef, out ResolvedRolePrefab candidate) ||
                    candidate.Prefab.GetComponentInChildren<ScreamDollValuable>(true) == null ||
                    candidate.Prefab.GetComponentInChildren<PhysGrabObject>(true) == null)
                {
                    continue;
                }
                _screamDoll = candidate;
                StageRolesPlugin.ModLogger.LogInfo(
                    $"Resolved vanilla Trickster decoy prefab: {candidate.ResourcePath}");
                resolved = candidate;
                return true;
            }
        }

        _screamDollResolveAttempts++;
        if (_screamDollResolveAttempts >= MaximumResolveAttempts)
        {
            _screamDollResolveFailed = true;
            StageRolesPlugin.ModLogger.LogWarning(
                "Vanilla Scream Doll prefab could not be resolved; Trickster is unavailable.");
        }
        resolved = default;
        return false;
    }

    internal bool TryGetRandomGrenade(
        StageRolesConfig config,
        out ResolvedRolePrefab resolved)
    {
        ResolveGrenades();
        List<ResolvedRolePrefab> enabled = new();
        foreach (ResolvedRolePrefab grenade in _grenades)
        {
            bool include = grenade.GrenadeKind switch
            {
                VanillaGrenadeKind.Explosive => config.BomberExplosiveEnabled.Value,
                VanillaGrenadeKind.Stun => config.BomberStunEnabled.Value,
                VanillaGrenadeKind.Shockwave => config.BomberShockwaveEnabled.Value,
                VanillaGrenadeKind.DuctTaped => config.BomberDuctTapedEnabled.Value,
                _ => false
            };
            if (include)
            {
                enabled.Add(grenade);
            }
        }
        if (enabled.Count == 0)
        {
            resolved = default;
            return false;
        }

        resolved = enabled[UnityEngine.Random.Range(0, enabled.Count)];
        return true;
    }

    internal bool TryGetStarProjectile(out ResolvedRolePrefab resolved)
    {
        if (IsResolved(_starProjectile))
        {
            resolved = _starProjectile;
            return true;
        }
        if (_starProjectileResolveFailed)
        {
            resolved = default;
            return false;
        }

        foreach (LevelValuables preset in Resources.FindObjectsOfTypeAll<LevelValuables>())
        {
            foreach (PrefabRef prefabRef in EnumerateValuableRefs(preset))
            {
                if (!TryGetVanillaPrefab(prefabRef, out ResolvedRolePrefab valuable))
                {
                    continue;
                }
                ValuableStarWand? wand =
                    valuable.Prefab.GetComponentInChildren<ValuableStarWand>(true);
                if (wand == null ||
                    !TryGetVanillaPrefab(wand.bulletPrefab, out ResolvedRolePrefab projectile) ||
                    projectile.Prefab.GetComponentInChildren<SlowProjectile>(true) == null)
                {
                    continue;
                }
                _starProjectile = projectile;
                StageRolesPlugin.ModLogger.LogInfo(
                    $"Resolved vanilla Star Wand projectile prefab: {projectile.ResourcePath}");
                break;
            }
            if (IsResolved(_starProjectile))
            {
                break;
            }
        }

        resolved = _starProjectile;
        if (IsResolved(resolved))
        {
            return true;
        }
        if (++_starProjectileResolveAttempts >= MaximumResolveAttempts)
        {
            _starProjectileResolveFailed = true;
            StageRolesPlugin.ModLogger.LogWarning(
                "Vanilla Star Wand projectile could not be resolved; Mage star casts are unavailable.");
        }
        return false;
    }

    internal bool TryGetWizardStaff(out ResolvedRolePrefab resolved)
    {
        if (IsResolved(_wizardStaff))
        {
            resolved = _wizardStaff;
            return true;
        }
        if (_wizardStaffResolveFailed)
        {
            resolved = default;
            return false;
        }

        foreach (LevelValuables preset in Resources.FindObjectsOfTypeAll<LevelValuables>())
        {
            foreach (PrefabRef prefabRef in EnumerateValuableRefs(preset))
            {
                if (!TryGetVanillaPrefab(prefabRef, out ResolvedRolePrefab candidate))
                {
                    continue;
                }
                ValuableWizardStaff? staff =
                    candidate.Prefab.GetComponentInChildren<ValuableWizardStaff>(true);
                if (staff?.semiLaser == null || staff.laserTransform == null)
                {
                    continue;
                }
                _wizardStaff = candidate;
                StageRolesPlugin.ModLogger.LogInfo(
                    $"Resolved vanilla Wizard Staff prefab: {candidate.ResourcePath}");
                break;
            }
            if (IsResolved(_wizardStaff))
            {
                break;
            }
        }

        resolved = _wizardStaff;
        if (IsResolved(resolved))
        {
            return true;
        }
        if (++_wizardStaffResolveAttempts >= MaximumResolveAttempts)
        {
            _wizardStaffResolveFailed = true;
            StageRolesPlugin.ModLogger.LogWarning(
                "Vanilla Wizard Staff could not be resolved; Mage beam casts are unavailable.");
        }
        return false;
    }

    internal bool TryGetRollProjectile(out ResolvedRolePrefab resolved) =>
        TryGetStaffProjectile(
            typeof(ItemStaffTorque),
            "Roll Staff",
            ref _rollProjectile,
            ref _rollProjectileResolveAttempts,
            ref _rollProjectileResolveFailed,
            out resolved);

    internal bool TryGetZeroGravityProjectile(out ResolvedRolePrefab resolved) =>
        TryGetStaffProjectile(
            typeof(ItemStaffZeroGravity),
            "Zero Gravity Staff",
            ref _zeroGravityProjectile,
            ref _zeroGravityProjectileResolveAttempts,
            ref _zeroGravityProjectileResolveFailed,
            out resolved);

    internal bool TryGetVoidProjectile(out ResolvedRolePrefab resolved) =>
        TryGetStaffProjectile(
            typeof(ItemStaffVoid),
            "Void Staff",
            ref _voidProjectile,
            ref _voidProjectileResolveAttempts,
            ref _voidProjectileResolveFailed,
            out resolved);

    internal bool TryGetVoidImpactPrefab(out PrefabRef impactPrefab)
    {
        TryGetVoidProjectile(out _);
        if (_voidImpactPrefab != null &&
            _voidImpactPrefab.IsValid() &&
            !string.IsNullOrEmpty(_voidImpactPrefab.ResourcePath))
        {
            impactPrefab = _voidImpactPrefab;
            return true;
        }
        impactPrefab = null!;
        return false;
    }

    private bool TryGetStaffProjectile(
        Type staffType,
        string displayName,
        ref ResolvedRolePrefab cached,
        ref int resolveAttempts,
        ref bool resolveFailed,
        out ResolvedRolePrefab resolved)
    {
        if (IsResolved(cached))
        {
            resolved = cached;
            return true;
        }
        if (resolveFailed)
        {
            resolved = default;
            return false;
        }

        foreach (Item item in EnumerateItems())
        {
            if (!TryGetVanillaPrefab(item, out ResolvedRolePrefab staffItem))
            {
                continue;
            }
            Component? component = staffItem.Prefab.GetComponentInChildren(
                staffType,
                true);
            PrefabRef? projectileRef = component switch
            {
                ItemStaffTorque torque => torque.projectilePrefab,
                ItemStaffZeroGravity zeroGravity => zeroGravity.projectilePrefab,
                ItemStaffVoid voidProjectileStaff => voidProjectileStaff.projectilePrefab,
                _ => null
            };
            if (component is ItemStaffVoid voidStaff &&
                voidStaff.prefabToInstantiateOnHit != null &&
                voidStaff.prefabToInstantiateOnHit.IsValid())
            {
                _voidImpactPrefab = voidStaff.prefabToInstantiateOnHit;
            }
            if (projectileRef == null ||
                !TryGetVanillaPrefab(projectileRef, out ResolvedRolePrefab projectile) ||
                projectile.Prefab.GetComponentInChildren<SlowProjectile>(true) == null)
            {
                continue;
            }
            cached = projectile;
            StageRolesPlugin.ModLogger.LogInfo(
                $"Resolved vanilla {displayName} projectile prefab: {projectile.ResourcePath}");
            break;
        }

        resolved = cached;
        if (IsResolved(resolved))
        {
            return true;
        }
        if (++resolveAttempts >= MaximumResolveAttempts)
        {
            resolveFailed = true;
            StageRolesPlugin.ModLogger.LogWarning(
                $"Vanilla {displayName} projectile could not be resolved; " +
                $"Mage {displayName} casts are unavailable.");
        }
        return false;
    }

    private void ResolveGrenades()
    {
        if (_grenadesResolved)
        {
            return;
        }

        _grenadesResolved = true;
        (Type Type, VanillaGrenadeKind Kind)[] requiredTypes =
        {
            (typeof(ItemGrenadeExplosive), VanillaGrenadeKind.Explosive),
            (typeof(ItemGrenadeStun), VanillaGrenadeKind.Stun),
            (typeof(ItemGrenadeShockwave), VanillaGrenadeKind.Shockwave),
            (typeof(ItemGrenadeDuctTaped), VanillaGrenadeKind.DuctTaped)
        };
        HashSet<string> paths = new(StringComparer.Ordinal);
        foreach ((Type type, VanillaGrenadeKind kind) in requiredTypes)
        {
            foreach (Item item in EnumerateItems())
            {
                if (!TryGetVanillaPrefab(item, out ResolvedRolePrefab candidate) ||
                    candidate.Prefab.GetComponentInChildren(type, true) == null ||
                    candidate.Prefab.GetComponentInChildren<ItemGrenade>(true) == null ||
                    candidate.Prefab.GetComponentInChildren<ItemToggle>(true) == null ||
                    !paths.Add(candidate.ResourcePath))
                {
                    continue;
                }

                _grenades.Add(new ResolvedRolePrefab(
                    candidate.Prefab,
                    candidate.ResourcePath,
                    kind));
                break;
            }
        }

        StageRolesPlugin.ModLogger.LogInfo(
            $"Resolved {_grenades.Count}/4 vanilla Bomber grenade prefabs.");
    }

    private void ResolveUraniumValuables()
    {
        HashSet<string> paths = new(StringComparer.Ordinal);
        foreach (LevelValuables preset in Resources.FindObjectsOfTypeAll<LevelValuables>())
        {
            foreach (PrefabRef prefabRef in EnumerateValuableRefs(preset))
            {
                if (!TryGetVanillaPrefab(prefabRef, out ResolvedRolePrefab candidate) ||
                    !paths.Add(candidate.ResourcePath) ||
                    candidate.Prefab.GetComponentInChildren<UraniumScript>(true) == null ||
                    candidate.Prefab.GetComponentInChildren<ValuableObject>(true) == null ||
                    candidate.Prefab.GetComponentInChildren<PhysGrabObjectImpactDetector>(true) == null)
                {
                    continue;
                }
                _uraniumValuables.Add(candidate);
            }
        }
        if (_uraniumValuables.Count > 0)
        {
            StageRolesPlugin.ModLogger.LogInfo(
                $"Resolved {_uraniumValuables.Count} vanilla uranium valuable prefab(s).");
        }
    }

    private static IEnumerable<Item> EnumerateItems()
    {
        HashSet<Item> seen = new();
        if (StatsManager.instance != null)
        {
            foreach (Item item in StatsManager.instance.itemDictionary.Values)
            {
                if (item != null && seen.Add(item))
                {
                    yield return item;
                }
            }
        }

        foreach (Item item in Resources.FindObjectsOfTypeAll<Item>())
        {
            if (item != null && seen.Add(item))
            {
                yield return item;
            }
        }
    }

    private static IEnumerable<PrefabRef> EnumerateValuableRefs(LevelValuables preset)
    {
        List<PrefabRef>[] groups =
        {
            preset.tiny,
            preset.small,
            preset.medium,
            preset.big,
            preset.wide,
            preset.tall,
            preset.veryTall
        };
        foreach (List<PrefabRef> group in groups)
        {
            if (group == null)
            {
                continue;
            }
            foreach (PrefabRef prefabRef in group)
            {
                if (prefabRef != null)
                {
                    yield return prefabRef;
                }
            }
        }
    }

    private static bool TryGetVanillaPrefab(Item item, out ResolvedRolePrefab resolved)
    {
        try
        {
            PrefabRef prefabRef = item.prefab;
            if (prefabRef == null || prefabRef.Bundle != null || !prefabRef.IsValid() ||
                string.IsNullOrEmpty(prefabRef.ResourcePath) || prefabRef.Prefab == null)
            {
                resolved = default;
                return false;
            }

            resolved = new ResolvedRolePrefab(prefabRef.Prefab, prefabRef.ResourcePath);
            return true;
        }
        catch
        {
            resolved = default;
            return false;
        }
    }

    private static bool TryGetVanillaPrefab(
        PrefabRef prefabRef,
        out ResolvedRolePrefab resolved)
    {
        try
        {
            if (prefabRef == null || prefabRef.Bundle != null || !prefabRef.IsValid() ||
                string.IsNullOrEmpty(prefabRef.ResourcePath) || prefabRef.Prefab == null)
            {
                resolved = default;
                return false;
            }

            resolved = new ResolvedRolePrefab(prefabRef.Prefab, prefabRef.ResourcePath);
            return true;
        }
        catch
        {
            resolved = default;
            return false;
        }
    }

    private static bool IsResolved(ResolvedRolePrefab resolved) =>
        resolved.Prefab != null && !string.IsNullOrEmpty(resolved.ResourcePath);
}
