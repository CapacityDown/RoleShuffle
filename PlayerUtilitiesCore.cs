using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace REPOJP.StageRoles;

internal static class HudLayoutMath
{
    internal static float ContentHeight(float minimumHeight, float headingHeight, float headingGap, float rowHeight, int requestedPlayers)
    {
        int reservedRows = Math.Min(6, Math.Max(2, Math.Min(20, requestedPlayers)));
        return Math.Max(minimumHeight, headingHeight + headingGap + reservedRows * rowHeight);
    }

    internal static int PageCapacity(int requestedPlayers, float contentHeight, float headingHeight, float headingGap, float rowHeight) =>
        Math.Min(Math.Max(2, Math.Min(20, requestedPlayers)),
            Math.Max(2, (int)Math.Floor((contentHeight - headingHeight - headingGap + 0.01f) / rowHeight)));

    internal static float Scale(float width, float height, float contentWidth, float contentHeight, float userScale, float resolutionScale)
    {
        float desired = Math.Max(0.1f, resolutionScale * userScale);
        if (width <= 1 || height <= 1) return desired;
        float margin = 16f * resolutionScale;
        float fit = Math.Min(Math.Max(1f, width - margin) / contentWidth, Math.Max(1f, height - margin) / contentHeight);
        return Math.Max(0.1f, Math.Min(desired, fit));
    }
    internal static int ClampOffset(int value, float anchor, float canvasSize, float contentSize, float resolutionScale, int limit)
    {
        float remaining = Math.Max(0, canvasSize - contentSize);
        int minimum = (int)Math.Ceiling(Math.Max(-limit, -anchor * remaining / resolutionScale));
        int maximum = (int)Math.Floor(Math.Min(limit, (1 - anchor) * remaining / resolutionScale));
        return Math.Max(minimum, Math.Min(maximum, value));
    }
}

internal static class HudLabelText
{
    private const int MaximumNameElements = 20;

    internal static string SingleLine(string value)
    {
        StringBuilder text = new(value.Length);
        foreach (char character in value)
            text.Append(char.IsControl(character) || character is '\u2028' or '\u2029' ? ' ' : character);
        return text.ToString().Trim();
    }

    internal static string Fit(string playerName, string roleName, float width, Func<string, float> measure)
    {
        string name = SingleLine(playerName);
        if (string.IsNullOrWhiteSpace(name)) name = "Player";
        int[] elements = StringInfo.ParseCombiningCharacters(name);
        int count = elements.Length <= MaximumNameElements ? elements.Length : MaximumNameElements - 1;
        string suffix = roleName.Length == 0 ? string.Empty : ": " + roleName;
        while (true)
        {
            string shortened = count == elements.Length ? name : name.Substring(0, elements[count]) + "…";
            string label = shortened + suffix;
            // Preserve the role name. TMP may shrink this minimal label if
            // the selected font size still makes it wider than the HUD.
            if (measure(label) <= width || count == 0) return label;
            count--;
        }
    }
}

// This format uses explicit, stable upgrade IDs, independent of role/catalog ordering.
internal sealed class UpgradeDrawRecord
{
    internal int Number;
    internal int Level;
    internal int Upgrade; // -1 = all upgrades
    internal int Delta;
    internal int[] Before = new int[DrawHistoryStore.UpgradeNames.Length];
    internal int[] After = new int[DrawHistoryStore.UpgradeNames.Length];
}

internal static class DrawHistoryStore
{
    internal const int Capacity = 50;
    private const string Prefix = "RoleShuffle.DrawHistory.";
    internal static readonly string[] UpgradeNames =
    {
        "Health", "Stamina", "ExtraJump", "Speed", "Strength", "Range", "Launch",
        "TumbleClimb", "TumbleWings", "CrouchRest", "MapPlayerCount", "DeathHeadBattery"
    };

    internal static void Append(IDictionary<string, int> stats, UpgradeDrawRecord record)
    {
        int previous = Math.Max(0, Get(stats, Prefix + "Sequence"));
        if (previous == int.MaxValue) previous = 0;
        record.Number = previous + 1;
        string slot = Prefix + (record.Number % Capacity).ToString(CultureInfo.InvariantCulture) + ".";
        int[] values = Values(record);
        for (int i = 0; i < values.Length; i++) stats[slot + i] = values[i];
        // Commit the sequence last, so incomplete slots are never displayed.
        stats[Prefix + "Sequence"] = record.Number;
    }

    internal static IReadOnlyList<UpgradeDrawRecord> Read(IDictionary<string, int> stats)
    {
        List<UpgradeDrawRecord> records = new();
        int sequence = Math.Max(0, Get(stats, Prefix + "Sequence"));
        int count = Math.Min(Capacity, sequence);
        for (int offset = 0; offset < count; offset++)
        {
            int number = sequence - offset;
            string slot = Prefix + (number % Capacity).ToString(CultureInfo.InvariantCulture) + ".";
            int[] values = new int[4 + UpgradeNames.Length * 2];
            bool complete = true;
            for (int i = 0; i < values.Length; i++)
                if (!stats.TryGetValue(slot + i, out values[i])) { complete = false; break; }
            if (complete && values[0] == number && TryRecord(values, out UpgradeDrawRecord record))
                records.Add(record);
        }
        return records;
    }

    private static int Get(IDictionary<string, int> stats, string key) =>
        stats.TryGetValue(key, out int value) ? value : 0;

