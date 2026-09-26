using BepInEx.Configuration;
using REPOJP.StageRoles;

internal static class AbilityConfigChecks
{
    internal static int Run(string directory)
    {
        int checks = 0;
        void Check(bool value, string label) { checks++; if (!value) throw new Exception(label); }
        string path = Path.Combine(directory, "ability-settings.cfg");
        File.WriteAllText(path, "[Migration]\nConfigVersion = 40\n[Tank]\nHealthMultiplier = 2.3\n[King]\nStrengthBonusLevels = 9\n[Courier]\nTinyHealAmount = 13\n");
        var file = new ConfigFile(path, false) { SaveOnConfigSet = false };
        var config = new StageRolesConfig(file);
        var cases = new (string Section, string Key, object Default, object Min, object Max)[]
        {
            ("Mage", "BeamDurationSeconds", 5f, 0.1f, 30f),
            ("Lifter", "HeavyGripMultiplier", 1f, 0.1f, 5f),
            ("Lifter", "HeavyRotationMultiplier", 1f, 0.1f, 5f),
            ("Lifter", "LightItemStrengthLevel", 1, 0, 200),
            ("Stinker", "BreakGraceSeconds", 0.5f, 0f, 10f),
            ("Influenza", "IncubationSeconds", 30f, 0f, 300f),
            ("Influenza", "MaximumHealth", 75, 1, 10000),
            ("Influenza", "SneezeMinimumSeconds", 30f, 1f, 600f),
            ("Influenza", "SneezeMaximumSeconds", 90f, 1f, 600f),
            ("Influenza", "SneezeRange", 5f, 0.1f, 30f),
            ("Influenza", "SneezeAngleDegrees", 40f, 1f, 360f),
            ("Influenza", "SneezeChancePercent", 60f, 0f, 100f),
            ("Influenza", "SpeechRange", 3f, 0.1f, 30f),
            ("Influenza", "SpeechAngleDegrees", 60f, 1f, 360f),
            ("Influenza", "SpeechChancePercent", 30f, 0f, 100f),
            ("Influenza", "SpeechSilenceSeconds", 0.75f, 0.1f, 5f),
            ("Influenza", "SneezeNoiseRadius", 5f, 0f, 100f),
        };
        var entries = file.ToDictionary(pair => pair.Key, pair => pair.Value);
        var saved = new Dictionary<ConfigDefinition, object>();
        foreach (var item in cases)
        {
            var definition = new ConfigDefinition(item.Section, item.Key);
            Check(entries.TryGetValue(definition, out ConfigEntryBase? found), "REPOConfig binding: " + definition);
            ConfigEntryBase entry = found!;
            Check(Equals(entry.BoxedValue, item.Default), "Existing behaviour is the default: " + definition);
            Check(entry.Description.AcceptableValues != null && entry.Description.Description.Length > 10,
                "Editable range and description: " + definition);
            entry.BoxedValue = item.Default is int ? (object)(-999) : -999f;
            Check(Equals(entry.BoxedValue, item.Min), "Lower range: " + definition);
            entry.BoxedValue = item.Default is int ? (object)999999 : 999999f;
            Check(Equals(entry.BoxedValue, item.Max), "Upper range: " + definition);
            entry.BoxedValue = item.Default is int
                ? (object)((int)item.Min + ((int)item.Max - (int)item.Min) / 3)
                : (float)item.Min + ((float)item.Max - (float)item.Min) / 3f;
            saved.Add(definition, entry.BoxedValue);
        }
        file.Save();
        var reloaded = new ConfigFile(path, false) { SaveOnConfigSet = false };
        var loaded = new StageRolesConfig(reloaded);
        var reloadedEntries = reloaded.ToDictionary(pair => pair.Key, pair => pair.Value);
        foreach (var pair in saved)
        {
            Check(reloadedEntries.TryGetValue(pair.Key, out ConfigEntryBase? entry) && Equals(entry.BoxedValue, pair.Value),
                "Custom ability value survives reload: " + pair.Key);
        }
        Check(loaded.TankHealthMultiplier.Value == 2.3f && loaded.KingStrengthBonus.Value == 9 && loaded.JoblessTinyHeal.Value == 13,
            "Existing role tuning is preserved");
        foreach (var key in new[] { ("Tank", "HealthMultiplier"), ("Tank", "MaximumHealth"), ("Runner", "SpeedMultiplier"),
            ("Runner", "StaminaMultiplier"), ("Runner", "MaximumSprintSpeed"), ("Runner", "MaximumStamina"),
            ("King", "UpgradeRadius"), ("King", "StrengthBonusLevels"), ("Courier", "ContractDistance"),
            ("Courier", "TinyHealAmount"), ("Courier", "VeryTallGraceSeconds") })
            Check(reloadedEntries.ContainsKey(new ConfigDefinition(key.Item1, key.Item2)), "Existing changed-role setting remains editable: " + key);
        return checks;
    }
}
