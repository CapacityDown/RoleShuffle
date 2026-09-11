using System;
using System.Collections.Generic;
using UnityEngine;

namespace REPOJP.StageRoles;

internal sealed class RoleAssignmentPlanner
{
    private const int SuperbotRelativeWeightDivisor = 1000;
    private const int StandardRoleWeight = 100;
    private const int RecentRoleWeightDivisor = 2;
    private static readonly HashSet<StageRole> ShowcaseRoles = new()
    {
        StageRole.Bomber,
        StageRole.Stinker,
        StageRole.Mage,
        StageRole.Gambler,
        StageRole.Trickster,
        StageRole.King,
        StageRole.Rider,
        StageRole.Influencer,
        StageRole.Diver
    };

    private static readonly HashSet<StageRole> SupportRoles = new()
    {
        StageRole.Medic,
        StageRole.Rescuer,
        StageRole.Mechanic,
        StageRole.Electrician,
        StageRole.Warden,
        StageRole.Bodyguard
    };

    private static readonly HashSet<StageRole> DangerRoles = new()
    {
        StageRole.Bomber,
        StageRole.Stinker,
        StageRole.Werewolf,
        StageRole.Disaster
    };

    private static readonly HashSet<StageRole> HardshipRoles = new()
    {
        StageRole.Jobless,
        StageRole.Tuna,
        StageRole.Disaster
    };

    private readonly StageRolesConfig _config;
    private readonly IReadOnlyDictionary<string, StageRole> _previousRoles;
    private readonly IReadOnlyDictionary<string, List<StageRole>>
        _recentRolesByPlayer;
    private readonly IReadOnlyDictionary<string, int> _lastHardshipStages;
    private readonly int _stageNumber;

    internal RoleAssignmentPlanner(
        StageRolesConfig config,
        IReadOnlyDictionary<string, StageRole> previousRoles,
        IReadOnlyDictionary<string, List<StageRole>> recentRolesByPlayer,
        IReadOnlyDictionary<string, int> lastHardshipStages,
        int stageNumber)
    {
        _config = config;
        _previousRoles = previousRoles;
        _recentRolesByPlayer = recentRolesByPlayer;
        _lastHardshipStages = lastHardshipStages;
        _stageNumber = Math.Max(1, stageNumber);
    }

    internal Dictionary<string, StageRole> PlanInitialAssignments(
        IReadOnlyList<PlayerAvatar> players,
        IReadOnlyList<StageRole> enabledRoles)
    {
        Dictionary<string, StageRole> result = new(StringComparer.Ordinal);
        List<PlayerSlot> remaining = new();
        foreach (PlayerAvatar player in players)
        {
            string steamId = PlayerIdentity.SteamId(player);
            if (!string.IsNullOrEmpty(steamId))
            {
                remaining.Add(new PlayerSlot(steamId, player));
            }
        }
        Shuffle(remaining);

        List<StageRole> selectedRoles = new();
        int playerCount = remaining.Count;
        AssignGuarantee(
            ShowcaseRoles,
            _config.ShowcaseMinimumForPlayerCount(playerCount),
            remaining,
            enabledRoles,
            selectedRoles,
            result,
            playerCount,
            "Showcase");
        AssignGuarantee(
            SupportRoles,
            _config.SupportMinimumForPlayerCount(playerCount),
            remaining,
            enabledRoles,
            selectedRoles,
            result,
            playerCount,
            "Support");

        Shuffle(remaining);
        foreach (PlayerSlot slot in remaining)
        {
            StageRole? role = PickForPlayer(
                slot.SteamId,
                enabledRoles,
                selectedRoles,
                playerCount,
                requiredCategory: null,
                allowRiskLimitRelaxation: true);
            if (role == null)
            {
                continue;
            }
            result[slot.SteamId] = role.Value;
            selectedRoles.Add(role.Value);
        }
        return result;
    }

