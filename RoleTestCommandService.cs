using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Photon.Pun;
using UnityEngine;

namespace REPOJP.StageRoles;

internal sealed class RoleTestCommandService : MonoBehaviour
{
    private const string FullCommand = "setrole";
    private const string ShortCommand = "sr";
    private const string HudPlayersCommand = "hudplayers";
    private const string HudPlayersShortCommand = "hp";
    private const string HudMultibyteCommand = "hudmultibyte";
    private const string HudMultibyteShortCommand = "hmb";
    private const string BaseSlotCommand = "baseslot";
    private const string BaseSlotShortCommand = "bs";
    private const string BaseUpgradeCommand = "baseupgrade";
    private const string BaseUpgradeShortCommand = "bu";
    private const string DrawHistoryCommand = "drawhistory";
    private const string DrawHistoryShortCommand = "dh";
    private static readonly FieldInfo? DebugConsoleInstanceField =
        AccessTools.Field(typeof(DebugConsoleUI), "instance");
    private static readonly FieldInfo? RegisteredCommandsField =
        AccessTools.Field(typeof(DebugCommandHandler), "_commands");
    private static readonly MethodInfo? SetResponseTextMethod =
        AccessTools.Method(
            typeof(DebugConsoleUI),
            "SetResponseText",
            new[] { typeof(string), typeof(Color), typeof(float) });

    private StageRolesConfig _config = null!;
    private StageRoleController _controller = null!;
    private DebugCommandHandler? _registeredHandler;

    internal void Initialize(
        StageRolesConfig config,
        StageRoleController controller)
    {
        _config = config;
        _controller = controller;
    }

    private void Update()
    {
        DebugCommandHandler? handler = DebugCommandHandler.instance;
        if (handler == null || ReferenceEquals(handler, _registeredHandler))
        {
            return;
        }

        _registeredHandler = handler;
        bool fullRegistered = TryRegister(handler, FullCommand);
        bool shortRegistered = TryRegister(handler, ShortCommand);
        bool hudPlayersRegistered = TryRegisterHudPlayers(
            handler,
            HudPlayersCommand);
        bool hudPlayersShortRegistered = TryRegisterHudPlayers(
            handler,
            HudPlayersShortCommand);
        bool baseSlotRegistered = TryRegisterBaseSlot(
            handler,
            BaseSlotCommand);
        bool baseSlotShortRegistered = TryRegisterBaseSlot(
            handler,
            BaseSlotShortCommand);
        bool baseUpgradeRegistered = TryRegisterBaseUpgrade(
            handler,
            BaseUpgradeCommand);
        bool baseUpgradeShortRegistered = TryRegisterBaseUpgrade(
            handler,
            BaseUpgradeShortCommand);
        bool drawHistoryRegistered = TryRegisterDrawHistory(handler, DrawHistoryCommand);
        bool drawHistoryShortRegistered = TryRegisterDrawHistory(handler, DrawHistoryShortCommand);
        bool hudMultibyteRegistered = TryRegisterHudMultibyte(handler, HudMultibyteCommand);
        bool hudMultibyteShortRegistered = TryRegisterHudMultibyte(handler, HudMultibyteShortCommand);
        if (fullRegistered || shortRegistered || hudPlayersRegistered ||
            hudPlayersShortRegistered || baseSlotRegistered ||
            baseSlotShortRegistered || baseUpgradeRegistered ||
            baseUpgradeShortRegistered || drawHistoryRegistered || drawHistoryShortRegistered ||
            hudMultibyteRegistered || hudMultibyteShortRegistered)
        {
            StageRolesPlugin.ModLogger.LogInfo(
                "Role testing command registration completed.");
        }
    }

    private bool TryRegisterHudMultibyte(DebugCommandHandler handler, string commandName)
    {
        try
        {
            return RegisterWithoutCommandNames(handler, new DebugCommandHandler.ChatCommand(
                commandName, "Previews multibyte player names locally in the HUD and HUD editor.",
                ExecuteHudMultibyte, SuggestHudMultibyte, () => _config.SetRoleCommandEnabled.Value, debugOnly: false));
        }
        catch (Exception exception)
        {
            StageRolesPlugin.ModLogger.LogWarning($"Role testing command registration failed ({exception.GetType().Name}).");
            return false;
        }
    }

