using System;
using System.Collections.Generic;

namespace REPOJP.StageRoles;

// Session entitlement: one grant per player per stage, no accumulation, and
// only the immediately following shop may consume it. Kept outside assignments.
internal sealed class NegotiatorState
{
    private readonly Dictionary<string, bool> _tickets = new(StringComparer.Ordinal);
    private readonly HashSet<string> _granted = new(StringComparer.Ordinal);
    internal bool InShop { get; private set; }
    internal IEnumerable<string> Players => _tickets.Keys;
    internal void BeginStage() => _granted.Clear();
    internal void Grant(string id)
    {
        if (InShop || string.IsNullOrEmpty(id) || !_granted.Add(id)) return;
        _tickets[id] = true;
    }
    internal bool Has(string id) => _tickets.ContainsKey(id);
    internal int Remaining(string id) => _tickets.TryGetValue(id, out bool unused) && unused ? 1 : 0;
    internal void EnterShop() => InShop = true;
    internal bool Consume(string id)
    {
        if (!InShop || Remaining(id) == 0) return false;
        _tickets[id] = false;
        return true;
    }
    internal void LeaveShop()
    {
        if (!InShop) return;
        _tickets.Clear();
        InShop = false;
    }
    internal void Reset() { _tickets.Clear(); _granted.Clear(); InShop = false; }
}
