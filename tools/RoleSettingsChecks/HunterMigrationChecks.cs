using BepInEx.Configuration;
using REPOJP.StageRoles;

internal static class HunterMigrationChecks
{
    internal static int Run(string directory)
    {
        int checks = 0;
        void Check(bool ok, string label) { checks++; if (!ok) throw new Exception(label); }
        foreach (int schema in new[] { 40, 41, 42, 99 })
        foreach (int interval in new[] { 0, 1, 2, 7 })
        {
            string path = Path.Combine(directory, $"hunter-{schema}-{interval}.cfg");
            string before = $"[Migration]\nConfigVersion = {schema}\n[Hunter]\nGuaranteedOrbEveryKills = {interval}\nWeaponBatteryConsumptionPercent = 75\nDoubleOrbChancePercent = 10\nJackpotOrbChancePercent = 0.5\nJackpotOrbCount = 10\n";
            File.WriteAllText(path, before);
            var file = new ConfigFile(path, false) { SaveOnConfigSet = false };
            var config = new StageRolesConfig(file);
            Check(config.HunterGuaranteedOrbEveryKills.Value == (schema < 42 && interval == 2 ? 5 : interval),
                "Only the old guarantee default migrates; disabled/custom/current/future values remain");
            Check(config.HunterBatteryConsumptionPercent.Value == 75 && config.HunterDoubleOrbChancePercent.Value == 10 &&
                config.HunterJackpotOrbChancePercent.Value == 0.5f && config.HunterJackpotOrbCount.Value == 10,
                "Existing battery and random-reward settings are unchanged");
            if (schema == 41)
                Check(File.ReadAllText(path + ".pre-v4.6.0-hunter.bak") == before, "Exact schema-41 rollback backup");
            if (schema == 99) Check(File.ReadAllText(path) == before, "Future schema not rewritten");
            file.Save();
            string migrated = File.ReadAllText(path);
            RoleConfigMigration.Apply(file);
            Check(File.ReadAllText(path) == migrated, "Hunter migration is idempotent");
            var loaded = new StageRolesConfig(new ConfigFile(path, false) { SaveOnConfigSet = false });
            Check(loaded.HunterGuaranteedOrbEveryKills.Value == config.HunterGuaranteedOrbEveryKills.Value,
                "Migrated/custom guarantee survives reload");
        }
        return checks;
    }
}
