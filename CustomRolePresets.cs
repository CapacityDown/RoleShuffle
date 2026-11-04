using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace REPOJP.StageRoles;

internal sealed class CustomRolePreset(IEnumerable<string> names, string note = "")
{
    private readonly HashSet<string> _enabled = new(names, StringComparer.Ordinal);
    internal string Note { get; } = note;
    internal bool Includes(StageRole role) => _enabled.Contains(role.ToString());
    internal int Count
    {
        get { int count = 0; foreach (StageRole role in RoleCatalog.AllRoles) if (Includes(role)) count++; return count; }
    }
    internal string[] Names { get { string[] result = new string[_enabled.Count]; _enabled.CopyTo(result); Array.Sort(result, StringComparer.Ordinal); return result; } }
}

// Local profile data, separate from run saves and the synchronized role selection.
internal sealed class CustomRolePresets
{
    internal const int Capacity = 3;
    internal const int MaximumNoteLength = 120;
    private readonly string _path;
    private CustomRolePreset?[] _slots = new CustomRolePreset?[Capacity];
    internal bool Available { get; private set; } = true;
    internal int Revision { get; private set; }

    internal CustomRolePresets(string path)
    {
        _path = path;
        try
        {
            if (!File.Exists(path)) return;
            if (new FileInfo(path).Length > 65536) throw new InvalidDataException("Preset file too large.");
            using FileStream stream = File.OpenRead(path);
            var data = (PresetFile?)new DataContractJsonSerializer(typeof(PresetFile)).ReadObject(stream);
            if (data == null || (data.Version != 1 && data.Version != 2) || data.Slots == null || data.Slots.Length != Capacity)
                throw new InvalidDataException("Unsupported custom presets.");
            if ((data.Version == 2 && data.Notes == null) || (data.Notes != null && data.Notes.Length != Capacity))
                throw new InvalidDataException("Invalid custom preset notes.");
            for (int i = 0; i < Capacity; i++)
            {
                string[]? names = data.Slots[i];
                string note = data.Notes?[i] ?? string.Empty;
                if (note.Length > MaximumNoteLength || note != NormalizeNote(note) || (names == null && note.Length > 0))
                    throw new InvalidDataException("Invalid custom preset note.");
                if (names == null) continue; // null is unused; [] is a saved all-OFF preset.
                if (names.Length > 512 || Array.Exists(names, n => string.IsNullOrWhiteSpace(n) || n.Length > 80))
                    throw new InvalidDataException("Invalid custom preset roles.");
                // Stable names survive role-number changes. Unknown/removed roles
                // remain stored but never enable a different or newly added role.
                _slots[i] = new CustomRolePreset(names, note);
            }
        }
        catch (Exception exception)
        {
            Available = false; // Preserve unreadable/future-version files, never silently overwrite them.
            StageRolesPlugin.ModLogger.LogWarning($"Custom presets unavailable: {exception.Message}");
        }
    }

    internal CustomRolePreset? Get(int slot) => Available && slot >= 0 && slot < Capacity ? _slots[slot] : null;

    internal bool TrySave(int slot, Func<StageRole, bool> enabled, bool overwrite)
    {
        if (!Available || slot < 0 || slot >= Capacity || (_slots[slot] != null && !overwrite)) return false;
        List<string> names = new();
        foreach (StageRole role in RoleCatalog.AllRoles) if (enabled(role)) names.Add(role.ToString());
        var next = (CustomRolePreset?[])_slots.Clone();
        next[slot] = new CustomRolePreset(names, _slots[slot]?.Note ?? string.Empty);
        return Write(next);
    }

    internal bool TrySetNote(int slot, string? note)
    {
        var preset = Get(slot);
        if (preset == null) return false;
        string normalized = NormalizeNote(note);
        if (normalized == preset.Note) return true;
        var next = (CustomRolePreset?[])_slots.Clone();
        next[slot] = new CustomRolePreset(preset.Names, normalized);
        return Write(next);
    }

    // Single-line plain text. Avoid splitting UTF-16 pairs at the input limit.
    internal static string NormalizeNote(string? note, bool trim = true)
    {
        StringBuilder result = new();
        if (note != null)
            for (int i = 0; i < note.Length && result.Length < MaximumNoteLength; i++)
            {
                char c = note[i];
                if (char.IsHighSurrogate(c))
                {
                    if (i + 1 < note.Length && char.IsLowSurrogate(note[i + 1]) && result.Length + 2 <= MaximumNoteLength)
                    { result.Append(c).Append(note[++i]); }
                }
                else if (char.IsLowSurrogate(c)) continue;
                else if (char.IsWhiteSpace(c)) result.Append(' ');
                else if (!char.IsControl(c)) result.Append(c);
            }
        string value = result.ToString();
        return trim ? value.Trim() : value;
    }

    private bool Write(CustomRolePreset?[] next)
    {
        var data = new PresetFile { Version = 2, Slots = new string[]?[Capacity], Notes = new string?[Capacity] };
        for (int i = 0; i < Capacity; i++)
        { data.Slots[i] = next[i]?.Names; data.Notes[i] = next[i]?.Note; }
        string temporary = _path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(_path))!);
            using (FileStream stream = new(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                new DataContractJsonSerializer(typeof(PresetFile)).WriteObject(stream, data);
                stream.Flush(true);
            }
            if (File.Exists(_path)) File.Replace(temporary, _path, _path + ".bak");
            else File.Move(temporary, _path);
            _slots = next;
            Revision++;
            return true;
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    [DataContract]
    private sealed class PresetFile
    {
        [DataMember(Name = "version", IsRequired = true)] public int Version;
        [DataMember(Name = "slots", IsRequired = true)] public string[]?[] Slots = null!;
        [DataMember(Name = "notes", EmitDefaultValue = false)] public string?[]? Notes;
    }
}
