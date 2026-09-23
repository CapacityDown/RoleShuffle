using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

namespace REPOJP.StageRoles;

internal sealed class MageRoleRuntime
{
    private sealed class RecoveryState
    {
        internal int LastObservedHealth = -1;
        internal float LastDamageAt;
        internal float NextHealAt;
    }

    private const float BeamCarrierLifetimeSeconds = 4.25f;
    private readonly MonoBehaviour _coroutineOwner;
    private readonly StageRolesConfig _config;
    private readonly VanillaRolePrefabResolver _resolver;
    private readonly List<GameObject> _spawnedObjects = new();
    private readonly Dictionary<PlayerAvatar, RecoveryState> _recoveryStates = new();
    private int _generation;

    internal MageRoleRuntime(
        MonoBehaviour coroutineOwner,
        StageRolesConfig config,
        VanillaRolePrefabResolver resolver)
    {
        _coroutineOwner = coroutineOwner;
        _config = config;
        _resolver = resolver;
    }

    internal void Begin(IReadOnlyList<RoleAssignment> assignments)
    {
        Stop();
        foreach (RoleAssignment assignment in assignments)
        {
            assignment.MageNextCastAt = 0f;
            if (RoleCatalog.HasCapability(assignment.Role, StageRole.Mage))
            {
                InitializeRecovery(assignment.Player);
            }
        }
        _resolver.TryGetStarProjectile(out _);
        _resolver.TryGetRollProjectile(out _);
        _resolver.TryGetZeroGravityProjectile(out _);
        _resolver.TryGetVoidProjectile(out _);
        _resolver.TryGetWizardStaff(out _);
    }

    internal void AddPlayer(RoleAssignment assignment)
    {
        assignment.MageNextCastAt = 0f;
        if (RoleCatalog.HasCapability(assignment.Role, StageRole.Mage))
        {
            InitializeRecovery(assignment.Player);
        }
    }

    internal void MaintainSpawnedObjects()
    {
        _spawnedObjects.RemoveAll(instance => instance == null);
    }

    internal void Tick(RoleAssignment assignment)
    {
        if (!PlayerState.IsLiving(assignment.Player))
        {
            assignment.MageNextCastAt = 0f;
            ResetRecoveryDelay(assignment.Player);
            return;
        }

        TickRecovery(assignment);
    }

    internal void ObserveHealthUpdate(
        PlayerAvatar player,
        int previousHealth,
        int currentHealth)
    {
        if (currentHealth >= previousHealth ||
            !_recoveryStates.TryGetValue(player, out RecoveryState? state))
        {
            return;
        }

        float now = Time.time;
        state.LastObservedHealth = currentHealth;
        state.LastDamageAt = now;
        state.NextHealAt = now + ClampedRecoveryDelay;
    }

    internal bool TryCast(RoleAssignment assignment, string spell)
    {
        if (!PlayerState.IsLiving(assignment.Player) ||
            Time.time < assignment.MageNextCastAt)
        {
            return false;
        }

        int healthCost = HealthCost(spell);
        if (healthCost > 0 &&
            (!PlayerState.TryGetCurrentHealth(assignment.Player, out int currentHealth) ||
             currentHealth <= healthCost))
        {
            StageRolesPlugin.ModLogger.LogDebug(
                $"Mage {spell} cast was rejected because its {healthCost} health cost would be fatal.");
            return false;
        }

        Vector3 direction = GetForwardDirection(assignment.Player);
        bool cast = spell switch
        {
            "star" => TryCastProjectile(
                assignment.Player,
                direction,
                "Star",
                _resolver.TryGetStarProjectile),
            "roll" => TryCastProjectile(
                assignment.Player,
                direction,
                "Roll",
                _resolver.TryGetRollProjectile),
            "gravity" => TryCastProjectile(
                assignment.Player,
                direction,
                "Gravity",
                _resolver.TryGetZeroGravityProjectile),
            "void" => TryCastProjectile(
                assignment.Player,
                direction,
                "Void",
                _resolver.TryGetVoidProjectile),
            "laser" => TryCastBeam(
                GetCastOrigin(assignment.Player, direction, 3f),
                direction),
            _ => false
        };
        if (cast)
        {
            ConsumeHealth(assignment.Player, healthCost, spell);
            assignment.MageNextCastAt = Time.time + ClampedInterval;
        }
        return cast;
    }

