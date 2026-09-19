using System;
using System.Collections.Generic;

namespace REPOJP.StageRoles;

// Only the host owns this ledger. Native upgrade RPCs also work for unmodded guests.
internal static class KingUpgradeAura
{
    private static readonly string[] Commands = { "Speed", "Range", "Strength" };
    private static readonly Dictionary<string, Dictionary<string, int>> Applied = new(StringComparer.Ordinal);

    internal static int WithoutBonus(string steamId, string dictionaryName, int level) =>
        Math.Max(0, level - Bonus(steamId, dictionaryName));

    private static int Bonus(string steamId, string dictionaryName) =>
        Applied.TryGetValue(steamId, out var levels) ? levels.GetValueOrDefault(dictionaryName, 0) : 0;

    // Absolute role/base resets replace the aura too. The next tick recomputes it.
    internal static void Forget(string steamId, string dictionaryName)
    {
        if (Applied.TryGetValue(steamId, out var levels))
        {
            levels.Remove(dictionaryName);
            bool hasBonus = false;
            foreach (int amount in levels.Values) hasBonus |= amount > 0;
            if (!hasBonus) Applied.Remove(steamId);
        }
    }

    internal static int DesiredBonus(string command, int baseline, int requested)
    {
        int maximum = Math.Min(Math.Clamp(requested, 0, 200), Math.Max(0, 200 - baseline));
        if (command != "Strength") return maximum;
        for (int extra = maximum; extra > 0; extra--)
        {
            bool safe = true;
            foreach (bool light in new[] { true, false })
            foreach (bool rotation in new[] { true, false })
                if (RoleOverhaulRules.EffectiveGrabStrength(baseline + extra, light, rotation) <
                    RoleOverhaulRules.EffectiveGrabStrength(baseline, light, rotation)) safe = false;
            if (safe) return extra;
        }
        return 0;
    }

    internal static void Tick(StageRolesConfig config, IReadOnlyList<RoleAssignment> assignments)
    {
        if (!SemiFunc.IsMasterClientOrSingleplayer()) return;
        List<RoleAssignment> kings = new();
        foreach (var assignment in assignments)
        {
            assignment.Overhaul.KingSupportedAllies = 0;
            if (assignment.Role == StageRole.King && PlayerState.IsLiving(assignment.Player))
                kings.Add(assignment);
        }
        float radiusSquared = config.KingUpgradeRadius.Value * config.KingUpgradeRadius.Value;
        HashSet<string> active = new(StringComparer.Ordinal);
        foreach (var target in assignments)
        {
            active.Add(target.SteamId);
            List<RoleAssignment> nearby = new();
            if (target.Role != StageRole.King && PlayerState.IsLiving(target.Player))
                foreach (var king in kings)
                    if (king.SteamId != target.SteamId &&
                        (target.Player.transform.position - king.Player.transform.position).sqrMagnitude <= radiusSquared)
                        nearby.Add(king);
            bool supported = Reconcile(target.SteamId, nearby.Count > 0 ? config : null);
            if (supported) foreach (var king in nearby) king.Overhaul.KingSupportedAllies++;
        }
        foreach (string steamId in new List<string>(Applied.Keys))
            if (!active.Contains(steamId)) Reconcile(steamId, null);
    }

    private static bool Reconcile(string steamId, StageRolesConfig? config)
    {
        if (!UpgradeService.TryGetLevels(steamId, out var current)) return false;
        if (!Applied.TryGetValue(steamId, out var applied)) applied = new(StringComparer.Ordinal);
        foreach (string command in Commands)
        {
            string key = "playerUpgrade" + command;
            int level = current.GetValueOrDefault(key, 0);
            int previous = Math.Min(Math.Max(0, level), applied.GetValueOrDefault(key, 0));
            int requested = config == null ? 0 : command switch
            {
                "Speed" => config.KingSpeedBonus.Value,
                "Range" => config.KingRangeBonus.Value,
                _ => config.KingStrengthBonus.Value
            };
            int desired = DesiredBonus(command, Math.Max(0, level - previous), requested);
            int delta = desired - previous;
            if (delta != 0)
            {
                // Read the local result even if sending to peers fails after local application.
                // A retry must never stack another copy of an already applied grant.
                UpgradeService.AddLevelsHostAuthoritative(steamId, command, delta);
                if (!UpgradeService.TryGetLevels(steamId, out var after)) continue;
                previous = Math.Clamp(previous + after.GetValueOrDefault(key, 0) - level, 0, 200);
            }
            applied[key] = previous;
        }
        bool supported = false;
        foreach (int amount in applied.Values) supported |= amount > 0;
        if (supported) Applied[steamId] = applied;
        else Applied.Remove(steamId);
        return supported;
    }

    internal static void Stop()
    {
        // Startup/shutdown cleanup with no grants must never initialize Photon.
        if (Applied.Count == 0) return;
        try
        {
            if (UpgradeService.Ready && SemiFunc.IsMasterClientOrSingleplayer())
                foreach (string steamId in new List<string>(Applied.Keys)) Reconcile(steamId, null);
        }
        catch (Exception error)
        { StageRolesPlugin.ModLogger.LogDebug($"King aura cleanup skipped: {error.Message}"); }
        finally { Applied.Clear(); }
    }
}
