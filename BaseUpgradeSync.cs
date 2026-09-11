using System;
using System.Collections.Generic;
using System.Text;
using ExitGames.Client.Photon;
using Photon.Pun;

namespace REPOJP.StageRoles;

internal readonly struct BaseUpgradeSnapshot
{
    internal BaseUpgradeSnapshot(
        string name,
        int currentLevel,
        int configuredLevel,
        int truckDrawBonus)
    {
        Name = name;
        CurrentLevel = currentLevel;
        ConfiguredLevel = configuredLevel;
        TruckDrawBonus = truckDrawBonus;
    }

    internal string Name { get; }
    internal int CurrentLevel { get; }
    internal int ConfiguredLevel { get; }
    internal int TruckDrawBonus { get; }
}

internal static class BaseUpgradeSync
{
    private const string PropertyKey = "RS.BaseUpgrades";
    private const char EntrySeparator = ';';
    private const char FieldSeparator = ',';

    internal static void Publish(StageRolesConfig config)
    {
        if (!SemiFunc.IsMultiplayer() || !PhotonNetwork.IsMasterClient ||
            PhotonNetwork.CurrentRoom == null)
        {
            return;
        }
        PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable
        {
            [PropertyKey] = LocalSignature(config)
        });
    }

    internal static string LocalSignature(StageRolesConfig config) =>
        Serialize(BuildLocal(config));

    internal static string CurrentSignature(StageRolesConfig config)
    {
        if (!SemiFunc.IsMultiplayer() || PhotonNetwork.IsMasterClient)
        {
            return LocalSignature(config);
        }
        return TryReadRemotePayload(out string payload)
            ? payload
            : "unavailable";
    }

    internal static IReadOnlyList<BaseUpgradeSnapshot> Read(
        StageRolesConfig config)
    {
        if (!SemiFunc.IsMultiplayer() || PhotonNetwork.IsMasterClient)
        {
            return BuildLocal(config);
        }
        if (TryReadRemotePayload(out string payload) &&
            TryParse(payload, out List<BaseUpgradeSnapshot> upgrades))
        {
            return upgrades;
        }
        return Array.Empty<BaseUpgradeSnapshot>();
    }

    private static List<BaseUpgradeSnapshot> BuildLocal(
        StageRolesConfig config)
    {
        IReadOnlyList<UpgradeGrant> configured =
            RoleCatalog.ConfiguredBaseUpgrades(config);
        IReadOnlyList<UpgradeGrant> current = RoleCatalog.BaseUpgrades(config);
        Dictionary<string, UpgradeGrant> configuredByDictionary =
            new(StringComparer.Ordinal);
        foreach (UpgradeGrant upgrade in configured)
        {
            configuredByDictionary[upgrade.DictionaryName] = upgrade;
        }

        List<BaseUpgradeSnapshot> result = new(current.Count);
        foreach (UpgradeGrant upgrade in current)
        {
            int configuredLevel = configuredByDictionary.TryGetValue(
                upgrade.DictionaryName,
                out UpgradeGrant configuredUpgrade)
                    ? configuredUpgrade.Level
                    : 0;
            result.Add(new BaseUpgradeSnapshot(
                upgrade.CommandName,
                upgrade.Level,
                configuredLevel,
                BaseUpgradeBonusStore.Get(upgrade.DictionaryName)));
        }
        return result;
    }

    private static string Serialize(
        IReadOnlyList<BaseUpgradeSnapshot> upgrades)
    {
        StringBuilder payload = new();
        foreach (BaseUpgradeSnapshot upgrade in upgrades)
        {
            if (payload.Length > 0)
            {
                payload.Append(EntrySeparator);
            }
            payload.Append(upgrade.Name)
                .Append(FieldSeparator)
                .Append(upgrade.CurrentLevel)
                .Append(FieldSeparator)
                .Append(upgrade.ConfiguredLevel)
                .Append(FieldSeparator)
                .Append(upgrade.TruckDrawBonus);
        }
        return payload.ToString();
    }

    private static bool TryReadRemotePayload(out string payload)
    {
        payload = string.Empty;
        return PhotonNetwork.CurrentRoom != null &&
               PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(
                   PropertyKey,
                   out object value) &&
               value is string serialized &&
               !string.IsNullOrEmpty(payload = serialized);
    }

    private static bool TryParse(
        string payload,
        out List<BaseUpgradeSnapshot> upgrades)
    {
        upgrades = new List<BaseUpgradeSnapshot>();
        try
        {
            foreach (string entry in payload.Split(EntrySeparator))
            {
                string[] fields = entry.Split(FieldSeparator);
                if (fields.Length != 4 ||
                    string.IsNullOrWhiteSpace(fields[0]) ||
                    !int.TryParse(fields[1], out int current) ||
                    !int.TryParse(fields[2], out int configured) ||
                    !int.TryParse(fields[3], out int bonus))
                {
                    continue;
                }
                upgrades.Add(new BaseUpgradeSnapshot(
                    fields[0],
                    Math.Max(0, current),
                    Math.Max(0, configured),
                    bonus));
            }
        }
        catch (Exception exception)
        {
            StageRolesPlugin.ModLogger.LogDebug(
                $"Base Upgrade status could not be read: {exception.Message}");
            upgrades.Clear();
            return false;
        }
        return upgrades.Count > 0;
    }
}
