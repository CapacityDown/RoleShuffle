using BepInEx.Configuration;
using REPOJP.StageRoles;

internal static class LifterUpgradeMigrationChecks
{
    internal static int Run(string directory)
    {
        int checks = 0;
        void Check(bool value, string label) { checks++; if (!value) throw new Exception(label); }
        string path = Path.Combine(directory, "lifter-upgrade-migration.cfg");
        const string before = "[Migration]\nConfigVersion = 44\n[Lifter]\nHeavyGripMultiplier = 2.5\nHeavyRotationMultiplier = 0.5\nLightItemStrengthLevel = 12\nEnabled = false\nWeight = 75\n[Courier]\nInitialGraceSeconds = 17\n";
        File.WriteAllText(path, before);
        var file = new ConfigFile(path, false) { SaveOnConfigSet = false };
        var config = new StageRolesConfig(file); file.Save();
        string after = File.ReadAllText(path);
        Check(File.ReadAllText(path + ".pre-v5.0.0-lifter-upgrades.bak") == before, "Exact prior configuration backup");
        Check(!after.Contains("HeavyGripMultiplier") && !after.Contains("HeavyRotationMultiplier"), "Obsolete force multipliers removed");
        Check(!file.Any(x => x.Key.Section == "Lifter" && x.Key.Key.Contains("Multiplier")), "Obsolete multipliers not exposed in REPOConfig");
        Check(config.LifterStrengthLevel.Value == 200, "New upgrade level defaults to 200");
        Check(config.LifterLightItemStrengthLevel.Value == 12 && !config.LifterEnabled.Value && config.LifterWeight.Value == 75, "Light handling and selection preferences preserved");
        Check(config.JoblessInitialGrace.Value == 17, "Other role preferences unchanged");
        RoleConfigMigration.Apply(file); Check(File.ReadAllText(path) == after, "Migration idempotent");
        foreach(int level in new[] { 0, 1, 20, 49, 50, 200 })
        {
            config.LifterStrengthLevel.Value = level; file.Save();
            var loaded = new StageRolesConfig(new ConfigFile(path, false) { SaveOnConfigSet = false });
            Check(loaded.LifterStrengthLevel.Value == level, "Custom upgrade level survives reload");
            Check(File.ReadAllText(path + ".pre-v5.0.0-lifter-upgrades.bak") == before, "Backup never overwritten");
        }
        return checks;
    }
}
