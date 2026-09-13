using REPOJP.StageRoles;

int checks = 0;
void Check(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
    checks++;
}
var stats = new Dictionary<string, int> { ["level"] = 10, ["RoleShuffle.BaseUpgradeBonus.playerUpgradeHealth"] = 4 };
Check(DrawHistoryStore.Read(stats).Count == 0, "Existing saves without history remain readable");
for (int i = 1; i <= 137; i++)
{
    var record = new UpgradeDrawRecord { Level = i, Upgrade = i % 12, Delta = i % 2 == 0 ? 1 : -1 };
    record.Before[record.Upgrade] = 10;
    record.After[record.Upgrade] = 10 + record.Delta;
    DrawHistoryStore.Append(stats, record);
}
var history = DrawHistoryStore.Read(stats);
Check(history.Count == 50 && history[0].Number == 137 && history[^1].Number == 88, "Ring retains exactly the latest fifty draws");
Check(history.All(r => r.Level == r.Number), "Overwriting slots never mixes records");
Check(stats.Count == 50 * 28 + 3, "Save footprint remains bounded");
Check(stats["level"] == 10 && stats["RoleShuffle.BaseUpgradeBonus.playerUpgradeHealth"] == 4, "History does not mutate gameplay values");
string wire = DrawHistoryStore.Serialize(history);
Check(wire.Length < 20000, "Full history fits the bounded payload");
Check(DrawHistoryStore.TryParse(wire, out var remote) && remote.Count == 50, "Guest receives full history");
Check(DrawHistoryStore.Serialize(remote) == wire, "Host and guest values agree exactly");
var reloadedStats = new Dictionary<string, int>(stats);
Check(DrawHistoryStore.Serialize(DrawHistoryStore.Read(reloadedStats)) == wire, "Saving and reloading preserves every field");
Check(DrawHistoryStore.Read(new Dictionary<string, int>()).Count == 0, "Separate saves never inherit static history");
var corruptSequence = new Dictionary<string, int> { ["RoleShuffle.DrawHistory.Sequence"] = -100 };
DrawHistoryStore.Append(corruptSequence, new UpgradeDrawRecord { Upgrade = 0 });
Check(DrawHistoryStore.Read(corruptSequence).Count == 1, "New draw recovers a corrupt negative sequence");
stats.Remove("RoleShuffle.DrawHistory.37.7");
Check(DrawHistoryStore.Read(stats).Count == 49, "Incomplete slot is hidden");
Check(DrawHistoryStore.TryParse("1|", out var empty) && empty.Count == 0, "Empty host history is valid");
foreach (string bad in new[] { "", "2|", "1|1,2,3", "1|" + new string('x', 20001), wire + ";" + wire[2..],
             wire.Replace("137,137", "2147483648,137"), wire.Replace("137,137,5,-1", "137,137,999,-1"),
             "1|" + string.Join(";", wire[2..].Split(';').Reverse()) })
    Check(!DrawHistoryStore.TryParse(bad, out _), "Reject malformed, oversized, unsupported or out-of-order history");
var capped = new UpgradeDrawRecord { Level = 5, Upgrade = 0, Delta = 1 };
capped.Before[0] = capped.After[0] = 200;
DrawHistoryStore.Append(reloadedStats, capped);
var zero = new UpgradeDrawRecord { Level = 6, Upgrade = 0, Delta = 0 };
DrawHistoryStore.Append(reloadedStats, zero);
var all = new UpgradeDrawRecord { Level = 7, Upgrade = -1, Delta = -2 };
for (int i = 0; i < 12; i++) { all.Before[i] = i; all.After[i] = Math.Max(0, i - 2); }
DrawHistoryStore.Append(reloadedStats, all);
Check(DrawHistoryStore.TryParse(DrawHistoryStore.Serialize(DrawHistoryStore.Read(reloadedStats)), out var special), "Special outcomes round trip");
Check(special[0].Upgrade == -1 && special[0].After.SequenceEqual(all.After), "All Upgrades retains per-upgrade limits");
Check(special[1].Delta == 0 && special[2].Before[0] == special[2].After[0], "Zero draws and capped outcomes are retained");

