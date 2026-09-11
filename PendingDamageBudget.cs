using System;
using System.Collections.Generic;

namespace REPOJP.StageRoles;

internal sealed class PendingDamageBudget
{
    internal sealed class Request
    {
        internal string PlayerId = string.Empty;
        internal int Before;
        internal int After;
        internal int Amount;
    }

    private readonly Dictionary<string, List<Request>> _pending = new(StringComparer.Ordinal);

    internal int Reserved(string playerId)
    {
        long total = 0;
        if (_pending.TryGetValue(playerId, out List<Request>? requests))
            foreach (Request request in requests) total += request.Amount;
        return (int)Math.Min(int.MaxValue, total);
    }

    internal int Available(string playerId, int health) =>
        Math.Max(0, health - Reserved(playerId) - 1);

    internal Request Add(string playerId, int health, int amount)
    {
        int before = Math.Max(0, health - Reserved(playerId));
        Request request = new()
        {
            PlayerId = playerId, Before = before,
            After = Math.Max(0, before - amount), Amount = Math.Max(0, amount)
        };
        if (!_pending.TryGetValue(playerId, out List<Request>? requests))
            _pending[playerId] = requests = new List<Request>();
        requests.Add(request);
        return request;
    }

    internal bool Observe(string playerId, int before, int after)
    {
        if (!_pending.TryGetValue(playerId, out List<Request>? requests) || requests.Count == 0)
            return false;
        Request request = requests[0];
        if (request.Before != before || request.After != after)
            return false;
        Remove(request);
        return true;
    }

    internal void Remove(Request request)
    {
        if (_pending.TryGetValue(request.PlayerId, out List<Request>? requests))
        {
            requests.Remove(request);
            if (requests.Count == 0) _pending.Remove(request.PlayerId);
        }
    }

    internal void Clear(string playerId) => _pending.Remove(playerId);
    internal void Clear() => _pending.Clear();
}
