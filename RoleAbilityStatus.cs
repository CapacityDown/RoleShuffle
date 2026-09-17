using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace REPOJP.StageRoles;

internal enum AbilityMetric { Medic, Rescuer, Phoenix, MageRecovery, MageCooldown, Repair, Charge, King, Contracts, Grace, Carry, CloudDistance, DecoyActive, DecoyCooldown, DiveActive, DiveCooldown, Wager, Avenger, GrenadeDistance, RoyalSupport }

internal readonly struct AbilityValue(AbilityMetric metric, int remaining, int limit)
{
    internal AbilityMetric Metric { get; } = metric;
    internal int Remaining { get; } = Math.Clamp(remaining, 0, 10000);
    internal int Limit { get; } = Math.Clamp(limit, 0, 10000);
}

internal sealed class AbilitySnapshot(string steamId, StageRole assigned, StageRole effective, IReadOnlyList<AbilityValue> values)
{
    internal string SteamId { get; } = steamId;
    internal StageRole Assigned { get; } = assigned;
    internal StageRole Effective { get; } = effective;
    internal IReadOnlyList<AbilityValue> Values { get; } = values;
}

internal static class RoleAbilityCodec
{
    internal const int MaximumPayloadLength = 20000;
    internal static string Encode(IReadOnlyList<AbilitySnapshot> snapshots)
    {
        List<string> rows = new();
        foreach (AbilitySnapshot snapshot in snapshots)
        {
            if (rows.Count >= 30) break;
            List<string> values = new();
            foreach (AbilityValue value in snapshot.Values)
            {
                if (values.Count >= 20) break;
                values.Add(FormattableString.Invariant($"{(int)value.Metric},{value.Remaining},{value.Limit}"));
            }
            string id = Convert.ToBase64String(Encoding.UTF8.GetBytes(snapshot.SteamId));
            rows.Add(FormattableString.Invariant($"{id}|{(int)snapshot.Assigned}|{(int)snapshot.Effective}|{string.Join(";", values)}"));
        }
        return string.Join("\n", rows);
    }

    internal static IReadOnlyDictionary<string, AbilitySnapshot> Decode(string payload)
    {
        Dictionary<string, AbilitySnapshot> result = new(StringComparer.Ordinal);
        if (payload.Length > MaximumPayloadLength) return result;
        string[] rows = payload.Split('\n');
        if (rows.Length > 30) return result;
        foreach (string row in rows)
        {
            string[] fields = row.Split('|');
            if (fields.Length != 4 || fields[0].Length > 128 ||
                !Number(fields[1], out int assigned) || !Enum.IsDefined(typeof(StageRole), assigned) ||
                !Number(fields[2], out int effective) || !Enum.IsDefined(typeof(StageRole), effective)) continue;
            string id;
            try { id = Encoding.UTF8.GetString(Convert.FromBase64String(fields[0])); }
            catch (FormatException) { continue; }
            if (string.IsNullOrWhiteSpace(id) || result.ContainsKey(id)) continue;
            string[] encodedValues = fields[3].Length == 0 ? Array.Empty<string>() : fields[3].Split(';');
            if (encodedValues.Length > 20) continue;
            List<AbilityValue> values = new();
            HashSet<AbilityMetric> seen = new();
            bool valid = true;
            foreach (string encoded in encodedValues)
            {
                string[] parts = encoded.Split(',');
                if (parts.Length != 3 || !Number(parts[0], out int code) ||
                    !Enum.IsDefined(typeof(AbilityMetric), code) ||
                    !Number(parts[1], out int remaining) || remaining is < 0 or > 10000 ||
                    !Number(parts[2], out int limit) || limit is < 0 or > 10000 ||
                    !seen.Add((AbilityMetric)code)) { valid = false; break; }
                values.Add(new AbilityValue((AbilityMetric)code, remaining, limit));
            }
            if (valid) result[id] = new AbilitySnapshot(id, (StageRole)assigned, (StageRole)effective, values);
        }
        return result;
    }

    private static bool Number(string value, out int result) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
}

internal static class RoleAbilityText
{
    internal static string Format(IReadOnlyList<AbilityValue> values, RoleGuideLanguage language, int start = 0, int count = 20)
    {
        List<string> parts = new();
        for (int i = start; i < Math.Min(values.Count, start + count); i++)
        {
            AbilityValue value = values[i];
            (string en, string ja) = value.Metric switch
            {
                AbilityMetric.Medic => ("Healing", "回復"),
                AbilityMetric.Rescuer => ("Revives", "蘇生"),
                AbilityMetric.Phoenix => ("Self revive", "自己蘇生"),
                AbilityMetric.MageRecovery => ("Recovery", "自動回復"),
                AbilityMetric.MageCooldown => ("Cast", "魔法"),
                AbilityMetric.Repair => ("Repair", "修理"),
                AbilityMetric.Charge => ("Charge", "充電"),
                AbilityMetric.King => ("Aura", "王の回復"),
                AbilityMetric.RoyalSupport => ("Supported allies", "強化中の味方"),
                AbilityMetric.Contracts => ("Jobs left", "残り契約"),
                AbilityMetric.Grace => ("Grace", "免除"),
                AbilityMetric.Carry => ("Carry", "運搬"),
                AbilityMetric.CloudDistance => ("Cloud travel", "ウラン雲の移動量"),
                AbilityMetric.GrenadeDistance => ("Grenade travel", "爆弾の移動量"),
                AbilityMetric.DecoyActive => ("Decoy active", "デコイ稼働"),
                AbilityMetric.DecoyCooldown => ("Decoy", "デコイ"),
                AbilityMetric.DiveActive => ("Dive remaining", "潜行残り"),
                AbilityMetric.DiveCooldown => ("Dive", "潜行"),
                AbilityMetric.Wager => ("Wagers", "賭け"),
                _ => ("Revenge", "復讐")
            };
            string label = RoleText.Get(en, language, ja);
            bool seconds = value.Metric is AbilityMetric.MageCooldown or AbilityMetric.Grace or
                AbilityMetric.DecoyActive or AbilityMetric.DecoyCooldown or AbilityMetric.DiveActive or
                AbilityMetric.DiveCooldown or AbilityMetric.Avenger;
            string amount = seconds ? $"{value.Remaining}s" : $"{value.Remaining}/{value.Limit}";
            if (value.Metric == AbilityMetric.RoyalSupport) amount = value.Remaining.ToString(CultureInfo.InvariantCulture);
            if (value.Metric is AbilityMetric.Carry or AbilityMetric.CloudDistance or AbilityMetric.GrenadeDistance) amount += "m";
            if (value.Metric is AbilityMetric.Repair or AbilityMetric.Charge) amount += "%";
            parts.Add($"{label} {amount}");
        }
        return string.Join("  |  ", parts);
    }
}
