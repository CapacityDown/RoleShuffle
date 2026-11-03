using System;
using System.Reflection;
using HarmonyLib;
using Photon.Pun;
using UnityEngine;

namespace REPOJP.StageRoles;

// Body contact sensors extend the native attack volume. Both paths share the
// native per-target cooldown so a valuable is never charged twice for one hit.
internal sealed class DualWielderContacts(DualWielderEcho echo, ItemMelee melee, PhysGrabObject physics)
{
    private static readonly MethodInfo CanHit = AccessTools.Method(typeof(HurtCollider), "CanHit")
        ?? throw new MissingMethodException(nameof(HurtCollider), "CanHit");
    private static readonly MethodInfo NativePlayerHit = AccessTools.Method(typeof(HurtCollider), "PlayerHurt")
        ?? throw new MissingMethodException(nameof(HurtCollider), "PlayerHurt");
    private static readonly MethodInfo PhysHit = AccessTools.Method(typeof(HurtCollider), "PhysObjectHurt")
        ?? throw new MissingMethodException(nameof(HurtCollider), "PhysObjectHurt");
    private static readonly MethodInfo HitForce = AccessTools.Method(typeof(HurtCollider), "HitForceGet")
        ?? throw new MissingMethodException(nameof(HurtCollider), "HitForceGet");
    private static readonly FieldInfo PlayerImpact = AccessTools.Field(typeof(HurtCollider), "onImpactPlayerAvatar")
        ?? throw new MissingFieldException(nameof(HurtCollider), "onImpactPlayerAvatar");
    private static readonly FieldInfo PvpCooldown = AccessTools.Field(typeof(ItemMelee), "enemyOrPVPDurabilityLossCooldown")
        ?? throw new MissingFieldException(nameof(ItemMelee), "enemyOrPVPDurabilityLossCooldown");
    private static readonly FieldInfo DestroyDisabled = AccessTools.Field(typeof(PhysGrabObjectImpactDetector), "destroyDisable")
        ?? throw new MissingFieldException(nameof(PhysGrabObjectImpactDetector), "destroyDisable");

    private static readonly FieldInfo HingeJoint = AccessTools.Field(typeof(PhysGrabHinge), "joint")
        ?? throw new MissingFieldException(nameof(PhysGrabHinge), "joint");
    private static readonly FieldInfo AvatarTumble = AccessTools.Field(typeof(PlayerAvatar), "tumble")
        ?? throw new MissingFieldException(nameof(PlayerAvatar), "tumble");
    private static readonly FieldInfo Tumbling = AccessTools.Field(typeof(PlayerAvatar), "isTumbling")
        ?? throw new MissingFieldException(nameof(PlayerAvatar), "isTumbling");

    private static bool Allow(HurtCollider hurt, GameObject target, float cooldown, bool raycast,
        Vector3 position, HurtCollider.HitType type) => cooldown > 0 &&
        (bool)CanHit.Invoke(hurt, new object[] { target, cooldown, raycast, position, type })!;

    internal void Touch(HurtCollider hurt, Collider other)
    {
        if (!echo.CanContact || other == null || !other.enabled || other.isTrigger) return;
        if (other.GetComponentInParent<DualWielderEcho>(true) != null) return;
        PlayerAvatar? player = other.GetComponentInParent<PlayerAvatar>(true)
            ?? other.GetComponentInParent<PlayerController>(true)?.playerAvatarScript
            ?? other.GetComponentInParent<PlayerTumble>(true)?.playerAvatar;
        if (player != null) { NativePlayerHit.Invoke(hurt, new object[] { player }); return; }
        if (other.GetComponentInParent<Enemy>(true) != null ||
            other.GetComponentInParent<EnemyRigidbody>(true) != null ||
            other.GetComponentInParent<EnemyParent>(true) != null) return;
        PhysGrabObject? target = other.GetComponentInParent<PhysGrabObject>(true);
        if (target != null)
        {
            if (target == physics || target == echo.Source.GetComponent<PhysGrabObject>() ||
                hurt.ignoreObjects.Contains(target)) return;
            HitObject(hurt, target);
            return;
        }
        // Static scenery uses native feedback and cooldown, with no durability cost.
        melee.GroundHit();
    }

