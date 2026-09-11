using System;
using System.Collections.Generic;
using System.Text;
using ExitGames.Client.Photon;
using Photon.Pun;

namespace REPOJP.StageRoles;

internal readonly struct RoleSnapshot
{
    internal RoleSnapshot(
        string steamId,
        string playerName,
        StageRole assignedRole,
        StageRole effectiveRole,
        int playerNumber = 0)
    {
        SteamId = steamId;
        PlayerName = playerName;
        Role = assignedRole;
        EffectiveRole = effectiveRole;
        PlayerNumber = playerNumber;
    }

    internal string SteamId { get; }
    internal string PlayerName { get; }
    internal StageRole Role { get; }
    internal StageRole EffectiveRole { get; }
    internal int PlayerNumber { get; }
}

internal static class RoleAssignmentSync
{
    private const string RoleMapKey = "RS.RoleMap";
    private static string _localRoleMap = string.Empty;
    private static int _previewPlayerCount;
    private static string? _cachedPayload;
    private static int _cachedPreviewCount = -1;
    private static string _cachedLocalId = string.Empty;
    private static string _cachedLocalName = string.Empty;
    private static IReadOnlyList<RoleSnapshot> _cachedSnapshots = Array.Empty<RoleSnapshot>();

    internal static int PreviewPlayerCount => _previewPlayerCount;
    internal static string LocalPayload => _localRoleMap;

    internal static void SetPreviewPlayerCount(int count)
    {
        _previewPlayerCount = Math.Max(
            1,
            Math.Min(StageRolesConfig.MaximumSupportedPlayers, count));
    }

    internal static void ClearPreview()
    {
        _previewPlayerCount = 0;
    }

    internal static void Publish(IReadOnlyList<RoleAssignment> assignments)
    {
        List<string> records = new();
        foreach (RoleAssignment assignment in assignments)
        {
            if (!string.IsNullOrEmpty(assignment.SteamId))
            {
                string playerName = PlayerIdentity.Name(assignment.Player);
                string encodedName = Convert.ToBase64String(
                    Encoding.UTF8.GetBytes(playerName ?? string.Empty));
                records.Add(
                    $"{assignment.SteamId},{(int)assignment.AssignedRole}," +
                    $"{(int)assignment.Role},{encodedName}");
            }
        }
        _localRoleMap = string.Join(";", records);

        if (SemiFunc.IsMultiplayer() && PhotonNetwork.IsMasterClient &&
            PhotonNetwork.CurrentRoom != null)
        {
            PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable
            {
                [RoleMapKey] = _localRoleMap
            });
        }
    }

    internal static void Clear()
    {
        _localRoleMap = string.Empty;
        ResetCache();
        if (SemiFunc.IsMultiplayer() && PhotonNetwork.IsMasterClient &&
            PhotonNetwork.CurrentRoom != null)
        {
            PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable
            {
                [RoleMapKey] = null
            });
        }
    }

    internal static IReadOnlyList<RoleSnapshot> Read()
    {
        string serialized = _localRoleMap;
        if (SemiFunc.IsMultiplayer() && PhotonNetwork.CurrentRoom != null &&
            PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(
                RoleMapKey,
                out object raw) && raw is string remote)
        {
            serialized = remote;
        }

        PlayerAvatar? localPlayer = _previewPlayerCount > 0 ? SemiFunc.PlayerGetLocal() : null;
        string localId = localPlayer != null ? PlayerIdentity.SteamId(localPlayer) : string.Empty;
        string localName = localPlayer != null ? PlayerIdentity.Name(localPlayer) : string.Empty;
        if (_cachedPayload == serialized && _cachedPreviewCount == _previewPlayerCount &&
            _cachedLocalId == localId && _cachedLocalName == localName)
        {
            return _cachedSnapshots;
        }

        List<RoleSnapshot> result = new();
        if (string.IsNullOrEmpty(serialized))
        {
            return CacheSnapshots(serialized, localId, localName, result);
        }
        foreach (string record in serialized.Split(';'))
        {
            string[] fields = record.Split(',');
            if ((fields.Length != 3 && fields.Length != 4) ||
                string.IsNullOrEmpty(fields[0]) ||
                !int.TryParse(fields[1], out int roleValue) ||
                !Enum.IsDefined(typeof(StageRole), roleValue))
            {
                continue;
            }
            int effectiveRoleValue = roleValue;
            int encodedNameIndex = 2;
            if (fields.Length == 4)
            {
                if (!int.TryParse(fields[2], out effectiveRoleValue) ||
                    !Enum.IsDefined(typeof(StageRole), effectiveRoleValue))
                {
                    continue;
                }
                encodedNameIndex = 3;
            }
            string playerName;
            try
            {
                playerName = Encoding.UTF8.GetString(
                    Convert.FromBase64String(fields[encodedNameIndex]));
            }
            catch
            {
                playerName = string.Empty;
            }
            result.Add(new RoleSnapshot(
                fields[0],
                playerName,
                (StageRole)roleValue,
                (StageRole)effectiveRoleValue,
                result.Count + 1));
        }
        return CacheSnapshots(serialized, localId, localName, result);
    }

    internal static void ResetCache()
    {
        _cachedPayload = null;
        _cachedPreviewCount = -1;
        _cachedLocalId = string.Empty;
        _cachedLocalName = string.Empty;
        _cachedSnapshots = Array.Empty<RoleSnapshot>();
    }

    private static IReadOnlyList<RoleSnapshot> CacheSnapshots(
        string payload, string localId, string localName, List<RoleSnapshot> actual)
    {
        _cachedSnapshots = ApplyPreview(actual, localId, localName).AsReadOnly();
        _cachedPayload = payload;
        _cachedPreviewCount = _previewPlayerCount;
        _cachedLocalId = localId;
        _cachedLocalName = localName;
        return _cachedSnapshots;
    }

    private static List<RoleSnapshot> ApplyPreview(
        List<RoleSnapshot> actual, string localSteamId, string localName)
    {
        if (_previewPlayerCount <= 0)
        {
            return actual;
        }

        int targetCount = Math.Max(
            1,
            Math.Min(
                StageRolesConfig.MaximumSupportedPlayers,
                _previewPlayerCount));
        List<RoleSnapshot> preview = new(targetCount);
        int localIndex = -1;
        for (int index = 0; index < actual.Count; index++)
        {
            if (!string.IsNullOrEmpty(localSteamId) &&
                string.Equals(
                    actual[index].SteamId,
                    localSteamId,
                    StringComparison.Ordinal))
            {
                localIndex = index;
                break;
            }
        }

        if (localIndex >= 0)
        {
            preview.Add(actual[localIndex]);
        }
        else if (actual.Count == 0 && !string.IsNullOrEmpty(localSteamId))
        {
            preview.Add(new RoleSnapshot(
                localSteamId,
                string.IsNullOrWhiteSpace(localName) ? "Local Player" : localName,
                StageRole.Tank,
                StageRole.Tank));
        }

        for (int index = 0;
             index < actual.Count && preview.Count < targetCount;
             index++)
        {
            if (index != localIndex)
            {
                preview.Add(actual[index]);
            }
        }

        int dummyNumber = 1;
        while (preview.Count < targetCount)
        {
            StageRole role = RoleCatalog.AllRoles[
                (preview.Count - actual.Count + RoleCatalog.AllRoles.Count) %
                RoleCatalog.AllRoles.Count];
            preview.Add(new RoleSnapshot(
                $"preview:{dummyNumber}",
                $"Preview Player {dummyNumber:00}",
                role,
                role));
            dummyNumber++;
        }
        return preview;
    }
}
