using System;
using System.Collections.Generic;
using UnityEngine;

namespace REPOJP.StageRoles;

internal readonly struct DualWielderPose(float time, Vector3 position, Quaternion rotation,
    Quaternion hitRotation, bool attacking, int enemyDamage, int playerDamage)
{
    internal readonly float Time = time;
    internal readonly Vector3 Position = position;
    internal readonly Quaternion Rotation = rotation;
    internal readonly Quaternion HitRotation = hitRotation;
    internal readonly bool Attacking = attacking;
    internal readonly int EnemyDamage = enemyDamage;
    internal readonly int PlayerDamage = playerDamage;
}

// Timestamp-based interpolation: the delay stays constant when the frame rate changes.
internal sealed class DualWielderHistory
{
    private readonly LinkedList<DualWielderPose> _poses = new();
    internal int Count => _poses.Count;
    internal void Clear() => _poses.Clear();
    internal void Record(DualWielderPose pose)
    {
        if (_poses.Last != null && (pose.Time <= _poses.Last.Value.Time ||
            (pose.Position - _poses.Last.Value.Position).sqrMagnitude > 100f)) _poses.Clear();
        _poses.AddLast(pose);
        while (_poses.Count > 4096 || (_poses.First != null && pose.Time - _poses.First.Value.Time > 3f))
            _poses.RemoveFirst();
    }

    internal bool Sample(float time, out DualWielderPose pose)
    {
        pose = default;
        if (_poses.First == null || time < _poses.First.Value.Time) return false;
        while (_poses.First.Next != null && _poses.First.Next.Value.Time <= time) _poses.RemoveFirst();
        DualWielderPose a = _poses.First.Value;
        if (_poses.First.Next == null) { pose = a; return true; }
        DualWielderPose b = _poses.First.Next.Value;
        float fraction = Mathf.Clamp01((time - a.Time) / (b.Time - a.Time));
        // Attack flags never interpolate into the future; only the older sample is authoritative.
        pose = new DualWielderPose(time, Vector3.Lerp(a.Position, b.Position, fraction),
            Quaternion.Slerp(a.Rotation, b.Rotation, fraction),
            Quaternion.Slerp(a.HitRotation, b.HitRotation, fraction), a.Attacking, a.EnemyDamage, a.PlayerDamage);
        return true;
    }
}
