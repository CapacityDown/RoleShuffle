using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace REPOJP.StageRoles;

[HarmonyPatch(typeof(ItemUpgrade), "PlayerUpgrade")]
internal static class UpgradeItemRetentionPatch
{
    private static readonly FieldInfo? Done = AccessTools.Field(typeof(ItemUpgrade), "upgradeDone");
    private static readonly FieldInfo? PlayerUpgrade = AccessTools.Field(typeof(ItemUpgrade), "isPlayerUpgrade");
    private static readonly FieldInfo? ToggleState = AccessTools.Field(typeof(ItemToggle), "toggleState");
    private static readonly FieldInfo? PlayerId = AccessTools.Field(typeof(ItemToggle), "playerTogglePhotonID");

    internal sealed class Consumption
    {
        internal StatsManager Stats = null!;
        internal Dictionary<string, int> Run = null!;
        internal string Save = string.Empty;
        internal string SteamId = string.Empty;
        internal bool Shared;
        internal Dictionary<string, int> Before = null!;
    }

    [HarmonyPrefix]
    internal static void Prefix(ItemUpgrade __instance, out Consumption? __state)
    {
        __state = null;
        try
        {
            StageRolesConfig? config = StageRolesPlugin.Instance?.Settings;
            if (config?.Enabled.Value != true || !config.KeepUpgradeItems.Value ||
                !SemiFunc.IsMasterClientOrSingleplayer() || StatsManager.instance == null ||
                Done?.GetValue(__instance) is not false || PlayerUpgrade?.GetValue(__instance) is not true) return;
            ItemToggle? toggle = __instance.GetComponent<ItemToggle>();
            if (toggle == null || ToggleState?.GetValue(toggle) is not true ||
                PlayerId?.GetValue(toggle) is not int id) return;
            PlayerAvatar? player = SemiFunc.PlayerAvatarGetFromPhotonID(id);
            if (player == null) return;
            string steamId = PlayerIdentity.SteamId(player);
            if (!UpgradeService.TryGetLevels(steamId, out var before)) return;
            __state = new Consumption
            {
                Stats = StatsManager.instance,
                Run = StatsManager.instance.runStats,
                Save = GameSaveState.CurrentName,
                SteamId = steamId,
                Shared = config.UpgradeItemScope.Value == UpgradeItemScope.AllPlayers,
                Before = new Dictionary<string, int>(before, StringComparer.Ordinal)
            };
        }
        catch (Exception error)
        {
            StageRolesPlugin.ModLogger.LogWarning($"Could not observe upgrade item use: {error.Message}");
        }
    }

    [HarmonyPostfix]
    internal static void Postfix(ItemUpgrade __instance, Consumption? __state)
    {
        if (__state == null) return;
        try
        {
            if (!SemiFunc.IsMasterClientOrSingleplayer() || Done?.GetValue(__instance) is not true ||
                !ReferenceEquals(StatsManager.instance, __state.Stats) ||
                !ReferenceEquals(StatsManager.instance.runStats, __state.Run) ||
                GameSaveState.CurrentName != __state.Save ||
                !UpgradeService.TryGetLevels(__state.SteamId, out var after)) return;
            var changes = UpgradeItemRetention.Record(__state.SteamId, __state.Shared, __state.Before, after);
            if (changes.Count == 0) return;
            if (__state.Shared)
                foreach (UpgradeGrant change in changes)
                    if (change.CommandName == "Throw")
                        UpgradeItemRetention.MarkSharedThrowApplied(__state.SteamId, change.Level);
            // The recipient already received the native item effect. Only send
            // the shared increment to the other players; absent players read
            // the saved shared amount when their base/role targets are applied.
            HashSet<string> recipients = new(StringComparer.Ordinal) { __state.SteamId };
            StageRolesPlugin.Instance.Controller?.RetainedUpgradeApplied(__state.SteamId, changes);
            if (__state.Shared && GameDirector.instance != null)
                foreach (PlayerAvatar player in GameDirector.instance.PlayerList)
                {
                    if (player == null) continue;
                    string steamId = PlayerIdentity.SteamId(player);
                    if (string.IsNullOrEmpty(steamId) || !recipients.Add(steamId)) continue;
                    List<UpgradeGrant> applied = new();
                    foreach (UpgradeGrant change in changes)
                        if (change.CommandName != "Throw" &&
                            UpgradeService.AddLevelsHostAuthoritative(steamId, change.CommandName, change.Level))
                            applied.Add(change);
                    ApplyPendingThrow(steamId);
                    StageRolesPlugin.Instance.Controller?.RetainedUpgradeApplied(steamId, applied);
                }
            ApplyPendingThrow(__state.SteamId);
            // Save after the native item inventory/destruction bookkeeping.
            // A failed disk write must not discard already-consumed amounts;
            // they remain in runStats for the next successful native save.
            if (GameSaveState.CanSave) __state.Stats.SaveFileSave();
        }
        catch (Exception error)
        {
            StageRolesPlugin.ModLogger.LogWarning($"Could not finish retained upgrade item processing: {error.Message}");
        }
    }

    // Throw is not reset by RoleShuffle. Record delivery of the shared amount
    // separately and only apply missing increments, including to later joiners.
    internal static void ApplyPendingThrow(string steamId)
    {
        if (StageRolesPlugin.Instance?.Settings.KeepUpgradeItems.Value != true ||
            !SemiFunc.IsMasterClientOrSingleplayer()) return;
        int pending = UpgradeItemRetention.PendingSharedThrow(steamId);
        if (pending > 0 && UpgradeService.AddLevelsHostAuthoritative(steamId, "Throw", pending))
            UpgradeItemRetention.MarkSharedThrowApplied(steamId, pending);
    }
}