    internal void Stop()
    {
        _generation++;
        foreach (GameObject instance in _spawnedObjects)
        {
            DestroyNetworkObject(instance);
        }
        _spawnedObjects.Clear();
        _recoveryStates.Clear();
    }

    private float ClampedInterval =>
        Mathf.Clamp(_config.MageCastIntervalSeconds.Value, 0.1f, 30f);

    private float ClampedRecoveryDelay =>
        Mathf.Clamp(_config.MageAutoRecoveryDelaySeconds.Value, 0f, 300f);

    private float ClampedRecoveryInterval =>
        Mathf.Clamp(_config.MageAutoRecoveryIntervalSeconds.Value, 0.1f, 60f);

    private int ClampedRecoveryAmount =>
        Mathf.Clamp(_config.MageAutoRecoveryAmount.Value, 0, 100);

    private void InitializeRecovery(PlayerAvatar player)
    {
        float now = Time.time;
        RecoveryState state = new()
        {
            LastDamageAt = now,
            NextHealAt = now + ClampedRecoveryDelay
        };
        if (PlayerState.TryGetCurrentHealth(player, out int currentHealth))
        {
            state.LastObservedHealth = currentHealth;
        }
        _recoveryStates[player] = state;
    }

    private void ResetRecoveryDelay(PlayerAvatar player)
    {
        if (!_recoveryStates.TryGetValue(player, out RecoveryState? state))
        {
            InitializeRecovery(player);
            return;
        }

        float now = Time.time;
        state.LastDamageAt = now;
        state.NextHealAt = now + ClampedRecoveryDelay;
        state.LastObservedHealth = PlayerState.TryGetCurrentHealth(player, out int currentHealth)
            ? currentHealth
            : -1;
    }

    private void TickRecovery(RoleAssignment assignment)
    {
        PlayerAvatar player = assignment.Player;
        if (!_recoveryStates.TryGetValue(player, out RecoveryState? state))
        {
            InitializeRecovery(player);
            return;
        }
        if (!PlayerState.TryGetCurrentHealth(player, out int currentHealth) ||
            !PlayerState.TryGetMaximumHealth(player, out int maximumHealth))
        {
            return;
        }

        float now = Time.time;
        if (state.LastObservedHealth >= 0 && currentHealth < state.LastObservedHealth)
        {
            state.LastDamageAt = now;
            state.NextHealAt = now + ClampedRecoveryDelay;
        }
        state.LastObservedHealth = currentHealth;

        int recoveryAmount = ClampedRecoveryAmount;
        int recoveryRemaining = Math.Max(0,
            Mathf.Clamp(_config.MageAutoRecoveryTotalHealingLimit.Value, 1, 10000) -
            assignment.MageAutoRecoveryUsed);
        if (!_config.MageAutoRecoveryEnabled.Value ||
            recoveryAmount <= 0 ||
            recoveryRemaining <= 0 ||
            player.playerHealth == null ||
            currentHealth >= maximumHealth ||
            now < state.LastDamageAt + ClampedRecoveryDelay ||
            now < state.NextHealAt)
        {
            return;
        }

        int actualAmount = Math.Min(recoveryRemaining,
            Math.Min(recoveryAmount, maximumHealth - currentHealth));
        try
        {
            assignment.MageAutoRecoveryUsed += actualAmount;
            if (!RoleHealingRuntime.TryHeal(player, actualAmount, restored =>
                    assignment.MageAutoRecoveryUsed = Math.Max(0,
                        assignment.MageAutoRecoveryUsed - actualAmount + restored)))
            {
                assignment.MageAutoRecoveryUsed -= actualAmount;
            }
            // Keep the last observed value until the owner's health RPC is
            // reflected locally. Treating the requested value as observed can
            // make the replication delay look like new damage and restart the
            // full recovery delay after every tick.
            state.LastObservedHealth = currentHealth;
            state.NextHealAt = now + ClampedRecoveryInterval;
        }
        catch (Exception exception)
        {
            state.NextHealAt = now + ClampedRecoveryInterval;
            StageRolesPlugin.ModLogger.LogWarning(
                $"Mage automatic recovery failed: {exception.Message}");
        }
    }

