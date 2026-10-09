using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace REPOJP.StageRoles;

internal sealed class TrackerRoleRuntime
{
    private static readonly FieldInfo? SpawnedField = AccessTools.Field(typeof(EnemyParent), "Spawned");
    private static readonly FieldInfo? DeadField = AccessTools.Field(typeof(EnemyHealth), "dead");
    private readonly StageRolesConfig _config;
    private readonly RoleNotifier _notifier;
    private readonly Dictionary<RoleAssignment, Observer> _observers = new();
    private readonly List<RoleAssignment> _removed = new();
    private float _nextTick;
    private sealed class Observer
    {
        internal Observer(PlayerAvatar player) { Player = player; }
        internal readonly PlayerAvatar Player;
        internal readonly TrackerRules Rules = new();
        internal bool Pending;
    }

    internal TrackerRoleRuntime(StageRolesConfig config, RoleNotifier notifier)
    { _config = config; _notifier = notifier; }

    internal void Stop() { _observers.Clear(); _nextTick = 0f; }
    internal void Forget(RoleAssignment assignment) => _observers.Remove(assignment);

    private bool Eligible(RoleAssignment assignment) => _config.Enabled.Value &&
        _config.AnnouncementsEnabled.Value && RoleCatalog.HasCapability(assignment.Role, StageRole.Tracker) &&
        PlayerState.IsLiving(assignment.Player) && assignment.Player.photonView?.Owner != null;

    internal void Tick(IReadOnlyList<RoleAssignment> assignments)
    {
        if (Time.time < _nextTick) return;
        _nextTick = Time.time + 0.25f;
        _removed.Clear();
        foreach (var entry in _observers)
            if (!Contains(assignments, entry.Key) || !Eligible(entry.Key) || entry.Key.Player != entry.Value.Player)
                _removed.Add(entry.Key);
        foreach (RoleAssignment assignment in _removed) _observers.Remove(assignment);

        foreach (RoleAssignment assignment in assignments)
        {
            if (!Eligible(assignment)) continue;
            if (!_observers.TryGetValue(assignment, out Observer? observer))
                _observers.Add(assignment, observer = new Observer(assignment.Player));
            TrackerSample sample = Sample(assignment.Player, assignments);
            observer.Rules.Observe(Time.time, sample, DangerRange);
            if (observer.Pending || Choose(observer, sample) == TrackerNotice.None) continue;
            observer.Pending = true;
            bool Valid() => Contains(assignments, assignment) && Eligible(assignment) &&
                assignment.Player == observer.Player &&
                _observers.TryGetValue(assignment, out Observer? current) && ReferenceEquals(current, observer);
            // A single replaceable report waits in the existing per-player queue.
            // Re-sample at dispatch, so turn/movement/death cannot deliver old bearings.
            bool sent = _notifier.NotifyPrivate(observer.Player, "Tracker", Valid, () =>
            {
                if (!Valid()) return false;
                TrackerSample latest = Sample(observer.Player, assignments);
                observer.Rules.Observe(Time.time, latest, DangerRange);
                TrackerNotice notice = Choose(observer, latest);
                if (notice == TrackerNotice.None ||
                    !PrivatePlayerSpeech.Send(observer.Player, TrackerRules.Message(notice, latest))) return false;
                observer.Rules.Delivered(Time.time, notice, latest, _config.TrackerNotificationInterval.Value);
                return true;
            }, () => observer.Pending = false);
            if (!sent) observer.Pending = false;
        }
    }

    private float DangerRange => Math.Min(_config.TrackerEnemyRange.Value, _config.TrackerDangerRange.Value);
    private TrackerNotice Choose(Observer observer, TrackerSample sample) =>
        observer.Rules.Choose(Time.time, sample, DangerRange, _config.TrackerDistanceChange.Value);

    private TrackerSample Sample(PlayerAvatar player, IReadOnlyList<RoleAssignment> assignments)
    {
        Vector3 origin = player.transform.position;
        Transform facing = player.playerTransform != null ? player.playerTransform : player.transform;
        TrackerContact? nearestEnemy = null, nearestHead = null;
        float nearestEnemyDistance = float.PositiveInfinity;
        var enemies = EnemyDirector.instance?.enemiesSpawned;
        if (enemies != null)
            foreach (EnemyParent parent in enemies)
            {
                if (parent == null || !parent.gameObject.activeInHierarchy ||
                    SpawnedField?.GetValue(parent) is not true) continue;
                Enemy? enemy = parent.GetComponentInChildren<Enemy>();
                if (enemy == null || !enemy.gameObject.activeInHierarchy ||
                    enemy.CurrentState is EnemyState.Spawn or EnemyState.Despawn) continue;
                EnemyHealth? health = parent.GetComponentInChildren<EnemyHealth>();
                if (health != null && DeadField?.GetValue(health) is not false) continue;
                Vector3 position = enemy.CenterTransform != null ? enemy.CenterTransform.position : enemy.transform.position;
                TrackerContact contact = Contact(parent.GetInstanceID(), position - origin, facing.forward);
                nearestEnemyDistance = Math.Min(nearestEnemyDistance, contact.Distance);
                if (contact.Distance <= _config.TrackerEnemyRange.Value &&
                    (!nearestEnemy.HasValue || contact.Distance < nearestEnemy.Value.Distance)) nearestEnemy = contact;
            }
        foreach (RoleAssignment target in assignments)
        {
            if (target.Player == null || target.Player == player || PlayerState.IsLiving(target.Player) ||
                !PlayerState.TryGetDeathHeadRuntime(target.Player, out PlayerDeathHead? head, out _, out _) ||
                head == null || !head.gameObject.activeInHierarchy) continue;
            TrackerContact contact = Contact(head.GetInstanceID(), head.transform.position - origin,
                facing.forward, ShortName(target.Player));
            if (!nearestHead.HasValue || contact.Distance < nearestHead.Value.Distance) nearestHead = contact;
        }
        return new TrackerSample(nearestEnemy, nearestHead, nearestEnemyDistance);
    }

    private static TrackerContact Contact(int id, Vector3 offset, Vector3 forward, string name = "")
    {
        float distance = offset.magnitude;
        int height = offset.y >= 3f ? 1 : offset.y <= -3f ? -1 : 0;
        offset.y = 0f; forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
        float bearing = offset.sqrMagnitude < 0.001f ? 0f : Vector3.SignedAngle(forward, offset, Vector3.up);
        return new TrackerContact(id, distance, bearing, height, name);
    }

    private static string ShortName(PlayerAvatar player)
    {
        string name = string.Concat(PlayerIdentity.Name(player).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        name = name.Replace(":", "");
        return name.Length == 0 ? "Player" : name.Length > 16 ? name.Substring(0, 16) : name;
    }

    private static bool Contains(IReadOnlyList<RoleAssignment> assignments, RoleAssignment target)
    {
        foreach (RoleAssignment assignment in assignments) if (ReferenceEquals(assignment, target)) return true;
        return false;
    }
}
