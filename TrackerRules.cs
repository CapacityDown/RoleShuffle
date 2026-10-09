using System;
using System.Globalization;

namespace REPOJP.StageRoles;

internal enum TrackerNotice { None, Enemy, Head, Danger }

internal readonly struct TrackerContact
{
    internal TrackerContact(int id, float distance, float bearing, int height, string name = "")
    { Id = id; Distance = distance; Bearing = bearing; Height = height; Name = name; }
    internal int Id { get; }
    internal float Distance { get; }
    internal float Bearing { get; }
    internal int Height { get; }
    internal string Name { get; }

    internal string Describe(string prefix)
    {
        string[] directions = { "Front", "FrontRight", "Right", "BackRight", "Back", "BackLeft", "Left", "FrontLeft" };
        int sector = ((int)Math.Floor((Bearing + 22.5f) / 45f) % 8 + 8) % 8;
        string height = Height > 0 ? ":Above" : Height < 0 ? ":Below" : "";
        string name = Name.Length > 0 ? ":" + Name : "";
        return prefix + name + ":" + directions[sector] + height + ":" +
               Math.Ceiling(Distance).ToString(CultureInfo.InvariantCulture) + "m";
    }
}

internal readonly struct TrackerSample
{
    internal TrackerSample(TrackerContact? enemy, TrackerContact? head, float nearestEnemyDistance = float.NaN)
    { Enemy = enemy; Head = head; NearestEnemyDistance = float.IsNaN(nearestEnemyDistance) ? enemy?.Distance ?? float.PositiveInfinity : nearestEnemyDistance; }
    internal TrackerContact? Enemy { get; }
    internal TrackerContact? Head { get; }
    internal float NearestEnemyDistance { get; }
}

// Stores only delivered reports. Waiting, expired or cancelled notices spend no cooldown.
internal sealed class TrackerRules
{
    private TrackerContact? _enemy, _head;
    private float _normalReadyAt, _dangerReadyAt, _awaySince = -1f;
    private bool _dangerLatched, _lastWasEnemy;

    internal void Observe(float now, TrackerSample sample, float dangerRange)
    {
        if (!_dangerLatched) return;
        float resetRange = dangerRange + Math.Max(2f, dangerRange * 0.375f);
        if (sample.NearestEnemyDistance <= resetRange)
        { _awaySince = -1f; return; }
        if (_awaySince < 0f) _awaySince = now;
        if (now - _awaySince >= 3f) { _dangerLatched = false; _awaySince = -1f; }
    }

    internal TrackerNotice Choose(float now, TrackerSample sample, float dangerRange, float distanceChange)
    {
        if (dangerRange > 0f && !_dangerLatched && now >= _dangerReadyAt &&
            sample.Enemy.HasValue && sample.Enemy.Value.Distance <= dangerRange)
            return TrackerNotice.Danger;
        if (now < _normalReadyAt) return TrackerNotice.None;
        bool enemy = Changed(_enemy, sample.Enemy, distanceChange);
        bool head = Changed(_head, sample.Head, distanceChange);
        if (head && (!enemy || _lastWasEnemy)) return TrackerNotice.Head;
        return enemy ? TrackerNotice.Enemy : TrackerNotice.None;
    }

    internal void Delivered(float now, TrackerNotice notice, TrackerSample sample, float interval)
    {
        if (notice == TrackerNotice.None) return;
        _normalReadyAt = now + interval;
        _lastWasEnemy = notice != TrackerNotice.Head;
        if (notice == TrackerNotice.Head) _head = sample.Head;
        else _enemy = sample.Enemy;
        if (notice == TrackerNotice.Danger)
        { _dangerLatched = true; _awaySince = -1f; _dangerReadyAt = now + interval; }
    }

    internal static string Message(TrackerNotice notice, TrackerSample sample) => notice switch
    {
        TrackerNotice.Danger => sample.Enemy?.Describe("Danger") ?? "",
        TrackerNotice.Enemy => sample.Enemy?.Describe("Enemy") ?? "Enemy:Clear",
        TrackerNotice.Head => sample.Head?.Describe("Head") ?? "Head:Clear",
        _ => ""
    };

    private static bool Changed(TrackerContact? previous, TrackerContact? current, float distanceChange)
    {
        if (!previous.HasValue || !current.HasValue) return previous.HasValue != current.HasValue;
        TrackerContact a = previous.Value, b = current.Value;
        float angle = Math.Abs(a.Bearing - b.Bearing) % 360f;
        angle = Math.Min(angle, 360f - angle);
        return a.Id != b.Id || a.Height != b.Height ||
               Math.Abs(a.Distance - b.Distance) >= distanceChange || angle >= 60f;
    }
}
