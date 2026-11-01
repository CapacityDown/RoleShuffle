using System;
using System.Collections.Generic;

namespace REPOJP.StageRoles;

internal sealed partial class StageRoleController
{
    private static readonly StageRole[] DrawContextRoles =
    {
        StageRole.Musician, StageRole.Rider, StageRole.Brawler, StageRole.Sniper,
        StageRole.DualWielder, StageRole.Rigger, StageRole.Engineer, StageRole.Electrician
    };

    // Read-only preview of a fresh whole-party draw with the current world and
    // personal history. No RNG, assignment, upgrade or history writes occur here.
    internal RoleDrawSnapshot BuildDrawEligibility(string source)
    {
        List<PlayerAvatar> players = CollectPlayers();
        Dictionary<string, PlayerAvatar> identified = new(StringComparer.Ordinal);
        foreach (PlayerAvatar player in players)
        {
            string id = PlayerIdentity.SteamId(player);
            if (!string.IsNullOrEmpty(id)) identified[id] = player;
        }
        int count = Math.Min(30, identified.Count);
        bool rosterReady = count > 0 && identified.Count == players.Count;
        bool playersReady = UpgradeService.Ready && rosterReady;
        bool solo = !SemiFunc.IsMultiplayer() || (rosterReady && count <= 1);
        bool known = IsPlayableStageContext() && SemiFunc.LevelGenDone();
        List<StageRole> context = new(RoleCatalog.AllRoles);
        bool checkedContext = true;
        if (known) checkedContext = RemoveUnavailableContextRoles(context);
        List<StageRole> pool = EnabledRoles();
        if (solo) RemoveSoloIncompatibleRoles(pool);
        if (known) pool.RemoveAll(role => !context.Contains(role));
        RemoveOrphanedSuperbot(pool);
        Dictionary<string, StageRole> previous;
        Dictionary<string, List<StageRole>> recent;
        Dictionary<string, int> hardship;
        int stage;
        if (_stageReady && _assignmentsInitialized)
            BuildRerollHistory(out previous, out recent, out hardship, out stage);
        else
        {
            previous = new(_previousRoles, StringComparer.Ordinal); recent = CopyRecentRoleHistory();
            hardship = new(_lastHardshipStages, StringComparer.Ordinal); stage = _completedAssignmentStages + 1;
        }
        RoleAssignmentPlanner planner = new(_config, previous, recent, hardship, stage);
        Dictionary<string, IReadOnlyDictionary<StageRole, DrawReason>> result = new(StringComparer.Ordinal);
        foreach (var member in identified)
        {
            if (result.Count >= 30) break;
            Dictionary<StageRole, DrawReason> rows = new();
            foreach (StageRole role in RoleCatalog.AllRoles)
            {
                // Do not disclose hidden capabilities or conditional rarity.
                if (RoleCatalog.IsSecretRole(role)) { rows[role] = DrawReason.Secret; continue; }
                DrawReason reason = DrawReason.None;
                if (!_config.Enabled.Value) reason |= DrawReason.AssignmentOff;
                if (!_config.RoleIsEnabled(role)) reason |= DrawReason.RoleOff;
                if (_config.RoleWeightValue(role) <= 0) reason |= DrawReason.WeightZero;
                if (solo && Array.IndexOf(SoloExcludedRoles, role) >= 0) reason |= DrawReason.Solo;
                if (known && !context.Contains(role)) reason |= DrawReason.MissingObject;
                if (_config.ExcludeUnavailableContextRoles.Value && Array.IndexOf(DrawContextRoles, role) >= 0)
                {
                    if (!known) reason |= DrawReason.MapUnknown;
                    else if (!checkedContext) reason |= DrawReason.ContextUnknown;
                }
                if (!playersReady) reason |= DrawReason.PlayerUnknown;
                if (playersReady && RoleCatalog.BaseUpgradeMeetsOrExceedsRoleTarget(role, _config, member.Key)) reason |= DrawReason.BaseTarget;
                if (playersReady && role == StageRole.Influencer && !RoleCatalog.InfluencerHasUpgradeBenefit(_config, count, member.Key)) reason |= DrawReason.NoUpgradeBenefit;
                if (recent.TryGetValue(member.Key, out List<StageRole>? history) && history.Contains(role)) reason |= DrawReason.RecentWeight;
                if (playersReady && (reason & RoleDrawEligibility.Hard) == 0)
                {
                    if (role == StageRole.Imitator)
                    {
                        bool possible = false;
                        foreach (var other in identified)
                        {
                            if (other.Key == member.Key) continue;
                            foreach (StageRole candidate in pool)
                                if (RoleCatalog.CanBeCopiedByImitator(candidate) &&
                                    planner.IsInitialCandidate(other.Key, candidate, pool, count, false))
                                { possible = true; break; }
                            if (possible) break;
                        }
                        reason |= possible ? DrawReason.Copy : DrawReason.NoCopyTarget;
                        if (planner.BlockedByHistory(member.Key, role)) reason |= DrawReason.History;
                    }
                    else if (role == StageRole.Twins)
                    {
                        int partners = 0;
                        foreach (var other in identified)
                            if (PlayerState.IsLiving(other.Value) && planner.IsInitialCandidate(other.Key, role, pool, count, true)) partners++;
                        reason |= PlayerState.IsLiving(member.Value) && planner.IsInitialCandidate(member.Key, role, pool, count, true) && partners >= 2
                            ? DrawReason.Pair : DrawReason.NoPartner;
                    }
                    else if (!planner.IsInitialCandidate(member.Key, role, pool, count, false)) reason |= DrawReason.OtherRule;
                    else if (!planner.IsInitialCandidate(member.Key, role, pool, count, true)) reason |= DrawReason.History;
                }
                rows[role] = reason;
            }
            result[member.Key] = rows;
        }
        return new(source, count, known, result);
    }
}