    private void ExecuteHudMultibyte(bool isDebugConsole, string[] args)
    {
        bool success = RoleHudTestPreview.TrySet(_config.SetRoleCommandEnabled.Value, args,
            PhotonNetwork.CurrentRoom, GameSaveState.CurrentName,
            PlayerIdentity.SteamId(SemiFunc.PlayerGetLocal()), out string response);
        Respond(response, success);
    }

    private static List<string> SuggestHudMultibyte(bool isDebugConsole, string partial, string[] args)
    {
        List<string> suggestions = new();
        if (args.Length > 1) return suggestions;
        foreach (string value in new[] { "6", "12", "30", "reset" })
            AddSuggestion(suggestions, value, partial ?? string.Empty);
        return suggestions;
    }

    private bool TryRegisterDrawHistory(DebugCommandHandler handler, string commandName)
    {
        try
        {
            return RegisterWithoutCommandNames(handler, new DebugCommandHandler.ChatCommand(
                commandName,
                "Previews sample draw history locally for display testing.",
                ExecuteDrawHistory,
                SuggestDrawHistory,
                () => _config.SetRoleCommandEnabled.Value,
                debugOnly: false));
        }
        catch (Exception exception)
        {
            StageRolesPlugin.ModLogger.LogWarning(
                $"Role testing command registration failed ({exception.GetType().Name}).");
            return false;
        }
    }

    private static void ExecuteDrawHistory(bool isDebugConsole, string[] args)
    {
        bool success = BaseUpgradeHistory.TrySetDisplayPreview(args, out string response);
        Respond(response, success);
    }

    private static List<string> SuggestDrawHistory(bool isDebugConsole, string partial, string[] args)
    {
        List<string> suggestions = new();
        if (args.Length > 1) return suggestions;
        foreach (string value in new[] { "0", "1", "10", "50", "reset" })
            AddSuggestion(suggestions, value, partial ?? string.Empty);
        return suggestions;
    }

    private bool TryRegisterBaseUpgrade(
        DebugCommandHandler handler,
        string commandName)
    {
        try
        {
            return RegisterWithoutCommandNames(handler, new DebugCommandHandler.ChatCommand(
                commandName,
                "Changes a Base Upgrade in the truck for testing.",
                ExecuteBaseUpgrade,
                SuggestBaseUpgrade,
                () => _config.SetRoleCommandEnabled.Value,
                debugOnly: false));
        }
        catch (Exception exception)
        {
            StageRolesPlugin.ModLogger.LogWarning(
                $"Role testing command registration failed ({exception.GetType().Name}).");
            return false;
        }
    }

    private static void ExecuteBaseUpgrade(
        bool isDebugConsole,
        string[] args)
    {
        if (args.Length != 2)
        {
            Respond(
                "Usage: /baseupgrade <upgrade|all> <level|+delta|-delta>",
                success: false);
            return;
        }
        BaseUpgradeDrawRuntime? draw =
            StageRolesPlugin.Instance?.BaseUpgradeDraw;
        string response = "Base Upgrade control is unavailable.";
        bool success = draw != null && draw.TryChangeBaseUpgrade(
            args[0],
            args[1],
            out response);
        Respond(response, success);
    }

    private static List<string> SuggestBaseUpgrade(
        bool isDebugConsole,
        string partial,
        string[] args)
    {
        List<string> suggestions = new();
        if (args.Length > 1)
        {
            return suggestions;
        }
        string filter = partial ?? string.Empty;
        AddSuggestion(suggestions, "all", filter);
        StageRolesConfig? config = StageRolesPlugin.Instance?.Settings;
        if (config == null)
        {
            return suggestions;
        }
        foreach (UpgradeGrant upgrade in RoleCatalog.BaseUpgrades(config))
        {
            AddSuggestion(suggestions, upgrade.CommandName, filter);
        }
        return suggestions;
    }

    private bool TryRegisterBaseSlot(
        DebugCommandHandler handler,
        string commandName)
    {
        try
        {
            return RegisterWithoutCommandNames(handler, new DebugCommandHandler.ChatCommand(
                commandName,
                "Runs the Base Upgrade slot in the truck for testing.",
                ExecuteBaseSlot,
                null,
                () => _config.SetRoleCommandEnabled.Value,
                debugOnly: false));
        }
        catch (Exception exception)
        {
            StageRolesPlugin.ModLogger.LogWarning(
                $"Role testing command registration failed ({exception.GetType().Name}).");
            return false;
        }
    }

