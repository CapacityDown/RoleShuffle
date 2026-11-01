using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace REPOJP.StageRoles;

[Flags]
internal enum DrawReason
{
    None = 0, AssignmentOff = 1, RoleOff = 2, WeightZero = 4, Solo = 8,
    BaseTarget = 16, NoUpgradeBenefit = 32, MissingObject = 64, NoPartner = 128,
    NoCopyTarget = 256, History = 512, Pair = 1024, Copy = 2048,
    MapUnknown = 4096, PlayerUnknown = 8192, ContextUnknown = 16384,
    Secret = 32768, OtherRule = 65536, RecentWeight = 131072
}
internal enum DrawStatus { Eligible, Conditional, Excluded, Unknown }
internal static class RoleDrawEligibility
{
    internal const DrawReason Hard = DrawReason.AssignmentOff | DrawReason.RoleOff | DrawReason.WeightZero |
        DrawReason.Solo | DrawReason.BaseTarget | DrawReason.NoUpgradeBenefit | DrawReason.MissingObject |
        DrawReason.NoPartner | DrawReason.NoCopyTarget | DrawReason.OtherRule;
    internal const DrawReason Pending = DrawReason.MapUnknown | DrawReason.PlayerUnknown | DrawReason.ContextUnknown | DrawReason.Secret;
    internal static DrawStatus Status(DrawReason reasons) => (reasons & Hard) != 0 ? DrawStatus.Excluded :
        (reasons & Pending) != 0 ? DrawStatus.Unknown :
        (reasons & (DrawReason.History | DrawReason.Pair | DrawReason.Copy)) != 0 ? DrawStatus.Conditional : DrawStatus.Eligible;
    internal static string Label(DrawReason reasons, RoleGuideLanguage language) => RoleText.Get(Status(reasons) switch
    {
        DrawStatus.Eligible => "Eligible",
        DrawStatus.Conditional => "Conditional",
        DrawStatus.Excluded => "Excluded",
        _ => "Not yet known"
    }, language);
    internal static string Describe(DrawReason reasons, RoleGuideLanguage language)
    {
        List<string> parts = new();
        foreach (DrawReason reason in Enum.GetValues(typeof(DrawReason)))
        {
            if (reason == DrawReason.None || (reasons & reason) == 0) continue;
            string text = reason switch
            {
                DrawReason.AssignmentOff => "Role assignment is disabled in MOD settings.",
                DrawReason.RoleOff => "Role is switched OFF.",
                DrawReason.WeightZero => "Weight 0",
                DrawReason.Solo => "Requires at least two players.",
                DrawReason.BaseTarget => "Your Base Upgrades already reach this role's target.",
                DrawReason.NoUpgradeBenefit => "This party size cannot improve your Base Upgrades.",
                DrawReason.MissingObject => "Required items are absent from this stage.",
                DrawReason.NoPartner => "Twins needs two eligible living players.",
                DrawReason.NoCopyTarget => "No teammate can receive a copyable role.",
                DrawReason.History => "Your role history blocks this choice unless the draw must relax its limits.",
                DrawReason.Pair => "Selected as a pair with another eligible living player.",
                DrawReason.Copy => "A teammate must receive a copyable role first.",
                DrawReason.MapUnknown => "Required items will be checked after stage generation.",
                DrawReason.PlayerUnknown => "Waiting for your player information.",
                DrawReason.ContextUnknown => "Stage conditions could not be checked yet.",
                DrawReason.Secret => "Secret role conditions are hidden.",
                DrawReason.RecentWeight => "Recent role history halves your selection weight.",
                _ => "Current draw rules exclude this role."
            };
            parts.Add(RoleText.Get(text, language));
        }
        return string.Join(" ", parts);
    }
}
internal sealed class RoleDrawSnapshot(string source, int playerCount, bool stageKnown,
    IReadOnlyDictionary<string, IReadOnlyDictionary<StageRole, DrawReason>> players)
{
    internal string Source { get; } = source;
    internal int PlayerCount { get; } = playerCount;
    internal bool StageKnown { get; } = stageKnown;
    internal IReadOnlyDictionary<string, IReadOnlyDictionary<StageRole, DrawReason>> Players { get; } = players;
}
internal static class RoleDrawCodec
{
    internal const int MaxLength = 24000;
    internal static string Encode(RoleDrawSnapshot snapshot)
    {
        List<string> lines = new() { B64(snapshot.Source) + "|" + snapshot.PlayerCount.ToString(CultureInfo.InvariantCulture) + "|" + (snapshot.StageKnown ? "1" : "0") };
        foreach (var player in snapshot.Players)
        {
            List<string> values = new();
            foreach (var pair in player.Value)
                values.Add(RoleWireId.Encode(pair.Key).ToString(CultureInfo.InvariantCulture) + ":" + ((int)pair.Value).ToString(CultureInfo.InvariantCulture));
            lines.Add(B64(player.Key) + "|" + string.Join(",", values));
        }
        string result = string.Join("\n", lines);
        if (result.Length > MaxLength) throw new ArgumentException("Draw preview exceeds its payload bound.");
        return result;
    }
    internal static RoleDrawSnapshot? Decode(string? payload)
    {
        if (payload == null || payload.Length > MaxLength) return null;
        try
        {
            string[] lines = payload.Split('\n'); if (lines.Length < 1 || lines.Length > 31) return null;
            string[] header = lines[0].Split('|');
            if (header.Length != 3 || !Number(header[1], out int count) || count < 0 || count > 30 || (header[2] != "0" && header[2] != "1")) return null;
            string source = Un64(header[0], 256);
            Dictionary<string, IReadOnlyDictionary<StageRole, DrawReason>> players = new(StringComparer.Ordinal);
            for (int i = 1; i < lines.Length; i++)
            {
                string[] fields = lines[i].Split('|'); if (fields.Length != 2) return null;
                string id = Un64(fields[0], 128); if (string.IsNullOrWhiteSpace(id) || players.ContainsKey(id)) return null;
                Dictionary<StageRole, DrawReason> roles = new();
                string[] entries = fields[1].Split(','); if (entries.Length > 64) return null;
                foreach (string entry in entries)
                {
                    string[] pair = entry.Split(':');
                    if (pair.Length != 2 || !Number(pair[0], out int code) || !Number(pair[1], out int flags) || flags < 0 || flags > 262143) return null;
                    StageRole role = RoleWireId.Decode(code);
                    if (!Enum.IsDefined(typeof(StageRole), role) || roles.ContainsKey(role)) return null;
                    roles[role] = (DrawReason)flags;
                }
                players[id] = roles;
            }
            return new(source, count, header[2] == "1", players);
        }
        catch (FormatException) { return null; }
        catch (DecoderFallbackException) { return null; }
    }
    private static bool Number(string s, out int n) => int.TryParse(s, NumberStyles.None, CultureInfo.InvariantCulture, out n);
    private static string B64(string s) => Convert.ToBase64String(Encoding.UTF8.GetBytes(s));
    private static string Un64(string s, int max)
    {
        if (s.Length > max * 4) throw new FormatException();
        string text = new UTF8Encoding(false, true).GetString(Convert.FromBase64String(s));
        if (text.Length > max) throw new FormatException(); return text;
    }
}
