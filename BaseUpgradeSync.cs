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
        int truckDrawBonus,
        int manualAdjustment = 0)
    {
        Name = name;
        CurrentLevel = currentLevel;
        ConfiguredLevel = configuredLevel;
        TruckDrawBonus = truckDrawBonus;
        ManualAdjustment = manualAdjustment;
    }

    internal string Name { get; }
    internal int CurrentLevel { get; }
    internal int ConfiguredLevel { get; }
    internal int TruckDrawBonus { get; }
    internal int ManualAdjustment { get; }
}

internal static class BaseUpgradeSync
{
    private const string PropertyKey = "RS.BaseUpgrades";
    private const string DetailPropertyKey = "RS.BaseUpgrades.Detail";
    private const char EntrySeparator = ';';
    private const char FieldSeparator = ',';

    internal static void Publish(StageRolesConfig config)
    {
        if (!SemiFunc.IsMultiplayer() || !PhotonNetwork.IsMasterClient ||
            PhotonNetwork.CurrentRoom == null)
        {
            return;
        }
        Hashtable properties = new()
        {
            [PropertyKey] = LocalSignature(config),
            [BaseUpgradeSettingsSync.PropertyKey] = BaseUpgradeSettingsSync.LocalSignature(config)
        };
        AddDetails(properties, config);
        PhotonNetwork.CurrentRoom.SetCustomProperties(properties);
    }

    // Keep the four-field payload (and existing sync stamp) readable by older
    // clients. The extension is accepted only for the same host and base data.
    internal static void AddDetails(Hashtable properties, StageRolesConfig config)
    {
        string detail = (PhotonNetwork.LocalPlayer?.ActorNumber ?? 0) + "|" + Serialize(BuildLocal(config), true);
        if (PhotonNetwork.CurrentRoom == null ||
            !PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(DetailPropertyKey, out object old) ||
            !string.Equals(old as string, detail, StringComparison.Ordinal))
            properties[DetailPropertyKey] = detail;
    }

    internal static string LocalSignature(StageRolesConfig config) =>
        Serialize(BuildLocal(config));

    internal static string LocalPublishSignature(StageRolesConfig config) =>
        CurrentSignature(config) + "|" + BaseUpgradeSettingsSync.LocalSignature(config);

    internal static string CurrentSignature(StageRolesConfig config)
    {
        var upgrades = Read(config);
        return upgrades.Count == 0 ? "unavailable" : Serialize(upgrades, true);
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
            if (PhotonNetwork.CurrentRoom!.CustomProperties.TryGetValue(DetailPropertyKey, out object raw) &&
                raw is string detail)
            {
                int separator = detail.IndexOf('|');
                if (separator > 0 && int.TryParse(detail.Substring(0, separator), out int actor) &&
                    actor == (PhotonNetwork.MasterClient?.ActorNumber ?? 0) &&
                    TryParse(detail.Substring(separator + 1), out var detailed, true) &&
                    Serialize(detailed) == payload)
                    return detailed;
            }
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
                BaseUpgradeBonusStore.Get(upgrade.DictionaryName),
                BaseUpgradeManualStore.Get(upgrade.DictionaryName)));
        }
        return result;
    }

    private static string Serialize(
        IReadOnlyList<BaseUpgradeSnapshot> upgrades, bool includeManual = false)
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
            if (includeManual) payload.Append(FieldSeparator).Append(upgrade.ManualAdjustment);
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
        out List<BaseUpgradeSnapshot> upgrades, bool includeManual = false)
    {
        upgrades = new List<BaseUpgradeSnapshot>();
        try
        {
            foreach (string entry in payload.Split(EntrySeparator))
            {
                string[] fields = entry.Split(FieldSeparator);
                if (fields.Length != (includeManual ? 5 : 4) ||
                    string.IsNullOrWhiteSpace(fields[0]) ||
                    !int.TryParse(fields[1], out int current) ||
                    !int.TryParse(fields[2], out int configured) ||
                    !int.TryParse(fields[3], out int bonus))
                {
                    if (includeManual) return false;
                    continue;
                }
                int manual = 0;
                if (includeManual && !int.TryParse(fields[4], out manual)) return false;
                upgrades.Add(new BaseUpgradeSnapshot(
                    fields[0],
                    Math.Max(0, current),
                    Math.Max(0, configured),
                    bonus, manual));
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
