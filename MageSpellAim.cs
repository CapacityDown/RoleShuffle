using UnityEngine;

namespace REPOJP.StageRoles;

internal static class MageSpellAim
{
    internal static Vector3 GetOrigin(PlayerAvatar player, Vector3 direction, float forwardOffset)
    {
        Transform? head = player.playerAvatarVisuals?.headLookAtTransform;
        Vector3 basePosition = head != null
            ? head.position
            : player.transform.position + Vector3.up;
        return basePosition + direction * forwardOffset;
    }

    internal static Vector3 GetDirection(PlayerAvatar player)
    {
        try
        {
            if (player.localCamera != null)
            {
                Transform cameraTransform = player.localCamera.GetOverrideTransform();
                if (cameraTransform != null && cameraTransform.forward.sqrMagnitude > 0.001f)
                    return cameraTransform.forward.normalized;
            }
        }
        catch
        {
            // Remote/local camera state may be unavailable during transitions.
        }

        Transform body = player.playerTransform != null ? player.playerTransform : player.transform;
        return body.forward.sqrMagnitude > 0.001f ? body.forward.normalized : Vector3.forward;
    }

    internal static void GetBeamPose(PlayerAvatar player, Vector3 relativeLaserPosition,
        Quaternion relativeLaserRotation, out Vector3 position, out Quaternion rotation)
    {
        Vector3 direction = GetDirection(player);
        rotation = Quaternion.LookRotation(direction) * Quaternion.Inverse(relativeLaserRotation);
        position = GetOrigin(player, direction, 3f) - rotation * relativeLaserPosition;
    }
}