    private static void ExecuteBaseSlot(
        bool isDebugConsole,
        string[] args)
    {
        if (args.Length != 0)
        {
            Respond("Usage: /baseslot", success: false);
            return;
        }
        BaseUpgradeDrawRuntime? draw =
            StageRolesPlugin.Instance?.BaseUpgradeDraw;
        string response = "Base Upgrade slot is unavailable.";
        bool success = draw != null &&
            draw.TryRunTestDraw(out response);
        Respond(response, success);
    }

    private bool TryRegisterHudPlayers(
        DebugCommandHandler handler,
        string commandName)
    {
        try
        {
            return RegisterWithoutCommandNames(handler, new DebugCommandHandler.ChatCommand(
                commandName,
                "Overrides the local role HUD player count for testing.",
                ExecuteHudPlayers,
                SuggestHudPlayers,
                () => _config.SetRoleCommandEnabled.Value,
                debugOnly: false));
        }
        catch (Exception exception)
        {
            StageRolesPlugin.ModLogger.LogWarning(
                $"Role testing command registration failed ({exception.GetType().Name}).");
            return false;
        }
    }

    private static void ExecuteHudPlayers(bool isDebugConsole, string[] args)
    {
        if (args.Length != 1)
        {
            Respond(
                $"Usage: /hudplayers <1-{StageRolesConfig.MaximumSupportedPlayers}|reset>",
                success: false);
            return;
        }
        if (string.Equals(args[0], "reset", StringComparison.OrdinalIgnoreCase))
        {
            RoleAssignmentSync.ClearPreview();
            Respond("HUD player preview reset.", success: true);
            return;
        }
        if (!int.TryParse(args[0], out int count) || count < 1 ||
            count > StageRolesConfig.MaximumSupportedPlayers)
        {
            Respond(
                $"HUD player count must be from 1 to {StageRolesConfig.MaximumSupportedPlayers}.",
                success: false);
            return;
        }

        RoleAssignmentSync.SetPreviewPlayerCount(count);
        Respond($"Local HUD preview player count: {count}.", success: true);
    }

    private static List<string> SuggestHudPlayers(
        bool isDebugConsole,
        string partial,
        string[] args)
    {
        List<string> suggestions = new();
        if (args.Length > 1)
        {
            return suggestions;
        }
        string filter = partial ?? string.Empty;
        AddSuggestion(suggestions, "1", filter);
        AddSuggestion(suggestions, "8", filter);
        AddSuggestion(suggestions, "20", filter);
        AddSuggestion(suggestions, "30", filter);
        AddSuggestion(suggestions, "reset", filter);
        return suggestions;
    }

    private bool TryRegister(DebugCommandHandler handler, string commandName)
    {
        try
        {
            return RegisterWithoutCommandNames(handler, new DebugCommandHandler.ChatCommand(
                commandName,
                "Changes a player's stage role for testing.",
                Execute,
                Suggest,
                () => _config.SetRoleCommandEnabled.Value,
                debugOnly: false));
        }
        catch (Exception exception)
        {
            StageRolesPlugin.ModLogger.LogWarning(
                $"Role testing command registration failed ({exception.GetType().Name}).");
            return false;
        }
    }

    private static bool RegisterWithoutCommandNames(
        DebugCommandHandler handler,
        DebugCommandHandler.ChatCommand command)
    {
        // Vanilla Register logs the command name on a duplicate. Check first;
        // never replace another mod's command or globally suppress logging.
        if (RegisteredCommandsField?.GetValue(handler) is not IDictionary commands)
        {
            StageRolesPlugin.ModLogger.LogWarning(
                "Role testing command registration skipped: registry unavailable.");
            return false;
        }
        if (commands.Contains(command.Name))
        {
            StageRolesPlugin.ModLogger.LogWarning(
                "Role testing command registration skipped: an entry already exists.");
            return false;
        }
        handler.Register(command);
        return true;
    }

