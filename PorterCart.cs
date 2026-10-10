using System;
using System.Reflection;
using HarmonyLib;
using Photon.Pun;
using UnityEngine;

namespace REPOJP.StageRoles;

internal static class PorterCart
{
    private static readonly FieldInfo? GroundCollider = AccessTools.Field(typeof(PlayerCollisionGrounded), "Collider");
    private static readonly FieldInfo? Riding = AccessTools.Field(typeof(PlayerAvatar), "clientPhysRiding");
    private static readonly FieldInfo? RidingId = AccessTools.Field(typeof(PlayerAvatar), "clientPhysRidingID");

    internal static PhysGrabCart? RidingCart(PlayerAvatar player)
    {
        if (player == SemiFunc.PlayerAvatarLocal())
        {
            PlayerCollisionGrounded? ground = PlayerController.instance?.CollisionGrounded;
            if (ground == null || !ground.physRiding || GroundCollider?.GetValue(ground) is not SphereCollider collider) return null;
            // Use the same contact sphere as vanilla, also when solo carts have ViewID 0.
            PhysGrabCart? selected = null;
            foreach (Collider contact in Physics.OverlapSphere(ground.transform.position,
                collider.radius, ground.LayerMask, QueryTriggerInteraction.Ignore))
            {
                PhysGrabCart? cart = contact.GetComponentInParent<PhysGrabCart>();
                if (Area(cart) != null && (selected == null || cart!.GetInstanceID() < selected.GetInstanceID()))
                    selected = cart;
            }
            return selected;
        }
        // Vanilla guests already send the ridden PhotonView ID to the host.
        if (Riding?.GetValue(player) is not true || RidingId?.GetValue(player) is not int id || id <= 0) return null;
        PhysGrabCart? remoteCart = PhotonView.Find(id)?.GetComponent<PhysGrabCart>();
        return Area(remoteCart) != null ? remoteCart : null;
    }

    private static Transform? Area(PhysGrabCart? cart)
    {
        if (cart == null || !cart.isActiveAndEnabled || cart.transform.up.y < 0.5f) return null;
        // This is the oriented volume used by PhysGrabCart.ObjectsInCart itself.
        Transform? area = cart.transform.Find("In Cart");
        if (area == null) return null;
        Vector3 size = area.localScale;
        return PorterRules.Finite(size.x) && PorterRules.Finite(size.y) && PorterRules.Finite(size.z) &&
            size.x > 0 && size.y > 0 && size.z > 0 ? area : null;
    }

    internal static Vector3 DropPosition(PhysGrabCart cart, int index)
    {
        Transform area = Area(cart) ?? throw new InvalidOperationException("Cart cargo area unavailable.");
        Vector3 size = area.localScale;
        // Keep release points inside the native cargo volume, away from its walls.
        float angle = index * 2.3999632f;
        float radius = index == 0 ? 0 : 0.22f;
        Vector3 offset = new(Mathf.Cos(angle) * size.x * radius, size.y * 0.2f,
            Mathf.Sin(angle) * size.z * radius);
        return area.position + area.rotation * offset;
    }

    internal static Vector3 Velocity(PhysGrabCart cart, Vector3 position) =>
        cart.GetComponent<Rigidbody>()?.GetPointVelocity(position) ?? Vector3.zero;
}
