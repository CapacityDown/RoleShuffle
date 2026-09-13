using System;
using System.Collections.Generic;
using System.Globalization;

namespace REPOJP.StageRoles;

// Consumed only by RoleHud, never by assignments, reports or network payloads.
internal static class RoleHudTestPreview
{
    private static IReadOnlyList<RoleSnapshot>? _players;
    private static object? _room;
    private static string _save = string.Empty, _localId = string.Empty;
    private static readonly string[] Names =
    {
        "日本語の長いプレイヤー名あいうえおかきくけこ",
        "简体中文测试玩家名字很长的情况",
        "繁體中文測試玩家名字很長的情況",
        "한국어긴플레이어이름테스트",
        "Игрок_Українська_é́"
    };

    internal static bool TrySet(bool enabled, string[] args, object? room, string save, string localId, out string response)
    {
        response = "Enable Testing.Enabled to use the HUD preview.";
        if (!enabled) { Clear(); return false; }
        response = $"Usage: /hudmultibyte [1-{StageRolesConfig.MaximumSupportedPlayers}|reset] (short: /hmb)";
        if (args.Length > 1) return false;
        if (args.Length == 1 && string.Equals(args[0], "reset", StringComparison.OrdinalIgnoreCase))
        {
            Clear(); response = "Multibyte HUD preview reset."; return true;
        }
        int count = 6;
        if (args.Length == 1 && (!int.TryParse(args[0], NumberStyles.None, CultureInfo.InvariantCulture, out count) ||
            count < 1 || count > StageRolesConfig.MaximumSupportedPlayers)) return false;

        List<RoleSnapshot> players = new(count);
        for (int index = 0; index < count; index++)
        {
            StageRole role = index % 2 == 0 ? StageRole.Tank : StageRole.Runner;
            players.Add(new RoleSnapshot(index == 0 && localId.Length > 0 ? localId : "hud-multibyte-" + index,
                index == 0 ? "YOU" : Names[(index - 1) % Names.Length] + (index > 5 ? " " + index : string.Empty), role, role));
        }
        _players = players.AsReadOnly(); _room = room; _save = save; _localId = localId;
        response = $"Local multibyte HUD preview: {count} players including YOU. View the stage HUD or HUD EDITOR; /hmb reset to finish.";
        return true;
    }

    internal static IReadOnlyList<RoleSnapshot>? Read(object? room, string save, string localId)
    {
        if (_players != null && (!ReferenceEquals(_room, room) || _save != save || _localId != localId)) Clear();
        return _players;
    }

    internal static void Clear() { _players = null; _room = null; _save = _localId = string.Empty; }
}