    private void Execute(bool isDebugConsole, string[] args)
    {
        if (args.Length == 0)
        {
            Respond(
                $"Usage: /setrole <role name|1-{RoleCatalog.AllRoles.Count - 1}|" +
                "random|rd> [player number|player name|Steam ID|all]",
                success: false);
            return;
        }

        if (string.Equals(args[0], "list", StringComparison.OrdinalIgnoreCase))
        {
            Respond(BuildRoleList(), success: true, duration: 8f);
            return;
        }

        bool allPlayers = args.Length == 2 &&
            string.Equals(args[1], "all", StringComparison.OrdinalIgnoreCase);
        string targetIdentifier = args.Length > 1
            ? string.Join(" ", args, 1, args.Length - 1)
            : string.Empty;
        if (IsRandomRoleArgument(args[0]))
        {
            bool randomSuccess = allPlayers
                ? _controller.TryRandomizeAllRoles(out string randomAllResponse)
                : _controller.TryRandomizeRole(
                    targetIdentifier,
                    out randomAllResponse);
            Respond(randomAllResponse, randomSuccess);
            return;
        }

        if (!TryParseRole(args[0], out StageRole role))
        {
            Respond(
                $"Unknown role: {args[0]}. Use /setrole list.",
                success: false);
            return;
        }

        bool success = allPlayers
            ? _controller.TrySetRoleForAll(role, out string response)
            : _controller.TrySetRole(targetIdentifier, role, out response);
        Respond(response, success);
    }

    private static List<string> Suggest(
        bool isDebugConsole,
        string partial,
        string[] args)
    {
        List<string> suggestions = new();
        string filter = partial ?? string.Empty;
        if (args.Length > 1)
        {
            AddSuggestion(suggestions, "all", filter);
            foreach (RoleSnapshot snapshot in RoleAssignmentSync.Read())
            {
                if (snapshot.PlayerNumber > 0)
                {
                    AddSuggestion(suggestions, snapshot.PlayerNumber.ToString(), filter);
                }
            }
            return suggestions;
        }

        AddSuggestion(suggestions, "list", filter);
        AddSuggestion(suggestions, "random", filter);
        AddSuggestion(suggestions, "rd", filter);
        for (int index = 0; index < RoleCatalog.AllRoles.Count; index++)
        {
            StageRole role = RoleCatalog.AllRoles[index];
            AddSuggestion(
                suggestions,
                RoleCatalog.AssignmentName(role),
                filter);
            AddSuggestion(
                suggestions,
                RoleCatalog.IsSecretRole(role)
                    ? ((int)role).ToString()
                    : (index + 1).ToString(),
                filter);
        }
        return suggestions;
    }

    private static bool IsRandomRoleArgument(string value) =>
        string.Equals(value, "random", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(value, "rd", StringComparison.OrdinalIgnoreCase);

    private static void AddSuggestion(
        ICollection<string> suggestions,
        string candidate,
        string filter)
    {
        if (candidate.StartsWith(filter, StringComparison.OrdinalIgnoreCase))
        {
            suggestions.Add(candidate);
        }
    }

    private static bool TryParseRole(string value, out StageRole role)
    {
        if (string.Equals(value, "???", StringComparison.Ordinal) ||
            string.Equals(value, "???1", StringComparison.Ordinal))
        {
            role = StageRole.Superbot;
            return true;
        }
        if (string.Equals(value, "???2", StringComparison.Ordinal))
        {
            role = StageRole.Disaster;
            return true;
        }
        if (int.TryParse(value, out int number) &&
            (number == (int)StageRole.Superbot || number == (int)StageRole.Disaster))
        {
            role = (StageRole)number;
            return true;
        }
        if (number >= 1 && number <= (int)StageRole.Brawler + 1)
        {
            role = RoleCatalog.AllRoles[number - 1];
            return true;
        }

        return Enum.TryParse(value, ignoreCase: true, out role) &&
               Enum.IsDefined(typeof(StageRole), role);
    }

    private static string BuildRoleList()
    {
        List<string> entries = new(RoleCatalog.AllRoles.Count);
        for (int index = 0; index < RoleCatalog.AllRoles.Count; index++)
        {
            StageRole role = RoleCatalog.AllRoles[index];
            int id = RoleCatalog.IsSecretRole(role) ? (int)role : index + 1;
            entries.Add($"{id}={RoleCatalog.AssignmentName(role)}");
        }
        return string.Join(", ", entries);
    }

    private static void Respond(
        string text,
        bool success,
        float duration = 4f)
    {
        if (success)
        {
            StageRolesPlugin.ModLogger.LogInfo(text);
        }
        else
        {
            StageRolesPlugin.ModLogger.LogWarning(text);
        }

        try
        {
            object? console = DebugConsoleInstanceField?.GetValue(null);
            if (console != null && SetResponseTextMethod != null)
            {
                SetResponseTextMethod.Invoke(
                    console,
                    new object[]
                    {
                        text,
                        success ? Color.green : Color.red,
                        duration
                    });
            }
        }
        catch (Exception exception)
        {
            StageRolesPlugin.ModLogger.LogDebug(
                $"Could not show command response: {exception.Message}");
        }
    }
}
