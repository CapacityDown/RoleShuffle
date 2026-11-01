using BepInEx.Configuration;
using Photon.Pun;
namespace REPOJP.StageRoles;
internal static class CustomPresetChecks
{
    internal static void Run(string directory)
    {
        int checks = 0;
        void Check(bool ok, string name) { checks++; if (!ok) throw new Exception(name); }
        SemiFunc.Multiplayer = false; GameManager.instance = new();
        string path = Path.Combine(directory, "custom.cfg");
        var file = new ConfigFile(path, false) { SaveOnConfigSet = true };
        var config = new StageRolesConfig(file);
        var service = new RoleSelectionSettings(config, file);
        StageRolesPlugin.Instance.RoleSettings = service;
        string presetPath = Path.ChangeExtension(path, ".presets.json");
        Check(CustomRolePresets.Capacity == 3 && !File.Exists(presetPath), "Exactly three empty slots without eager write");
        foreach (int invalid in new[] {-1,3,100}) Check(!service.TrySaveCustomPreset(invalid) && !service.TryApplyCustomPreset(invalid), "Invalid slot rejected");
        config.MageWeight.Value = 27; config.MageStarHealthCost.Value = 11;
        for (int i = 0; i < 3; i++)
        {
            service.TryApplyPreset((RolePreset)i);
            string before = File.ReadAllText(path);
            Check(service.TrySaveCustomPreset(i), "Save each slot");
            Check(File.ReadAllText(path) == before, "Saving preset does not apply or alter live config");
        }
        var loaded = new RoleSelectionSettings(config, file);
        for (int i = 0; i < 3; i++)
        {
            Check(loaded.TryApplyCustomPreset(i), "Apply after restart");
            Check(RoleCatalog.AllRoles.All(r => config.RoleIsEnabled(r) == RolePresets.Find((RolePreset)i)!.Includes(r)), "Every saved switch restored");
            Check(config.MageWeight.Value == 27 && config.MageStarHealthCost.Value == 11, "Weights and ability values retained");
        }
        string saved = File.ReadAllText(presetPath);
        Check(!loaded.TrySaveCustomPreset(0) && File.ReadAllText(presetPath) == saved, "Occupied slot needs explicit overwrite");
        foreach (var role in RoleCatalog.AllRoles) config.RoleEnabledEntry(role)!.Value = false;
        Check(loaded.TrySaveCustomPreset(0, true), "Overwrite allowed");
        Check(File.ReadAllText(presetPath + ".bak") == saved, "Previous custom preset file backed up");
        var reload = new RoleSelectionSettings(config, file);
        Check(reload.CustomPresets.Get(0)?.Count == 0 && reload.TryApplyCustomPreset(0), "Saved all-OFF preset differs from empty slot");
        Check(reload.CustomPresets.Get(1)!.Count == RolePresets.Find(RolePreset.Beginner)!.Count, "Overwrite preserves other slots");
        var switches = RoleCatalog.AllRoles.ToDictionary(r => r, config.RoleIsEnabled);
        using (File.Open(path, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            bool failed = false; try { reload.TryApplyCustomPreset(1); } catch (IOException) { failed = true; }
            Check(failed && switches.All(p => config.RoleIsEnabled(p.Key) == p.Value), "Failed application rolls back every switch");
        }
        int revision = reload.CustomPresets.Revision;
        saved = File.ReadAllText(presetPath);
        using (File.Open(presetPath, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            bool failed = false; try { reload.TrySaveCustomPreset(1,true); } catch (IOException) { failed = true; }
            Check(failed && reload.CustomPresets.Revision == revision, "Failed storage does not change memory");
        }
        Check(File.ReadAllText(presetPath) == saved && !Directory.GetFiles(directory, "*.tmp").Any(), "Failed storage preserves file and clears temporary data");
        SemiFunc.Multiplayer = true; PhotonNetwork.IsMasterClient = false;
        Check(!reload.TrySaveCustomPreset(0,true) && !reload.TryApplyCustomPreset(1), "Guests cannot save/apply host presets");
        Check(File.ReadAllText(presetPath) == saved, "Guest denial leaves disk intact");
        PhotonNetwork.IsMasterClient = true;
        foreach (string invalid in new[] {"broken json", "{\"version\":2,\"slots\":[null,null,null]}", "{\"version\":1,\"slots\":[null,null,null,null]}"})
        {
            File.WriteAllText(presetPath, invalid);
            var bad = new RoleSelectionSettings(config,file);
            Check(!bad.CustomPresets.Available && !bad.TrySaveCustomPreset(0,true) && !bad.TryApplyCustomPreset(0), "Invalid/future data is unavailable, not silently replaced");
            Check(File.ReadAllText(presetPath) == invalid, "Invalid original preserved");
        }
        File.WriteAllText(presetPath,"{\"version\":1,\"slots\":[[\"Tank\",\"FutureRole\"],null,null]}");
        var future = new RoleSelectionSettings(config,file);
        Check(future.TryApplyCustomPreset(0) && config.TankEnabled.Value && !config.RunnerEnabled.Value, "Stable role names and unknown future role do not alias role numbers");
        StageRolesPlugin.Instance.RoleSettings = future;
        RoleMenu.Show(config, presets:true);
        Check(RoleMenu.Entries.Count(e => e.Text.StartsWith("Custom ")) == 3, "Three slots shown");
        RoleMenu.Entries.Single(e => e.Text == "Overwrite: Custom 1").OnClick!();
        Check(File.ReadAllText(presetPath).Contains("FutureRole"), "First overwrite click is only confirmation UI");
        RoleMenu.Entries.Single(e => e.Text == "Confirm overwrite: Custom 1").OnClick!();
        Check(!File.ReadAllText(presetPath).Contains("FutureRole"), "Confirmation saves current known selection");
        RoleMenu.Entries.Single(e => e.Text == "Save current: Custom 2").OnClick!();
        Check(future.CustomPresets.Get(1) != null, "Empty slot saves with one click");
        Console.WriteLine($"PASS: {checks} custom-preset persistence, authority, failure and menu checks.");
    }
}