string?[] payloads = { "guide", "visible", "base", "", "1|" };
string stamp = SyncStamp.Create(2, "4.4.6", int.MaxValue - 5, payloads);
Check(SyncStamp.TryRead(stamp, out int actor, out string version, out int timestamp, out var hashes) && actor == 2 && version == "4.4.6", "Status identifies current host and version");
Check(SyncStamp.Matches(hashes, payloads), "Complete snapshot matches");
for (int i = 0; i < 5; i++)
{
    var changed = payloads.ToArray(); changed[i] += "changed";
    Check(!SyncStamp.Matches(hashes, changed), "Each unsynchronized data set is detected");
    changed[i] = null;
    Check(!SyncStamp.Matches(hashes, changed), "Missing data differs from valid empty data");
}
Check(SyncStamp.AgeMilliseconds(int.MinValue + 4, timestamp) == 10, "Photon timestamp wrap preserves heartbeat age");
Check(SyncStamp.AgeMilliseconds(30001, 10000) > 20000, "Stale heartbeat crosses delay threshold");
foreach (string bad in new[] { "", "2|2|4.4.6|100|abc", stamp.Replace("4.4.6", "invalid"), stamp.Replace("1|2|", "1|0|"), new string('a', 513), stamp[..^10] })
    Check(!SyncStamp.TryRead(bad, out _, out _, out _, out _), "Invalid metadata is never considered synchronized");

string sensitive = "Alice joined room-secret from 192.168.1.20:1234, 76561198000000001\n" +
    "Path C:\\Users\\Alice\\Games\\repo\\plugin.dll\n2001:db8:abcd::12\npassword=hunter2 token=abc123\n" +
    "github_pat_ABC123_456\nOrdinary failure: Upgrade Health 1 -> 2.\n日本語の例外";
string clean = ReportRedactor.Clean(sensitive, new[] { "Alice", "room-secret" });
foreach (string privateValue in new[] { "Alice", "room-secret", "192.168.1.20", "76561198000000001", "C:\\Users", "2001:db8", "hunter2", "abc123", "github_pat_ABC123_456" })
    Check(!clean.Contains(privateValue, StringComparison.Ordinal), "Private report value is masked: " + privateValue);