    internal StageRole? PlanJoinedAssignment(
        string steamId,
        IReadOnlyList<StageRole> enabledRoles,
        IReadOnlyList<StageRole> reservedRoles,
        int connectedPlayerCount)
    {
        List<StageRole> selectedRoles = new(reservedRoles);
        HashSet<StageRole>? requiredCategory = null;
        if (CountCategory(selectedRoles, ShowcaseRoles) <
            _config.ShowcaseMinimumForPlayerCount(connectedPlayerCount))
        {
            requiredCategory = ShowcaseRoles;
        }
        else if (CountCategory(selectedRoles, SupportRoles) <
                 _config.SupportMinimumForPlayerCount(connectedPlayerCount))
        {
            requiredCategory = SupportRoles;
        }

        if (requiredCategory != null)
        {
            List<StageRole> guaranteedCandidates = Candidates(
                steamId,
                enabledRoles,
                selectedRoles,
                connectedPlayerCount,
                applyHistory: true,
                enforceUnique: true,
                enforceRiskLimits: true,
                requiredCategory);
            if (guaranteedCandidates.Count > 0)
            {
                return PickWeightedRole(steamId, guaranteedCandidates);
            }
        }
        return PickForPlayer(
            steamId,
            enabledRoles,
            selectedRoles,
            connectedPlayerCount,
            requiredCategory: null,
            allowRiskLimitRelaxation: true);
    }

    private void AssignGuarantee(
        HashSet<StageRole> category,
        int minimum,
        List<PlayerSlot> remaining,
        IReadOnlyList<StageRole> enabledRoles,
        List<StageRole> selectedRoles,
        IDictionary<string, StageRole> result,
        int playerCount,
        string categoryName)
    {
        int assigned = CountCategory(selectedRoles, category);
        while (assigned < minimum && remaining.Count > 0)
        {
            List<GuaranteeOption> options = new();
            foreach (PlayerSlot slot in remaining)
            {
                List<StageRole> candidates = Candidates(
                    slot.SteamId,
                    enabledRoles,
                    selectedRoles,
                    playerCount,
                    applyHistory: true,
                    enforceUnique: true,
                    enforceRiskLimits: true,
                    requiredCategory: category);
                if (candidates.Count > 0)
                {
                    options.Add(new GuaranteeOption(slot, candidates));
                }
            }
            if (options.Count == 0)
            {
                StageRolesPlugin.ModLogger.LogDebug(
                    $"Could not satisfy the configured {categoryName} role minimum without relaxing role history or risk limits.");
                return;
            }

            GuaranteeOption option = options[UnityEngine.Random.Range(0, options.Count)];
            StageRole role = PickWeightedRole(
                option.Slot.SteamId,
                option.Candidates);
            result[option.Slot.SteamId] = role;
            selectedRoles.Add(role);
            remaining.Remove(option.Slot);
            assigned++;
        }
    }

    private StageRole? PickForPlayer(
        string steamId,
        IReadOnlyList<StageRole> enabledRoles,
        IReadOnlyList<StageRole> selectedRoles,
        int playerCount,
        HashSet<StageRole>? requiredCategory,
        bool allowRiskLimitRelaxation)
    {
        List<StageRole> candidates = Candidates(
            steamId,
            enabledRoles,
            selectedRoles,
            playerCount,
            applyHistory: true,
            enforceUnique: true,
            enforceRiskLimits: true,
            requiredCategory);
        RemoveSuperbotIfNoStandardCandidate(candidates);
        if (candidates.Count == 0)
        {
            candidates = Candidates(
                steamId,
                enabledRoles,
                selectedRoles,
                playerCount,
                applyHistory: false,
                enforceUnique: true,
                enforceRiskLimits: true,
                requiredCategory);
            candidates.RemoveAll(RoleCatalog.IsSecretRole);
        }
        if (candidates.Count == 0)
        {
            candidates = Candidates(
                steamId,
                enabledRoles,
                selectedRoles,
                playerCount,
                applyHistory: false,
                enforceUnique: false,
                enforceRiskLimits: true,
                requiredCategory);
            candidates.RemoveAll(RoleCatalog.IsSecretRole);
        }
        if (candidates.Count == 0 && allowRiskLimitRelaxation)
        {
            candidates = Candidates(
                steamId,
                enabledRoles,
                selectedRoles,
                playerCount,
                applyHistory: false,
                enforceUnique: false,
                enforceRiskLimits: false,
                requiredCategory);
            candidates.RemoveAll(RoleCatalog.IsSecretRole);
        }
        return candidates.Count > 0
            ? PickWeightedRole(steamId, candidates)
            : null;
    }