    private int HealthCost(string spell) => spell switch
    {
        "star" => Mathf.Clamp(_config.MageStarHealthCost.Value, 0, 100),
        "gravity" => Mathf.Clamp(_config.MageGravityHealthCost.Value, 0, 100),
        "roll" => Mathf.Clamp(_config.MageRollHealthCost.Value, 0, 100),
        "void" => Mathf.Clamp(_config.MageVoidHealthCost.Value, 0, 100),
        "laser" => Mathf.Clamp(_config.MageLaserHealthCost.Value, 0, 100),
        _ => 0
    };

    private static void ConsumeHealth(
        PlayerAvatar player,
        int healthCost,
        string spell)
    {
        if (healthCost <= 0)
        {
            return;
        }
        try
        {
            player.playerHealth?.HurtOther(healthCost, Vector3.zero, false);
            StageRolesPlugin.ModLogger.LogDebug(
                $"Mage consumed {healthCost} health for the {spell} spell.");
        }
        catch (Exception exception)
        {
            StageRolesPlugin.ModLogger.LogWarning(
                $"Mage {spell} health cost failed: {exception.Message}");
        }
    }

    private delegate bool TryResolveProjectile(out ResolvedRolePrefab resolved);

    private bool TryCastProjectile(
        PlayerAvatar player,
        Vector3 direction,
        string spellName,
        TryResolveProjectile resolver)
    {
        if (!resolver(out ResolvedRolePrefab resolved))
        {
            return false;
        }
        try
        {
            GameObject projectile = SpawnNetworkObject(
                resolved,
                GetCastOrigin(player, direction, 0.8f),
                Quaternion.LookRotation(direction));
            projectile.name = $"RoleShuffle_Mage{spellName}";
            SetGeneratedValuablePriceZero(projectile);
            StageFluxCompatibility.MarkInternalCarrier(projectile);
            SlowProjectile? slowProjectile =
                projectile.GetComponent<SlowProjectile>() ??
                projectile.GetComponentInChildren<SlowProjectile>(true);
            if (slowProjectile == null)
            {
                DestroyNetworkObject(projectile);
                return false;
            }
            bool isVoid = string.Equals(
                spellName,
                "Void",
                StringComparison.Ordinal);
            if (isVoid &&
                !_resolver.TryGetVoidImpactPrefab(out _))
            {
                DestroyNetworkObject(projectile);
                StageRolesPlugin.ModLogger.LogWarning(
                    "Mage Void cast was cancelled because the vanilla Void impact prefab was unavailable.");
                return false;
            }
            if (isVoid)
            {
                _resolver.TryGetVoidImpactPrefab(out PrefabRef voidImpactPrefab);
                slowProjectile.prefabToInstantiateOnHit = voidImpactPrefab;
            }
            if (isVoid ||
                string.Equals(spellName, "Roll", StringComparison.Ordinal) ||
                string.Equals(spellName, "Gravity", StringComparison.Ordinal))
            {
                slowProjectile.investigateRadiusOnHit = 24f;
            }
            slowProjectile.SetSpawner(player.gameObject);
            slowProjectile.Launch(direction);
            _spawnedObjects.Add(projectile);
            StageRolesPlugin.ModLogger.LogDebug(
                $"Mage fired the {spellName} spell.");
            return true;
        }
        catch (Exception exception)
        {
            StageRolesPlugin.ModLogger.LogWarning(
                $"Mage {spellName} cast failed: {exception.Message}");
            return false;
        }
    }

    private bool TryCastBeam(Vector3 origin, Vector3 direction)
    {
        if (!_resolver.TryGetWizardStaff(out ResolvedRolePrefab resolved))
        {
            return false;
        }
        ValuableWizardStaff? templateStaff =
            resolved.Prefab.GetComponentInChildren<ValuableWizardStaff>(true);
        if (templateStaff?.laserTransform == null)
        {
            return false;
        }
        try
        {
            Transform templateRoot = resolved.Prefab.transform;
            Quaternion relativeLaserRotation =
                Quaternion.Inverse(templateRoot.rotation) *
                templateStaff.laserTransform.rotation;
            Quaternion rootRotation =
                Quaternion.LookRotation(direction) *
                Quaternion.Inverse(relativeLaserRotation);
            Vector3 relativeLaserPosition =
                templateRoot.InverseTransformPoint(templateStaff.laserTransform.position);
            Vector3 rootPosition = origin - rootRotation * relativeLaserPosition;

            GameObject carrier = SpawnNetworkObject(
                resolved,
                rootPosition,
                rootRotation,
                new object[] { MageBeamCarrier.SpawnMarker });
            carrier.name = "RoleShuffle_MageBeam";
            MageBeamCarrier.Attach(carrier);
            SetGeneratedValuablePriceZero(carrier);
            StageFluxCompatibility.MarkInternalCarrier(carrier);
            _spawnedObjects.Add(carrier);
            int generation = _generation;
            _coroutineOwner.StartCoroutine(ActivateBeam(carrier, generation));
            return true;
        }
        catch (Exception exception)
        {
            StageRolesPlugin.ModLogger.LogWarning(
                $"Mage beam cast failed: {exception.Message}");
            return false;
        }
    }

