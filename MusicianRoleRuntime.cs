using System;
using System.Collections.Generic;
using UnityEngine;

namespace REPOJP.StageRoles;

// One ledger per performer for the whole stage, shared by every instrument and
// retained across role changes, death and revival. Only stage cleanup clears it.
internal sealed class MusicianRoleRuntime
{
    private sealed class State
    {
        internal int Used;
        internal float NextHealAt = float.NegativeInfinity;
    }
    private readonly Dictionary<string, State> _players = new(StringComparer.Ordinal);

    internal void Clear() => _players.Clear();
    internal int HealingUsed(string steamId) => _players.TryGetValue(steamId, out State? state) ? state.Used : 0;

    internal void Play(PlayerAvatar musician, string steamId, IEnumerable<PlayerAvatar> players,
        int amount, float radius, float cooldown, int limit)
    {
        if (string.IsNullOrEmpty(steamId) || !PlayerState.IsLiving(musician)) return;
        if (!_players.TryGetValue(steamId, out State? state)) _players[steamId] = state = new State();
        limit = Mathf.Clamp(limit, 1, 10000);
        if (Time.time < state.NextHealAt || state.Used >= limit) return;
        state.NextHealAt = Time.time + Mathf.Clamp(cooldown, 0.1f, 30f);
        radius = Mathf.Clamp(radius, 1f, 50f);
        amount = Mathf.Clamp(amount, 1, 100);
        HashSet<PlayerAvatar> seen = new();
        foreach (PlayerAvatar player in players)
        {
            if (state.Used >= limit) break;
            if (!seen.Add(player) || !PlayerState.IsLiving(player) ||
                (player.transform.position - musician.transform.position).sqrMagnitude > radius * radius ||
                !PlayerState.TryGetCurrentHealth(player, out int health) ||
                !PlayerState.TryGetMaximumHealth(player, out int maximum)) continue;
            int reserved = Math.Min(Math.Min(amount, limit - state.Used), Math.Max(0, maximum - health));
            if (reserved <= 0) continue;
            state.Used += reserved;
            try
            {
                if (!RoleHealingRuntime.TryHeal(player, reserved,
                    restored => state.Used -= reserved - Math.Clamp(restored, 0, reserved)))
                    state.Used -= reserved;
            }
            catch (Exception exception)
            {
                // Local failures report their actual result; uncertain remote
                // sends retain their reservation to prevent exceeding the cap.
                StageRolesPlugin.ModLogger.LogDebug($"Musician healing was skipped: {exception.Message}");
            }
        }
    }
}
