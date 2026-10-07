using UnityEngine;

namespace REPOJP.StageRoles;

// Host-side observation; never freezes the head or changes its physics.
internal sealed class DeathHeadStillness
{
    private const float RequiredStillSeconds = 0.5f;
    private const float MaximumSampleGapSeconds = 0.25f;
    private const float MaximumLinearSpeedSquared = 0.05f * 0.05f;
    private const float MaximumAngularSpeedSquared = 0.15f * 0.15f;
    private const float MaximumPositionDriftSquared = 0.01f * 0.01f;
    private const float MaximumRotationDriftDegrees = 2f;
    private PlayerDeathHead? _head;
    private bool _hasSample;
    private Vector3 _anchorPosition;
    private Quaternion _anchorRotation;
    private float _stillSince;
    private float _sampledAt;

    internal bool Observe(PlayerAvatar? player)
    {
        if (PlayerState.IsLiving(player) ||
            !PlayerState.TryGetDeathHeadRuntime(player, out PlayerDeathHead? head,
                out PhysGrabObject? phys, out _) ||
            head == null || !head.gameObject.activeInHierarchy ||
            phys == null || phys.rb == null)
        {
            Reset();
            return false;
        }

        float now = Time.time;
        Vector3 position = head.transform.position;
        Quaternion rotation = head.transform.rotation;
        bool continuous = _hasSample && _head == head &&
            now >= _sampledAt && now - _sampledAt <= MaximumSampleGapSeconds;
        _head = head;
        _sampledAt = now;
        _hasSample = true;

        // The transform checks also catch heads moved by network interpolation,
        // a carrier or teleportation while the host Rigidbody reports zero speed.
        if (!continuous ||
            !(phys.rb.velocity.sqrMagnitude <= MaximumLinearSpeedSquared) ||
            !(phys.rb.angularVelocity.sqrMagnitude <= MaximumAngularSpeedSquared) ||
            !((position - _anchorPosition).sqrMagnitude <= MaximumPositionDriftSquared) ||
            !(Quaternion.Angle(rotation, _anchorRotation) <= MaximumRotationDriftDegrees))
        {
            _anchorPosition = position;
            _anchorRotation = rotation;
            _stillSince = now;
            return false;
        }

        return now - _stillSince >= RequiredStillSeconds;
    }

    internal void Reset()
    {
        _head = null;
        _hasSample = false;
    }
}
