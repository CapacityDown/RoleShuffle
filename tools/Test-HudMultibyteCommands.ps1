$ErrorActionPreference = 'Stop'
$source = Get-Content -LiteralPath (Join-Path $PSScriptRoot '../RoleTestCommandService.cs') -Raw
$methods = foreach ($name in 'TryRegisterHudMultibyte', 'ExecuteHudMultibyte', 'SuggestHudMultibyte', 'RegisterWithoutCommandNames', 'AddSuggestion') {
    $match = [regex]::Match($source, "(?ms)^    private (?:static )?[^\r\n]+\b$name\(.*?(?=^    (?:private|internal|public) )")
    if (-not $match.Success) { throw "Production method not found: $name" }
    $match.Value
}
$preview = Get-Content -LiteralPath (Join-Path $PSScriptRoot '../RoleHudTestPreview.cs') -Raw
$preview = [regex]::Replace($preview, '(?m)^using [^\r\n]+\r?\n', '').Replace('namespace REPOJP.StageRoles;', '')
$constants = foreach ($name in 'HudMultibyteCommand', 'HudMultibyteShortCommand') {
    $match = [regex]::Match($source, "(?m)^    private const string $name = [^\r\n]+")
    if (-not $match.Success) { throw "Production command constant not found: $name" }
    $match.Value
}
$harness = @'
#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Linq;
public enum StageRole { Tank, Runner }
public record RoleSnapshot(string SteamId, string PlayerName, StageRole Role, StageRole EffectiveRole);
public class Flag { public bool Value = true; }
public class StageRolesConfig { public const int MaximumSupportedPlayers = 30; public Flag SetRoleCommandEnabled = new(); }
public class StatsManager { public static StatsManager instance = new(); public string saveFileCurrent = "test-save"; }
public static class GameSaveState { public static string CurrentName => StatsManager.instance.saveFileCurrent; }
public static class PhotonNetwork { public static object CurrentRoom = new(); }
public static class SemiFunc { public static object PlayerGetLocal() => new(); }
public static class PlayerIdentity { public static string SteamId(object player) => "self"; }
public static class StageRolesPlugin { public static Logger ModLogger = new(); }
public class Logger { public void LogWarning(string text) { } }
public class DebugCommandHandler {
    public Dictionary<string, ChatCommand> Commands = new();
    public void Register(ChatCommand command) => Commands.Add(command.Name, command);
    public record ChatCommand(string Name, string Description, Action<bool,string[]> Execute,
        Func<bool,string,string[],List<string>> Suggest, Func<bool> Enabled, bool debugOnly);
}
public class HudCommandChecks {
    StageRolesConfig _config = new();
    static FieldInfo RegisteredCommandsField = typeof(DebugCommandHandler).GetField("Commands")!;
    static bool Success;
    static void Respond(string text, bool success) { Success = success; }
__CONSTANTS__
__METHODS__
    public static void Run() {
        int checks = 0;
        void Check(bool condition, string message) { if (!condition) throw new Exception(message); checks++; }
        var runtime = new HudCommandChecks();
        var handler = new DebugCommandHandler();
        foreach (string name in new[] { HudMultibyteCommand, HudMultibyteShortCommand }) {
            Check(runtime.TryRegisterHudMultibyte(handler, name), "Both aliases register");
            var command = handler.Commands[name];
            Check(!runtime.TryRegisterHudMultibyte(handler, name) && ReferenceEquals(command, handler.Commands[name]), "Registration never replaces another command");
            runtime._config.SetRoleCommandEnabled.Value = false;
            Check(!command.Enabled(), "Testing disabled hides/disables the command");
            command.Execute(false, Array.Empty<string>());
            Check(!Success && RoleHudTestPreview.Read(PhotonNetwork.CurrentRoom, "test-save", "self") == null, "Callback rechecks testing permission");
            runtime._config.SetRoleCommandEnabled.Value = true;
            Check(command.Enabled(), "Testing enables the command");
            command.Execute(false, Array.Empty<string>());
            Check(Success && RoleHudTestPreview.Read(PhotonNetwork.CurrentRoom, "test-save", "self")!.Count == 6, "No argument displays six players including self");
            command.Execute(true, new[] { "12" });
            Check(Success && RoleHudTestPreview.Read(PhotonNetwork.CurrentRoom, "test-save", "self")!.Count == 12, "Console and chat use the same count handler");
            command.Execute(false, new[] { "invalid" });
            Check(!Success && RoleHudTestPreview.Read(PhotonNetwork.CurrentRoom, "test-save", "self")!.Count == 12, "Invalid argument preserves active preview");
            Check(command.Suggest(false, "r", Array.Empty<string>()).SequenceEqual(new[] { "reset" }), "Reset autocomplete");
            command.Execute(false, new[] { "reset" });
            Check(Success && RoleHudTestPreview.Read(PhotonNetwork.CurrentRoom, "test-save", "self") == null, "Both reset aliases restore actual HUD data");
        }
        Check(handler.Commands.ContainsKey("hudmultibyte") && handler.Commands.ContainsKey("hmb"), "Documented command names match production constants");
        Console.WriteLine($"PASS: {checks} production HUD command registration and callback checks (game stand-ins).");
    }
}
'@
Add-Type -TypeDefinition ($harness.Replace('__CONSTANTS__', ($constants -join "`n")).Replace('__METHODS__', ($methods -join "`n")) + $preview)
[HudCommandChecks]::Run()
