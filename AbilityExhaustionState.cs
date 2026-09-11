using System.Collections.Generic;

namespace REPOJP.StageRoles;

internal sealed class AbilityExhaustionState
{
    private readonly HashSet<int> _announced = new();
    private readonly HashSet<int> _queued = new();

    internal bool TryQueue(int ability, bool exhausted)
    {
        if (!exhausted)
        {
            _announced.Remove(ability);
            return false;
        }
        return !_announced.Contains(ability) && _queued.Add(ability);
    }

    internal void Sent(int ability) => _announced.Add(ability);
    internal void Finished(int ability) => _queued.Remove(ability);
    internal void Rearm(int ability) => _announced.Remove(ability);
    internal void Rearm() => _announced.Clear();
}
