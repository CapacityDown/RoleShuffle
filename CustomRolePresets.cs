using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace REPOJP.StageRoles;

internal sealed class CustomRolePreset(IEnumerable<string> names)
{
    private readonly HashSet<string> _enabled = new(names, StringComparer.Ordinal);
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
            if (data == null || data.Version != 1 || data.Slots == null || data.Slots.Length != Capacity)
                throw new InvalidDataException("Unsupported custom presets.");
            for (int i = 0; i < Capacity; i++)
            {
                string[]? names = data.Slots[i];
                if (names == null) continue; // null is unused; [] is a saved all-OFF preset.
                if (names.Length > 512 || Array.Exists(names, n => string.IsNullOrWhiteSpace(n) || n.Length > 80))
                    throw new InvalidDataException("Invalid custom preset roles.");
                // Stable names survive role-number changes. Unknown/removed roles
                // remain stored but never enable a different or newly added role.
                _slots[i] = new CustomRolePreset(names);
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
        next[slot] = new CustomRolePreset(names);
        var data = new PresetFile { Version = 1, Slots = new string[]?[Capacity] };
        for (int i = 0; i < Capacity; i++) data.Slots[i] = next[i]?.Names;
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
    }
}
