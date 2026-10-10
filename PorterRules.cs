using System;
using System.Collections.Generic;

namespace REPOJP.StageRoles;

// Storage accounting uses original mass, never a temporary grab/vehicle override.
internal static class PorterRules
{
    internal static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

    internal static bool AllowsSize(string allowed, string size)
    {
        if (size is not ("Tiny" or "Small" or "Medium" or "Big" or "Wide" or "Tall" or "VeryTall")) return false;
        foreach (string entry in allowed.Split(','))
            if (string.Equals(entry.Trim(), size, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    internal static bool Fits(float used, float mass, float capacity) =>
        Finite(used) && Finite(mass) && Finite(capacity) && used >= 0 && mass > 0 &&
        capacity > 0 && (double)used + mass <= (double)capacity + 0.000001;

    internal static float UnloadSeconds(float weight, float capacity, float fullSeconds) =>
        !Finite(weight) || !Finite(capacity) || !Finite(fullSeconds) || capacity <= 0
            ? 0 : Math.Clamp(weight / capacity, 0f, 1f) * Math.Max(0, fullSeconds);

    internal static int SpeedLevel(int baseline, float weight, float capacity) =>
        !Finite(weight) || !Finite(capacity) || capacity <= 0 ? Math.Max(0, baseline) :
        (int)Math.Floor(Math.Max(0, baseline) * (1d - Math.Clamp((double)weight / capacity, 0, 1)) + 0.000001);
}

internal sealed class PorterUnload
{
    internal float ReadyAt { get; private set; } = -1;
    internal bool Active => ReadyAt >= 0;
    internal void Reset() => ReadyAt = -1;
    internal bool Tick(bool inDeliveryArea, float weight, float capacity, float fullSeconds, float now)
    {
        if (!inDeliveryArea || weight <= 0) { Reset(); return false; }
        if (!Active) ReadyAt = now + PorterRules.UnloadSeconds(weight, capacity, fullSeconds);
        return now >= ReadyAt;
    }
}

internal sealed class PorterHold
{
    private int _candidate;
    private float _started;

    internal void Reset() { _candidate = 0; _started = 0; }

    internal bool Ready(int candidate, float now, float seconds)
    {
        if (candidate == 0 || !PorterRules.Finite(now) || !PorterRules.Finite(seconds) || seconds <= 0)
        { Reset(); return false; }
        if (_candidate != candidate || now < _started)
        { _candidate = candidate; _started = now; return false; }
        return now - _started >= seconds;
    }
}

internal sealed class PorterLoad
{
    private readonly Dictionary<int, float> _weights = new();
    internal float Weight
    {
        get { double sum = 0; foreach (float weight in _weights.Values) sum += weight; return (float)sum; }
    }
    internal int Count => _weights.Count;
    internal bool Contains(int id) => _weights.ContainsKey(id);
    internal bool Add(int id, float weight, float capacity)
    {
        if (id == 0 || Contains(id) || !PorterRules.Fits(Weight, weight, capacity)) return false;
        _weights.Add(id, weight);
        return true;
    }
    internal void Remove(int id) => _weights.Remove(id);
}