    private void HitObject(HurtCollider hurt, PhysGrabObject target)
    {
        var detector = target.GetComponent<PhysGrabObjectImpactDetector>();
        if (!hurt.physLogic || detector == null || !Allow(hurt, target.gameObject,
            hurt.physDamageCooldown, hurt.physRayCast, target.transform.position, HurtCollider.HitType.PhysObject)) return;
        PhysGrabHinge? hinge = target.GetComponent<PhysGrabHinge>();
        bool hit = false;
        if (hurt.physHingeDestroy && hinge != null) { hinge.DestroyHinge(); hit = true; }
        else
        {
            if (hurt.physHingeBreak && hinge != null && HingeJoint.GetValue(hinge) is Joint joint)
            { joint.breakForce = 0; joint.breakTorque = 0; }
            if (hurt.physDestroy && !(bool)DestroyDisabled.GetValue(detector)!)
            {
                if (hinge != null) hinge.DestroyHinge(); else detector.DestroyObject();
                hit = true;
            }
            else
            {
                if (hurt.physReleaseGrab)
                    foreach (PhysGrabber holder in target.playerGrabbing.ToArray())
                        if (holder != null) holder.OverrideGrabRelease(target.GetComponent<PhotonView>().ViewID, 0.25f);
                bool launch = hurt.physDestroy;
                hit = (bool)PhysHit.Invoke(hurt, new object?[] { target,
                    launch ? HurtCollider.BreakImpact.Heavy : hurt.physImpact,
                    launch ? 50f : hurt.physHitForce, launch ? 30f : hurt.physHitTorque,
                    true, launch, null })!;
            }
        }
        if (!hit) return;
        hurt.onImpactAny?.Invoke();
        hurt.onImpactPhysObject?.Invoke();
        SemiFunc.PlayerShopKeeperMeleePhysHitHost(echo.Owner);
    }

    internal void HitPlayer(HurtCollider hurt, PlayerAvatar target)
    {
        if (!echo.CanContact || !hurt.playerLogic || target == null || target == echo.Owner ||
            hurt.ignorePlayers.Contains(target) || !PlayerState.IsLiving(target) || target.playerHealth == null ||
            !Allow(hurt, target.gameObject, hurt.playerDamageCooldown, hurt.playerRayCast,
                target.transform.position, HurtCollider.HitType.Player)) return;
        int damage = hurt.playerDamage;
        if (hurt.playerKill && PlayerState.TryGetCurrentHealth(target, out int health)) damage = health;
        // Native owner-side subtraction preserves invulnerability, saving grace
        // and concurrent damage. This also works for a vanilla remote player.
        if (damage > 0) target.playerHealth.HurtOther(damage, Vector3.zero, true);
        Vector3 center = hurt.GetComponent<Collider>().bounds.center;
        Vector3 direction = SemiFunc.ClampDirection((target.transform.position - center).normalized,
            hurt.transform.forward, hurt.hitSpread);
        Vector3 Force(float strength) => (Vector3)HitForce.Invoke(hurt, new object[] { direction, strength })!;
        PlayerTumble? tumble = AvatarTumble.GetValue(target) as PlayerTumble;
        bool knocked = false;
        if (tumble != null)
        {
            if (hurt.playerTumbleTime > 0 || hurt.playerStunTime > 0)
            {
                tumble.TumbleRequest(true, false);
                if (hurt.playerTumbleTime > 0) tumble.TumbleOverrideTime(hurt.playerTumbleTime);
                if (hurt.playerStunTime > 0) tumble.TumbleStunTime(hurt.playerStunTime);
                if (hurt.playerTumbleImpactHurtTime > 0)
                    tumble.ImpactHurtSet(hurt.playerTumbleImpactHurtTime, hurt.playerTumbleImpactHurtDamage);
                if (hurt.playerTumbleEnemyHurtDisableTime > 0)
                    tumble.OverrideEnemyHurt(hurt.playerTumbleEnemyHurtDisableTime);
                knocked = true;
            }
            if (knocked || (bool)Tumbling.GetValue(target)!)
            {
                if (hurt.playerTumbleForce > 0) tumble.TumbleForce(Force(hurt.playerTumbleForce));
                if (hurt.playerTumbleTorque > 0)
                {
                    Vector3 axis = hurt.playerTumbleTorqueAxis switch
                    {
                        HurtCollider.TorqueAxis.up => target.transform.up,
                        HurtCollider.TorqueAxis.left => -target.transform.right,
                        HurtCollider.TorqueAxis.right => target.transform.right,
                        HurtCollider.TorqueAxis.forward => target.transform.forward,
                        HurtCollider.TorqueAxis.back => -target.transform.forward,
                        _ => -target.transform.up
                    };
                    tumble.TumbleTorque(Vector3.Cross(direction, axis) * hurt.playerTumbleTorque);
                }
                knocked = true;
            }
        }
        if (!knocked && hurt.playerHitForce > 0) target.ForceImpulse(Force(hurt.playerHitForce));
        if (damage <= 0 && !knocked && hurt.playerHitForce <= 0) return;
        PlayerImpact.SetValue(hurt, target);
        hurt.onImpactAny?.Invoke();
        hurt.onImpactPlayer?.Invoke();
        // Notify native combat/shop accounting. HP is still owned by the target.
        SemiFunc.PlayerDamagingPlayer(echo.Owner, target, Math.Max(0, damage), knocked, false, hurt.playerHitFullRuckus);
    }

    internal void PlayerHitFeedback()
    {
        if (melee.swingLogicOnly || (float)PvpCooldown.GetValue(melee)! > 0) return;
        if (SemiFunc.IsMultiplayer()) melee.GetComponent<PhotonView>().RPC("EnemyOrPVPSwingHitRPC", RpcTarget.All, true);
        else melee.EnemyOrPVPSwingHitRPC(true);
    }
}
