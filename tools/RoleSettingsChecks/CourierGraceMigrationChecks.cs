using BepInEx.Configuration;
using REPOJP.StageRoles;

internal static class CourierGraceMigrationChecks
{
    internal static int Run(string directory)
    {
        int checks = 0;
        void Check(bool ok, string label) { checks++; if (!ok) throw new Exception(label); }
        string[] keys = { "Initial", "Tiny", "Small", "Medium", "Big", "Wide", "Tall", "VeryTall" };
        float[] oldValues = { 30, 30, 30, 60, 90, 90, 90, 120 };
        float[] newValues = { 120, 90, 120, 180, 240, 240, 240, 300 };
        float[] Durations(StageRolesConfig c) => new[] { c.JoblessInitialGrace.Value, c.JoblessTinyGrace.Value,
            c.JoblessSmallGrace.Value, c.JoblessMediumGrace.Value, c.JoblessBigGrace.Value,
            c.JoblessWideGrace.Value, c.JoblessTallGrace.Value, c.JoblessVeryTallGrace.Value };
        foreach (int schema in new[] { 38, 42, 43, 44, 99 })
        foreach (string variant in new[] { "default", "decimal", "exponent", "zero", "custom", "mixed" })
        {
            string section = schema == 38 ? "Jobless" : "Courier";
            string path = Path.Combine(directory, $"courier-grace-{schema}-{variant}.cfg");
            float[] beforeValues = oldValues.Select((v, i) => variant == "zero" ? 0f :
                variant == "custom" || (variant == "mixed" && i % 2 == 0) ? 17.5f + i : v).ToArray();
            string text = $"[Migration]\nConfigVersion = {schema}\n[{section}]\n";
            for (int i = 0; i < keys.Length; i++)
                text += keys[i] + "GraceSeconds = " + beforeValues[i].ToString(
                    variant == "decimal" ? "F3" : variant == "exponent" ? "0.0E+0" : "G", System.Globalization.CultureInfo.InvariantCulture) + "\n";
            text += "Damage = 2\nDamageIntervalSeconds = 0.25\nContractDistance = 7\nTinyHealAmount = 13\nEnabled = false\nWeight = 7\n[Tracker]\nReportIntervalSeconds = 27\n";
            File.WriteAllText(path, text);
            var file = new ConfigFile(path, false) { SaveOnConfigSet = false };
            var config = new StageRolesConfig(file);
            float[] after = Durations(config);
            for (int i = 0; i < keys.Length; i++)
                Check(after[i] == (schema < 44 && beforeValues[i] == oldValues[i] ? newValues[i] : beforeValues[i]),
                    $"{schema}/{variant}/{keys[i]} changes only old defaults");
            Check(config.JoblessDamage.Value == 2 && config.JoblessDamageIntervalSeconds.Value == .25f &&
                config.JoblessContractDistance.Value == 7 && config.JoblessTinyHeal.Value == 13 &&
                !config.JoblessEnabled.Value && config.JoblessWeight.Value == 7 && File.ReadAllText(path).Contains("ReportIntervalSeconds = 27"),
                "Custom attrition, delivery, healing, draw settings and unrelated role values survive");
            if (schema == 43)
                Check(File.ReadAllText(path + ".pre-v4.6.1-courier-grace.bak") == text, "Exact rollback backup");
            if (schema >= 44) Check(File.ReadAllText(path) == text, "Current/future schema is not rewritten");
            file.Save();
            string once = File.ReadAllText(path);
            RoleConfigMigration.Apply(file);
            Check(File.ReadAllText(path) == once, "Repeated migration is idempotent");
            var loaded = new StageRolesConfig(new ConfigFile(path, false) { SaveOnConfigSet = false });
            Check(Durations(loaded).SequenceEqual(after), "Durations survive reload");
        }
        string freshPath = Path.Combine(directory, "courier-fresh.cfg");
        var freshFile = new ConfigFile(freshPath, false) { SaveOnConfigSet = false };
        var fresh = new StageRolesConfig(freshFile);
        Check(Durations(fresh).SequenceEqual(newValues), "Fresh configuration receives longer pauses");
        Check(fresh.JoblessDamage.Value == 1 && fresh.JoblessDamageIntervalSeconds.Value == .1f &&
            fresh.JoblessContractDistance.Value == 5, "Attrition and delivery distance defaults unchanged");
        foreach (string name in keys)
            Check(freshFile[new ConfigDefinition("Courier", name + "GraceSeconds")].Description.AcceptableValues is
                AcceptableValueRange<float> range && range.MinValue == 0 && range.MaxValue == 300,
                "All durations remain editable through REPOConfig");
        fresh.JoblessInitialGrace.Value = 30;
        fresh.JoblessSmallGrace.Value = 30;
        freshFile.Save();
        var restored = new StageRolesConfig(new ConfigFile(freshPath, false) { SaveOnConfigSet = false });
        Check(restored.JoblessInitialGrace.Value == 30 && restored.JoblessSmallGrace.Value == 30,
            "Users can restore old durations after migration without another override");
        return checks;
    }
}
