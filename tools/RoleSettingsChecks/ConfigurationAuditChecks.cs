using System.Reflection;
using BepInEx.Configuration;
using REPOJP.StageRoles;

internal static class ConfigurationAuditChecks
{
    internal static int Run(string directory)
    {
        int checks = 0;
        void Check(bool ok, string label) { if (!ok) throw new Exception(label); checks++; }
        string path = Path.Combine(directory, "configuration-audit.cfg");
        var file = new ConfigFile(path, false) { SaveOnConfigSet = false };
        var config = new StageRolesConfig(file);
        var properties = typeof(StageRolesConfig).GetProperties(BindingFlags.Instance | BindingFlags.NonPublic)
            .Where(p => typeof(ConfigEntryBase).IsAssignableFrom(p.PropertyType)).Select(p => (ConfigEntryBase)p.GetValue(config)!).ToArray();
        Check(properties.Distinct().Count() == properties.Length, "No two public settings share the same bound entry");
        var saved = new Dictionary<ConfigDefinition, object>();
        foreach (var pair in file)
        {
            ConfigEntryBase entry = pair.Value;
            string label = pair.Key.Section + "." + pair.Key.Key;
            Check(entry.Description.Description.Length > 0, "Description: " + label);
            Check(Equals(entry.BoxedValue, entry.DefaultValue), "Fresh default: " + label);
            var acceptable = entry.Description.AcceptableValues;
            Check(acceptable == null || acceptable.IsValid(entry.DefaultValue), "Default within supported range: " + label);
            if (entry.SettingType == typeof(int))
            {
                Check(acceptable is AcceptableValueRange<int>, "Integer range: " + label);
                var range = (AcceptableValueRange<int>)acceptable!;
                entry.BoxedValue = int.MinValue; Check(Equals(entry.BoxedValue, range.MinValue), "Lower clamp: " + label);
                entry.BoxedValue = int.MaxValue; Check(Equals(entry.BoxedValue, range.MaxValue), "Upper clamp: " + label);
                entry.BoxedValue = range.MinValue + (range.MaxValue - range.MinValue) / 3;
            }
            else if (entry.SettingType == typeof(float))
            {
                Check(acceptable is AcceptableValueRange<float>, "Float range: " + label);
                var range = (AcceptableValueRange<float>)acceptable!;
                foreach (float invalid in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity })
                {
                    entry.BoxedValue = invalid;
                    Check(float.IsFinite((float)entry.BoxedValue) && acceptable!.IsValid(entry.BoxedValue), "Non-finite clamp: " + label);
                }
                entry.BoxedValue = range.MinValue + (range.MaxValue - range.MinValue) / 3f;
            }
            else if (entry.SettingType == typeof(bool)) entry.BoxedValue = !(bool)entry.DefaultValue;
            else if (acceptable is AcceptableValueList<string> list)
                entry.BoxedValue = list.AcceptableValues.Last();
            saved.Add(pair.Key, entry.BoxedValue);
        }
        config.NegotiatorDeliveryCarryDistance.Value = 37.5f;
        config.NegotiatorDeliveryAllowedSizes.Value = "small, Big,unknown";
        config.NegotiatorEnemyTier.Value = 2;
        config.SniperReferenceDistance.Value = 20;
        config.SniperMaximumMultiplierDistance.Value = 5;
        Check(config.SniperEffectiveMaximumDistance == 20.1f, "Sniper reports the same effective distance used by damage");
        foreach (var pair in file) saved[pair.Key] = pair.Value.BoxedValue;
        file.Save();
        var reloaded = new ConfigFile(path, false) { SaveOnConfigSet = false };
        var loaded = new StageRolesConfig(reloaded);
        Check(reloaded.Count == file.Count, "Reload keeps every setting");
        foreach (var pair in reloaded)
            Check(saved.TryGetValue(pair.Key, out object? expected) && Equals(pair.Value.BoxedValue, expected), "Custom value survives reload: " + pair.Key);
        Check(loaded.NegotiatorDeliveryCarryDistance.Value == 37.5f && loaded.NegotiatorEnemyTier.Value == 2 &&
            loaded.NegotiatorDeliveryAllowedSizes.Value == "small, Big,unknown", "New Negotiator settings retain user input");
        Console.WriteLine($"Configuration audit: {file.Count} bindings, {checks} range/default/finite/persistence checks passed.");
        return checks;
    }
}
