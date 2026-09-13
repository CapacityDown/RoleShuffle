using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Photon.Pun;
using UnityEngine;

namespace REPOJP.StageRoles;

internal static class RoleExpressionPatches
{
    private static readonly FieldInfo? IsLocalField = AccessTools.Field(typeof(PlayerAvatar), "isLocal");
    private static readonly FieldInfo? CurrentMenuPageField = AccessTools.Field(typeof(MenuManager), "currentMenuPage");
    private static readonly ConditionalWeakTable<PlayerAvatar, Dictionary<int, float>> PendingInputs = new();

    // ToggleExpression applies a key press after a native 0.1-second delay.
    // Keep intent briefly so a quick tap works even after the key is released.
    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayerExpression), "ToggleExpression")]
    private static void TogglePrefix(PlayerExpression __instance, int _index)
    {
        RecordInput(__instance, _index);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayerExpression), "DoExpression")]
    private static void HoldPrefix(PlayerExpression __instance, int _index, bool _playerInput)
    {
        if (_playerInput)
        {
            RecordInput(__instance, _index);
        }
    }

    private static void RecordInput(PlayerExpression expression, int index)
    {
        PlayerAvatar player = expression.playerAvatar;
        if (player == null || IsLocalField?.GetValue(player) is not true ||
            !ReferenceEquals(expression, player.playerExpression) || MenuIsOpen())
        {
            return;
        }

        PendingInputs.GetOrCreateValue(player)[index] = Time.unscaledTime + 0.5f;
    }

    private static bool MenuIsOpen() =>
        MenuManager.instance != null &&
        CurrentMenuPageField?.GetValue(MenuManager.instance) is MenuPage page && page != null;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerAvatar), nameof(PlayerAvatar.PlayerExpressionSetRPC))]
    private static void SetPostfix(PlayerAvatar __instance, int _expressionIndex, float _percent, PhotonMessageInfo _info)
    {
        if (!SemiFunc.OwnerOnlyRPC(_info, __instance.photonView))
        {
            return;
        }

        bool hasInput = false;
        if (PendingInputs.TryGetValue(__instance, out Dictionary<int, float>? inputs))
        {
            hasInput = inputs.TryGetValue(_expressionIndex, out float expiresAt) && Time.unscaledTime <= expiresAt;
            inputs.Remove(_expressionIndex);
        }

        // Stop/reset notifications and local menu restoration are not ability inputs.
        // Vanilla peers have no input-intent signal, so their positive owner RPC
        // remains the host-only trigger; never gate peers on the host's menu.
        if (_percent <= 0f || (IsLocalField?.GetValue(__instance) is true && (!hasInput || MenuIsOpen())))
        {
            return;
        }

        StageRolesPlugin.Instance?.Controller?.TryHandleRoleExpression(__instance, _expressionIndex);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerAvatar), nameof(PlayerAvatar.PlayerExpressionStopRPC))]
    private static void StopPostfix(PlayerAvatar __instance, int _expressionIndex, PhotonMessageInfo _info)
    {
        if (SemiFunc.OwnerOnlyRPC(_info, __instance.photonView) &&
            PendingInputs.TryGetValue(__instance, out Dictionary<int, float>? inputs))
        {
            inputs.Remove(_expressionIndex);
        }
    }
}