    private IEnumerator ActivateBeam(GameObject carrier, int generation)
    {
        yield return null;
        if (carrier == null || generation != _generation)
        {
            yield break;
        }
        MageBeamCarrier.Attach(carrier);
        ValuableWizardStaff? staff =
            carrier.GetComponent<ValuableWizardStaff>() ??
            carrier.GetComponentInChildren<ValuableWizardStaff>(true);
        if (staff == null)
        {
            _spawnedObjects.Remove(carrier);
            DestroyNetworkObject(carrier);
            yield break;
        }
        staff.StaffLaser();
        StageRolesPlugin.ModLogger.LogDebug("Mage fired a Wizard Staff beam.");
        yield return new WaitForSeconds(BeamCarrierLifetimeSeconds);
        _spawnedObjects.Remove(carrier);
        DestroyNetworkObject(carrier);
    }

    private static GameObject SpawnNetworkObject(
        ResolvedRolePrefab resolved,
        Vector3 position,
        Quaternion rotation,
        object[]? data = null) =>
        SemiFunc.IsMultiplayer()
            ? PhotonNetwork.Instantiate(resolved.ResourcePath, position, rotation, 0, data)
            : UnityEngine.Object.Instantiate(resolved.Prefab, position, rotation);

    private static void SetGeneratedValuablePriceZero(GameObject instance)
    {
        foreach (ValuableObject valuable in
                 instance.GetComponentsInChildren<ValuableObject>(true))
        {
            PhotonView? view = valuable.GetComponent<PhotonView>() ??
                               valuable.GetComponentInParent<PhotonView>();
            if (SemiFunc.IsMultiplayer() && view != null && view.ViewID != 0)
            {
                view.RPC(
                    nameof(ValuableObject.DollarValueSetRPC),
                    RpcTarget.All,
                    0f);
            }
            else
            {
                valuable.DollarValueSetRPC(0f);
            }
        }
    }

    private static Vector3 GetCastOrigin(
        PlayerAvatar player,
        Vector3 direction,
        float forwardOffset)
    {
        Transform? head = player.playerAvatarVisuals?.headLookAtTransform;
        Vector3 basePosition = head != null
            ? head.position
            : player.transform.position + Vector3.up;
        return basePosition + direction * forwardOffset;
    }

    private static Vector3 GetForwardDirection(PlayerAvatar player)
    {
        try
        {
            if (player.localCamera != null)
            {
                Transform cameraTransform = player.localCamera.GetOverrideTransform();
                if (cameraTransform != null && cameraTransform.forward.sqrMagnitude > 0.001f)
                {
                    return cameraTransform.forward.normalized;
                }
            }
        }
        catch
        {
            // Fall through to avatar transforms if camera state is unavailable.
        }

        Transform body = player.playerTransform != null
            ? player.playerTransform
            : player.transform;
        if (body.forward.sqrMagnitude > 0.001f)
        {
            return body.forward.normalized;
        }
        return Vector3.forward;
    }

    private static void DestroyNetworkObject(GameObject? instance)
    {
        if (instance == null)
        {
            return;
        }
        try
        {
            PhotonView? view = instance.GetComponent<PhotonView>() ??
                               instance.GetComponentInChildren<PhotonView>(true);
            if (SemiFunc.IsMultiplayer() && PhotonNetwork.IsMasterClient &&
                view != null && view.ViewID != 0)
            {
                PhotonNetwork.Destroy(view.gameObject);
            }
            else
            {
                UnityEngine.Object.Destroy(instance);
            }
        }
        catch (Exception exception)
        {
            StageRolesPlugin.ModLogger.LogDebug(
                $"Mage object cleanup was deferred: {exception.Message}");
        }
    }
}
