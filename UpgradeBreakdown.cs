using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace REPOJP.StageRoles;
internal sealed class UpgradeBreakdownRow(string name, int basis, int retained, int target, int dynamic,
    int support, int penalty, int actual)
{
    internal string Name { get; } = name;
    internal int Base { get; } = basis;
    internal int Retained { get; } = retained;
    internal int Target { get; } = target;
    internal int Dynamic { get; } = dynamic;
    internal int Support { get; } = support;
    internal int Penalty { get; } = penalty;
    internal int Actual { get; } = actual;
    internal long Other => (long)Actual - ((long)Target + Dynamic + Support - Penalty);
}
// No account IDs or player names in the payload: the requester receives only their own levels.
internal sealed class UpgradeBreakdownSnapshot(StageRole? role, IReadOnlyList<UpgradeBreakdownRow> rows)
{
    internal StageRole? Role { get; } = role;
    internal IReadOnlyList<UpgradeBreakdownRow> Rows { get; } = rows;
}
internal static class UpgradeBreakdownCodec
{
    internal const int MaximumLength = 8192;
    internal static string Encode(UpgradeBreakdownSnapshot snapshot)
    {
        var text = new StringBuilder("1|" + (snapshot.Role.HasValue ? (int)snapshot.Role.Value : -1) + "|");
        foreach (var row in snapshot.Rows)
            text.Append(row.Name).Append(',').Append(row.Base).Append(',').Append(row.Retained).Append(',').Append(row.Target)
                .Append(',').Append(row.Dynamic).Append(',').Append(row.Support).Append(',').Append(row.Penalty).Append(',').Append(row.Actual).Append(';');
        return text.ToString();
    }
    internal static UpgradeBreakdownSnapshot? Decode(string? text)
    {
        if (string.IsNullOrEmpty(text) || text.Length > MaximumLength) return null;
        string[] fields = text.Split('|');
        if (fields.Length != 3 || fields[0] != "1" ||
            !int.TryParse(fields[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int role) ||
            (role != -1 && !Enum.IsDefined(typeof(StageRole), role))) return null;
        var rows = new List<UpgradeBreakdownRow>(); var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (string entry in fields[2].Split(';'))
        {
            if (entry.Length == 0) continue;
            string[] values = entry.Split(',');
            if (values.Length != 8 || !names.Add(values[0]) || values[0].Length == 0 || values[0].Length > 32 ||
                Array.Exists(values[0].ToCharArray(), c => c < 'A' || c > 'Z' && c < 'a' || c > 'z')) return null;
            int[] numbers = new int[7];
            for (int n = 0; n < 7; n++)
                if (!int.TryParse(values[n+1], NumberStyles.Integer, CultureInfo.InvariantCulture, out numbers[n]) ||
                    (n != 3 && numbers[n] < 0)) return null;
            rows.Add(new UpgradeBreakdownRow(values[0], numbers[0], numbers[1], numbers[2], numbers[3], numbers[4], numbers[5], numbers[6]));
            if (rows.Count > 16) return null;
        }
        return new UpgradeBreakdownSnapshot(role == -1 ? null : (StageRole)role, rows);
    }
}