    private static int[] Values(UpgradeDrawRecord record) =>
        new[] { record.Number, record.Level, record.Upgrade, record.Delta }
            .Concat(record.Before).Concat(record.After).ToArray();

    internal static string Serialize(IReadOnlyList<UpgradeDrawRecord> records) =>
        "1|" + string.Join(";", records.Take(Capacity).Select(record =>
            string.Join(",", Values(record).Select(v => v.ToString(CultureInfo.InvariantCulture)))));

    internal static bool TryParse(string payload, out IReadOnlyList<UpgradeDrawRecord> records)
    {
        records = Array.Empty<UpgradeDrawRecord>();
        if (payload.Length > 20000 || !payload.StartsWith("1|", StringComparison.Ordinal)) return false;
        if (payload == "1|") return true;
        string[] entries = payload.Substring(2).Split(';');
        if (entries.Length > Capacity) return false;
        List<UpgradeDrawRecord> parsed = new();
        long previous = (long)int.MaxValue + 1;
        foreach (string entry in entries)
        {
            string[] fields = entry.Split(',');
            if (fields.Length != 4 + UpgradeNames.Length * 2) return false;
            int[] values = new int[fields.Length];
            for (int i = 0; i < fields.Length; i++)
                if (!int.TryParse(fields[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out values[i])) return false;
            if (!TryRecord(values, out UpgradeDrawRecord record) || record.Number >= previous) return false;
            previous = record.Number;
            parsed.Add(record);
        }
        records = parsed;
        return true;
    }

    private static bool TryRecord(int[] values, out UpgradeDrawRecord record)
    {
        record = new UpgradeDrawRecord();
        if (values[0] <= 0 || values[1] < 0 || values[2] < -1 || values[2] >= UpgradeNames.Length ||
            values[3] < -200 || values[3] > 200) return false;
        for (int i = 4; i < values.Length; i++) if (values[i] < 0 || values[i] > 200) return false;
        record.Number = values[0]; record.Level = values[1]; record.Upgrade = values[2]; record.Delta = values[3];
        Array.Copy(values, 4, record.Before, 0, UpgradeNames.Length);
        Array.Copy(values, 4 + UpgradeNames.Length, record.After, 0, UpgradeNames.Length);
        return true;
    }
}

internal static class SyncStamp
{
    internal static string Hash(string? payload)
    {
        if (payload == null) return "-";
        using SHA256 sha = SHA256.Create();
        byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return BitConverter.ToString(hash, 0, 8).Replace("-", string.Empty);
    }

    internal static string Create(int actor, string version, int timestamp, IEnumerable<string?> payloads) =>
        $"1|{actor.ToString(CultureInfo.InvariantCulture)}|{version}|{timestamp.ToString(CultureInfo.InvariantCulture)}|" +
        string.Join(",", payloads.Select(Hash));

    internal static bool TryRead(string stamp, out int actor, out string version, out int timestamp, out string[] hashes)
    {
        actor = 0; version = string.Empty; timestamp = 0; hashes = Array.Empty<string>();
        if (stamp.Length > 512) return false;
        string[] parts = stamp.Split('|');
        if (parts.Length != 5 || parts[0] != "1" || !int.TryParse(parts[1], out actor) || actor <= 0 ||
            !System.Version.TryParse(parts[2], out _) || !int.TryParse(parts[3], out timestamp)) return false;
        version = parts[2]; hashes = parts[4].Split(',');
        return hashes.Length == 5 && hashes.All(h => h.Length == 16 && h.All(Uri.IsHexDigit));
    }

    internal static bool Matches(string[] hashes, IEnumerable<string?> payloads) =>
        hashes.SequenceEqual(payloads.Select(Hash));

    internal static int AgeMilliseconds(int now, int then) => unchecked(now - then);
}

internal static class ReportRedactor
{
    internal static string Clean(string text, IEnumerable<string> privateValues, int limit = 80000)
    {
        // Bound work before applying regexes. Known values are replaced longest first.
        text = text.Substring(0, Math.Min(text.Length, 160000));
        foreach (string value in privateValues.Where(v => !string.IsNullOrWhiteSpace(v)).Distinct()
                     .OrderByDescending(v => v.Length))
            text = text.Replace(value, "[REDACTED]");
        text = Regex.Replace(text, @"(?i)(?:[a-z]:[\\/]|\\\\)[^\r\n\""<>|]*", "[PATH]");
        text = Regex.Replace(text, @"(?<!\d)\d{17}(?!\d)", "[STEAM-ID]");
        text = Regex.Replace(text, @"(?<!\d)(?:\d{1,3}\.){3}\d{1,3}(?::\d+)?", "[IP]");
        text = Regex.Replace(text, @"(?i)\b(?:[0-9a-f]{1,4}:){2,}[0-9a-f:]{0,39}\b",
            match => System.Net.IPAddress.TryParse(match.Value, out _) ? "[IP]" : match.Value);
        text = Regex.Replace(text, @"(?i)\b(?:token|password|authorization|api[_-]?key)\s*[:=]\s*[^\s,;]+", "[SECRET]");
        text = Regex.Replace(text, @"(?i)\b(?:gh[pousr]_[a-z0-9_]+|github_pat_[a-z0-9_]+)\b", "[SECRET]");
        text = Regex.Replace(text, @"[\x00-\x08\x0b\x0c\x0e-\x1f]", string.Empty);
        return text.Length <= limit ? text : text.Substring(0, limit) + "\n[TRUNCATED]";
    }
}
