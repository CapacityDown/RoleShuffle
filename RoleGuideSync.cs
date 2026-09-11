using System;
using System.Collections.Generic;
using System.Text;
using ExitGames.Client.Photon;
using Photon.Pun;

namespace REPOJP.StageRoles;

internal static class RoleGuideSync
{
    private const string LegacyPropertyKey = "RoleShuffleGuideV1";
    private const string PropertyKey = "RoleShuffleGuideV2";
    private const string EnabledPropertyKey = "RoleShuffleGuideEnabledV1";
    private static string _cachedEnabledPayload = string.Empty;
    private static string? _remoteEnabledPayload;
    private static HashSet<StageRole>? _remoteEnabledRoles;
    private const char EntrySeparator = ';';
    private const char FieldSeparator = ':';
    private static StageRolesConfig? _cachedConfig;
    private static string _cachedPayload = string.Empty;
    private static string _cachedLegacyPayload = string.Empty;
    private static IReadOnlyDictionary<StageRole, string>? _cachedEnglish;
    private static IReadOnlyDictionary<StageRole, string>? _cachedJapanese;
    private static string? _remotePayload;
    private static string? _remoteLegacyPayload;
    private static RoleGuideLanguage _remoteLanguage;
    private static IReadOnlyDictionary<StageRole, string>? _remoteDescriptions;
    private static string? _signaturePayload;
    private static string? _signatureLegacyPayload;
    private static string _remoteSignature = "generic";

    internal static void Invalidate()
    {
        _cachedConfig = null;
        _cachedPayload = string.Empty;
        _cachedLegacyPayload = string.Empty;
        _cachedEnglish = null;
        _cachedJapanese = null;
        _remotePayload = null;
        _remoteLegacyPayload = null;
        _remoteDescriptions = null;
        _remoteEnabledPayload = null;
        _remoteEnabledRoles = null;
    }

    private static void EnsureCached(StageRolesConfig config)
    {
        if (ReferenceEquals(_cachedConfig, config))
        {
            return;
        }
        _cachedEnglish = LocalDescriptions(config, RoleGuideLanguage.English);
        _cachedJapanese = LocalDescriptions(config, RoleGuideLanguage.Japanese);
        _cachedPayload = Serialize(config);
        _cachedLegacyPayload = SerializeLegacy(config);
        StringBuilder enabled = new();
        foreach (StageRole role in RoleCatalog.AllRoles)
        {
            if (!config.RoleIsEnabled(role)) continue;
            if (enabled.Length > 0) enabled.Append(',');
            enabled.Append((int)role);
        }
        _cachedEnabledPayload = enabled.ToString();
        _cachedConfig = config;
    }