Check(clean.Contains("Ordinary failure: Upgrade Health 1 -> 2.") && clean.Contains("日本語の例外"), "Diagnostic text and Japanese survive redaction");
Check(ReportRedactor.Clean(new string('x', 200000), Array.Empty<string>(), 100).Length < 120, "Output size is bounded");
Check(!ReportRedactor.Clean("text\u0000value", Array.Empty<string>()).Contains('\0'), "Control characters removed");
Check(ReportRedactor.Clean("At 12:34:56 operation failed", Array.Empty<string>()).Contains("12:34:56"), "Time of day is not mistaken for IPv6");
foreach (var viewport in new[] { (960f, 540f), (1280f, 720f), (1920f, 1080f), (3840f, 2160f), (720f, 540f), (2560f, 720f) })
{
    float density = viewport.Item2 / 540f;
    foreach (float userScale in new[] { 0.5f, 0.7f, 2f })
    {
        float scale = HudLayoutMath.Scale(viewport.Item1, viewport.Item2, 620, 340, userScale, density);
        Check(620 * scale < viewport.Item1 && 340 * scale < viewport.Item2, "HUD always fits the viewport");
        foreach (float anchor in new[] { 0f, 0.5f, 1f })
        {
            foreach (int offset in new[] { -3840, 0, 3840 })
            {
                int x = HudLayoutMath.ClampOffset(offset, anchor, viewport.Item1, 620 * scale, density, 3840);
                float left = anchor * viewport.Item1 + x * density - anchor * 620 * scale;
                Check(left >= -0.01f && left + 620 * scale <= viewport.Item1 + 0.01f, "Dragging stays in view at every anchor and resolution");
            }
        }
    }
}
foreach (float rowHeight in new[] { 36f, 44f, 58.3f, 72f, 96.7f, 136f, 180f })
foreach (int requested in new[] { 2, 4, 6, 8, 20 })
{
    const float heading = 68.3f, gap = 4f;
    float height = HudLayoutMath.ContentHeight(340, heading, gap, rowHeight, requested);
    int capacity = HudLayoutMath.PageCapacity(requested, height, heading, gap, rowHeight);
    Check(capacity >= Math.Min(6, requested) && capacity <= requested, "Tall fallback glyphs/icons cannot reduce a six-player page");
    Check(heading + gap + capacity * rowHeight <= height + 0.01f, "Every row fits below the heading");
    foreach (var viewport in new[] { (960f, 540f), (720f, 540f), (1920f, 1080f), (3840f, 2160f), (2560f, 720f) })
    foreach (float userScale in new[] { 0.5f, 0.7f, 2f })
    {
        float density = viewport.Item2 / 540f;
        float scale = HudLayoutMath.Scale(viewport.Item1, viewport.Item2, 620, height, userScale, density);
        foreach (float anchor in new[] { 0f, 0.5f, 1f })
        foreach (int offset in new[] { -2160, 0, 80, 2160 })
        {
            int y = HudLayoutMath.ClampOffset(offset, anchor, viewport.Item2, height * scale, density, 2160);
            float bottom = anchor * viewport.Item2 + y * density - anchor * height * scale;
            Check(bottom >= -0.01f && bottom + height * scale <= viewport.Item2 + 0.01f,
                "Six rows stay on screen for all anchors, scales and offsets");
        }
    }
}
// Model wide CJK and narrow Latin glyphs; production supplies TMP's measured width.
float TextWidth(string text) => text.Sum(c => c > 127 ? 28f : 12f);
foreach (string name in new[] { "日本語の長いプレイヤー名あいうえおかきくけこ", "简体中文测试玩家名字很长", "繁體中文測試玩家名字很長", "한국어긴플레이어이름테스트", "Український_гравець", "ＡＢＣＤＥＦＧＨＩＪＫＬＭＮＯＰ", "Player\nNew\rLine\tTest\u2028Name\u2029End" })
foreach (float width in new[] { 240f, 480f, 620f })
{
    string text = HudLabelText.Fit(name, "Imitator", width, TextWidth);
    Check(text.EndsWith(": Imitator", StringComparison.Ordinal) && TextWidth(text) <= width, "Long names preserve the whole role within the available width");
    Check(!text.Any(c => char.IsControl(c) || c is '\u2028' or '\u2029'), "Player names never add HUD rows");
}
string combinedName = string.Concat(Enumerable.Repeat("e\u0301", 30));
string shortenedCombining = HudLabelText.Fit(combinedName, "", 120, TextWidth);
Check(shortenedCombining.EndsWith("\u0301…", StringComparison.Ordinal), "Ellipsis retains the complete combining character");
string supplementary = string.Concat(Enumerable.Repeat("𠮷", 30));
string shortenedSupplementary = HudLabelText.Fit(supplementary, "Tank", 240, TextWidth);
Check(!System.Text.RegularExpressions.Regex.IsMatch(shortenedSupplementary, "[\\uD800-\\uDBFF](?![\\uDC00-\\uDFFF])|(?<![\\uD800-\\uDBFF])[\\uDC00-\\uDFFF]"), "Ellipsis never splits surrogate pairs");
Check(HudLabelText.Fit("YOU", "Tank", 620, TextWidth) == "YOU: Tank", "Pinned self stays readable");
Check(HudLabelText.Fit("Player", "", 620, TextWidth) == "Player", "Icon-only display omits the role text");
Check(HudLabelText.Fit("\n\t", "Tank", 620, TextWidth) == "Player: Tank", "Empty/control-only names use a fallback");
Console.WriteLine($"PASS: {checks} utility checks (history, save boundaries, synchronization, privacy, HUD geometry and Unicode names).");
