using System;
using System.Collections.Generic;

namespace REPOJP.StageRoles;

internal enum NegotiatorReward { Delivery = 1, EnemyKill = 2 }

// Stage earnings survive normal stage completion, but never death or role loss.
internal sealed class NegotiatorState
{
    internal const int MaximumRights = 2;
    private readonly Dictionary<string, int> _tickets = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _earned = new(StringComparer.Ordinal);
    private readonly HashSet<string> _participants = new(StringComparer.Ordinal);
    private readonly HashSet<string> _revoked = new(StringComparer.Ordinal);
    internal bool InShop { get; private set; }
    internal void BeginStage() { _earned.Clear(); _participants.Clear(); _revoked.Clear(); }
    internal void Activate(string id)
    {
        if (!InShop && !string.IsNullOrEmpty(id)) _participants.Add(id);
    }
    internal bool CanEarn(string id, NegotiatorReward reward) => !InShop &&
        _participants.Contains(id) && !_revoked.Contains(id) && Remaining(id) < MaximumRights &&
        (!_earned.TryGetValue(id, out int mask) || (mask & (int)reward) == 0);
    internal bool TryEarn(string id, NegotiatorReward reward)
    {
        if (!CanEarn(id, reward)) return false;
        _earned.TryGetValue(id, out int mask);
        _earned[id] = mask | (int)reward;
        _tickets[id] = Remaining(id) + 1;
        return true;
    }
    internal bool Revoke(string id)
    {
        if (!_participants.Contains(id) && !_tickets.ContainsKey(id)) return false;
        bool removed = _tickets.Remove(id);
        return _revoked.Add(id) || removed;
    }
    internal bool Has(string id) => _tickets.ContainsKey(id);
    internal int Remaining(string id) => _tickets.TryGetValue(id, out int count) ? count : 0;
    internal void EnterShop() => InShop = true;
    internal bool Consume(string id)
    {
        if (!InShop || Remaining(id) == 0) return false;
        _tickets[id]--;
        return true;
    }
    internal void LeaveShop()
    {
        if (!InShop) return;
        _tickets.Clear();
        InShop = false;
    }
    internal void Reset()
    {
        _tickets.Clear(); _earned.Clear(); _participants.Clear(); _revoked.Clear(); InShop = false;
    }
}