    private static void RemoveSuperbotIfNoStandardCandidate(
        List<StageRole> candidates)
    {
        if (!candidates.Exists(role => !RoleCatalog.IsSecretRole(role)))
        {
            candidates.Clear();
        }
    }

    private List<StageRole> Candidates(
        string steamId,
        IReadOnlyList<StageRole> enabledRoles,
        IReadOnlyList<StageRole> selectedRoles,
        int playerCount,
        bool applyHistory,
        bool enforceUnique,
        bool enforceRiskLimits,
        HashSet<StageRole>? requiredCategory)
    {
        List<StageRole> result = new();
        foreach (StageRole role in enabledRoles)
        {
            if (role == StageRole.Disaster && playerCount <= 1)
            {
                continue;
            }
            if (requiredCategory != null && !requiredCategory.Contains(role))
            {
                continue;
            }
            if (RoleCatalog.BaseUpgradeMeetsOrExceedsRoleTarget(role, _config))
            {
                continue;
            }
            if (role == StageRole.Influencer &&
                !RoleCatalog.InfluencerHasUpgradeBenefit(_config, playerCount))
            {
                continue;
            }
            if (role == StageRole.Imitator &&
                !ContainsImitatorCopyTarget(selectedRoles))
            {
                continue;
            }
            if (RoleCatalog.HasCapability(role, StageRole.King) &&
                ContainsCapability(selectedRoles, StageRole.King))
            {
                continue;
            }
            if (enforceUnique && _config.UniqueRoles.Value &&
                Contains(selectedRoles, role))
            {
                continue;
            }
            if (applyHistory && IsBlockedByHistory(steamId, role))
            {
                continue;
            }
            if (enforceRiskLimits && !WithinRiskLimits(
                    role,
                    selectedRoles,
                    playerCount))
            {
                continue;
            }
            result.Add(role);
        }
        return result;
    }

    private bool IsBlockedByHistory(string steamId, StageRole role)
    {
        if (_config.PreventConsecutiveSameRole.Value &&
            _previousRoles.TryGetValue(steamId, out StageRole previousRole) &&
            previousRole == role)
        {
            return true;
        }

        int cooldown = Mathf.Clamp(
            _config.HardshipPersonalCooldownStages.Value,
            0,
            10);
        return cooldown > 0 && HardshipRoles.Contains(role) &&
               _lastHardshipStages.TryGetValue(steamId, out int lastStage) &&
               _stageNumber - lastStage <= cooldown;
    }

    private bool WithinRiskLimits(
        StageRole role,
        IReadOnlyList<StageRole> selectedRoles,
        int playerCount)
    {
        if (!_config.RiskCombinationLimitsEnabled.Value)
        {
            return true;
        }

        bool danger = DangerRoles.Contains(role);
        bool hardship = HardshipRoles.Contains(role);
        if (!danger && !hardship)
        {
            return true;
        }
        if (danger && CountCategory(selectedRoles, DangerRoles) >=
            Mathf.Clamp(
                _config.DangerRoleLimitForPlayerCount(playerCount),
                1,
                StageRolesConfig.MaximumSupportedPlayers))
        {
            return false;
        }
        if (hardship && CountCategory(selectedRoles, HardshipRoles) >=
            Mathf.Clamp(
                _config.HardshipRoleLimitForPlayerCount(playerCount),
                1,
                StageRolesConfig.MaximumSupportedPlayers))
        {
            return false;
        }

        int smallPartyMaximum = Mathf.Clamp(
            _config.SmallPartyCombinedRiskPlayerCount.Value,
            1,
            StageRolesConfig.MaximumSupportedPlayers);
        if (playerCount <= smallPartyMaximum &&
            CountRiskRoles(selectedRoles) >= Mathf.Clamp(
                _config.SmallPartyCombinedRiskRoleLimit.Value,
                1,
                StageRolesConfig.MaximumSupportedPlayers))
        {
            return false;
        }
        return true;
    }