    internal static void Publish(StageRolesConfig config)
    {
        if (!SemiFunc.IsMultiplayer() || !PhotonNetwork.IsMasterClient ||
            PhotonNetwork.CurrentRoom == null)
        {
            return;
        }

        EnsureCached(config);
        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(PropertyKey, out object current) &&
            current is string payload && payload == _cachedPayload &&
            PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(LegacyPropertyKey, out object legacy) &&
            legacy is string legacyPayload && legacyPayload == _cachedLegacyPayload &&
            PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(EnabledPropertyKey, out object enabled) &&
            enabled is string enabledPayload && enabledPayload == _cachedEnabledPayload)
        {
            return;
        }
        PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable
        {
            [LegacyPropertyKey] = _cachedLegacyPayload,
            [PropertyKey] = _cachedPayload,
            [EnabledPropertyKey] = _cachedEnabledPayload
        });
    }

    internal static string VisibilitySignature(StageRolesConfig config)
    {
        if (!SemiFunc.IsMultiplayer() || PhotonNetwork.IsMasterClient)
        {
            EnsureCached(config);
            return _cachedEnabledPayload;
        }
        return PhotonNetwork.CurrentRoom != null &&
            PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(EnabledPropertyKey, out object value) &&
            value is string payload ? payload : "*";
    }

    internal static bool IsVisible(StageRole role, StageRolesConfig config)
    {
        if (!SemiFunc.IsMultiplayer() || PhotonNetwork.IsMasterClient)
            return config.RoleIsEnabled(role);

        string payload = VisibilitySignature(config);
        if (_remoteEnabledPayload != payload)
        {
            _remoteEnabledPayload = payload;
            _remoteEnabledRoles = null;
            if (payload != "*")
            {
                HashSet<StageRole> enabled = new();
                bool valid = true;
                if (payload.Length > 0)
                {
                    foreach (string entry in payload.Split(','))
                    {
                        if (!int.TryParse(entry, out int id)) { valid = false; break; }
                        enabled.Add((StageRole)id);
                    }
                }
                if (valid) _remoteEnabledRoles = enabled;
            }
        }
        // Older hosts do not publish visibility. Do not substitute guest settings.
        return _remoteEnabledRoles == null || _remoteEnabledRoles.Contains(role);
    }

    internal static string CurrentSignature(StageRolesConfig config)
    {
        if (!SemiFunc.IsMultiplayer() || PhotonNetwork.IsMasterClient)
        {
            EnsureCached(config);
            return _cachedPayload;
        }
        string? payload = null;
        string? legacyPayload = null;
        if (PhotonNetwork.CurrentRoom != null)
        {
            if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(PropertyKey, out object value))
                payload = value as string;
            if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(LegacyPropertyKey, out object legacy))
                legacyPayload = legacy as string;
        }
        if (_signaturePayload != payload || _signatureLegacyPayload != legacyPayload)
        {
            _signaturePayload = payload;
            _signatureLegacyPayload = legacyPayload;
            _remoteSignature = payload == null && legacyPayload == null
                ? "generic" : (payload ?? string.Empty) + "|legacy:" + legacyPayload;
        }
        return _remoteSignature;
    }

    private static string Serialize(StageRolesConfig config)
    {
        StringBuilder payload = new();
        foreach (StageRole role in RoleCatalog.AllRoles)
        {
            if (payload.Length > 0)
            {
                payload.Append(EntrySeparator);
            }
            string english = Convert.ToBase64String(
                Encoding.UTF8.GetBytes(_cachedEnglish![role]));
            string japanese = Convert.ToBase64String(
                Encoding.UTF8.GetBytes(_cachedJapanese![role]));
            payload.Append((int)role)
                .Append(FieldSeparator)
                .Append(english)
                .Append(FieldSeparator)
                .Append(japanese);
        }
        return payload.ToString();
    }

    private static string SerializeLegacy(StageRolesConfig config)
    {
        StringBuilder payload = new();
        foreach (StageRole role in RoleCatalog.AllRoles)
        {
            if (payload.Length > 0)
            {
                payload.Append(EntrySeparator);
            }
            string encoded = Convert.ToBase64String(
                Encoding.UTF8.GetBytes(_cachedEnglish![role]));
            payload.Append((int)role)
                .Append(FieldSeparator)
                .Append(encoded);
        }
        return payload.ToString();
    }

    internal static IReadOnlyDictionary<StageRole, string> Read(
        StageRolesConfig config,
        RoleGuideLanguage language)
    {
        if (!SemiFunc.IsMultiplayer() || PhotonNetwork.IsMasterClient)
        {
            EnsureCached(config);
            return language == RoleGuideLanguage.Japanese ? _cachedJapanese! : _cachedEnglish!;
        }

        string? currentPayload = null;
        string? currentLegacyPayload = null;
        if (PhotonNetwork.CurrentRoom != null)
        {
            if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(PropertyKey, out object current))
                currentPayload = current as string;
            if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(LegacyPropertyKey, out object legacy))
                currentLegacyPayload = legacy as string;
        }
        if (_remoteDescriptions != null && _remotePayload == currentPayload &&
            _remoteLegacyPayload == currentLegacyPayload && _remoteLanguage == language)
        {
            return _remoteDescriptions;
        }
        _remoteDescriptions = ReadRemote(language);
        _remotePayload = currentPayload;
        _remoteLegacyPayload = currentLegacyPayload;
        _remoteLanguage = language;
        return _remoteDescriptions;
    }

    private static IReadOnlyDictionary<StageRole, string> ReadRemote(RoleGuideLanguage language)
    {
        if (PhotonNetwork.CurrentRoom != null &&
            PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(
            PropertyKey,
                out object value) &&
            value is string payload &&
            TryParse(
                payload,
                language,
                out Dictionary<StageRole, string> descriptions))
        {
            return descriptions;
        }

        if (language == RoleGuideLanguage.English &&
            PhotonNetwork.CurrentRoom != null &&
            PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(
                LegacyPropertyKey,
                out object legacyValue) &&
            legacyValue is string legacyPayload &&
            TryParseLegacy(
                legacyPayload,
                out Dictionary<StageRole, string> legacyDescriptions))
        {
            return legacyDescriptions;
        }

        return GenericDescriptions(language);
    }

    private static Dictionary<StageRole, string> LocalDescriptions(
        StageRolesConfig config,
        RoleGuideLanguage language)
    {
        Dictionary<StageRole, string> descriptions = new();
        foreach (StageRole role in RoleCatalog.AllRoles)
        {
            descriptions[role] = RoleGuideCatalog.Description(
                role,
                config,
                language);
        }
        return descriptions;
    }

    private static Dictionary<StageRole, string> GenericDescriptions(
        RoleGuideLanguage language)
    {
        Dictionary<StageRole, string> descriptions = new();
        foreach (StageRole role in RoleCatalog.AllRoles)
        {
            descriptions[role] = RoleGuideCatalog.GenericDescription(
                role,
                language);
        }
        return descriptions;
    }

    private static bool TryParse(
        string payload,
        RoleGuideLanguage language,
        out Dictionary<StageRole, string> descriptions)
    {
        descriptions = new Dictionary<StageRole, string>();
        try
        {
            foreach (string entry in payload.Split(EntrySeparator))
            {
                string[] fields = entry.Split(FieldSeparator);
                if (fields.Length != 3 ||
                    !int.TryParse(
                        fields[0],
                        out int roleValue) ||
                    !Enum.IsDefined(typeof(StageRole), roleValue))
                {
                    continue;
                }
                string encoded = language == RoleGuideLanguage.Japanese
                    ? fields[2]
                    : fields[1];
                descriptions[(StageRole)roleValue] = Encoding.UTF8.GetString(
                    Convert.FromBase64String(encoded));
            }
        }
        catch (Exception exception)
        {
            StageRolesPlugin.ModLogger.LogWarning(
                $"Role guide settings could not be read: {exception.Message}");
            descriptions.Clear();
            return false;
        }
        return descriptions.Count == RoleCatalog.AllRoles.Count;
    }

    private static bool TryParseLegacy(
        string payload,
        out Dictionary<StageRole, string> descriptions)
    {
        descriptions = new Dictionary<StageRole, string>();
        try
        {
            foreach (string entry in payload.Split(EntrySeparator))
            {
                int separatorIndex = entry.IndexOf(FieldSeparator);
                if (separatorIndex <= 0 ||
                    !int.TryParse(
                        entry.Substring(0, separatorIndex),
                        out int roleValue) ||
                    !Enum.IsDefined(typeof(StageRole), roleValue))
                {
                    continue;
                }
                descriptions[(StageRole)roleValue] = Encoding.UTF8.GetString(
                    Convert.FromBase64String(entry.Substring(separatorIndex + 1)));
            }
        }
        catch (Exception exception)
        {
            StageRolesPlugin.ModLogger.LogWarning(
                $"Legacy role guide settings could not be read: {exception.Message}");
            descriptions.Clear();
            return false;
        }
        return descriptions.Count == RoleCatalog.AllRoles.Count;
    }
}