    private StageRole PickWeightedRole(
        string steamId,
        IReadOnlyList<StageRole> roles)
    {
        int totalWeight = 0;
        foreach (StageRole role in roles)
        {
            totalWeight += SelectionWeight(steamId, role);
        }
        int roll = UnityEngine.Random.Range(0, Mathf.Max(1, totalWeight));
        foreach (StageRole role in roles)
        {
            roll -= SelectionWeight(steamId, role);
            if (roll < 0)
            {
                return role;
            }
        }
        return roles[roles.Count - 1];
    }

    private int SelectionWeight(string steamId, StageRole role)
    {
        int weight = RoleCatalog.IsSecretRole(role)
            ? StandardRoleWeight
            : Mathf.Max(0, _config.RoleWeightValue(role)) *
              SuperbotRelativeWeightDivisor;
        if (_recentRolesByPlayer.TryGetValue(
                steamId,
                out List<StageRole> recentRoles) &&
            recentRoles.Contains(role))
        {
            weight /= RecentRoleWeightDivisor;
        }
        return Mathf.Max(1, weight);
    }

    private static int CountCategory(
        IReadOnlyList<StageRole> selectedRoles,
        HashSet<StageRole> category)
    {
        int count = 0;
        foreach (StageRole role in selectedRoles)
        {
            if (category.Contains(role))
            {
                count++;
            }
        }
        return count;
    }

    private static int CountRiskRoles(IReadOnlyList<StageRole> selectedRoles)
    {
        int count = 0;
        foreach (StageRole role in selectedRoles)
        {
            if (DangerRoles.Contains(role) || HardshipRoles.Contains(role))
            {
                count++;
            }
        }
        return count;
    }

    private static bool Contains(
        IReadOnlyList<StageRole> roles,
        StageRole role)
    {
        foreach (StageRole current in roles)
        {
            if (current == role)
            {
                return true;
            }
        }
        return false;
    }

    private static bool ContainsCapability(
        IReadOnlyList<StageRole> roles,
        StageRole capability)
    {
        foreach (StageRole role in roles)
        {
            if (RoleCatalog.HasCapability(role, capability))
            {
                return true;
            }
        }
        return false;
    }

    private static bool ContainsImitatorCopyTarget(
        IReadOnlyList<StageRole> roles)
    {
        foreach (StageRole role in roles)
        {
            if (RoleCatalog.CanBeCopiedByImitator(role))
            {
                return true;
            }
        }
        return false;
    }

    private static void Shuffle<T>(IList<T> values)
    {
        for (int index = values.Count - 1; index > 0; index--)
        {
            int swapIndex = UnityEngine.Random.Range(0, index + 1);
            (values[index], values[swapIndex]) =
                (values[swapIndex], values[index]);
        }
    }

    private sealed class PlayerSlot
    {
        internal PlayerSlot(string steamId, PlayerAvatar player)
        {
            SteamId = steamId;
            Player = player;
        }

        internal string SteamId { get; }
        internal PlayerAvatar Player { get; }
    }

    private sealed class GuaranteeOption
    {
        internal GuaranteeOption(
            PlayerSlot slot,
            List<StageRole> candidates)
        {
            Slot = slot;
            Candidates = candidates;
        }

        internal PlayerSlot Slot { get; }
        internal List<StageRole> Candidates { get; }
    }
}
