using System;
using System.Collections.Generic;
using System.Globalization;
using ExitGames.Client.Photon;
using Photon.Pun;

namespace REPOJP.StageRoles;

internal static class RoleAbilitySync
{
    private const string Key = "RS.Ability.v1";
    private static string _local = string.Empty;
    private static string _published = string.Empty;
    private static string _cached = string.Empty;
    private static object? _publishedRoom;
    private static double _publishedAt;
    private static IReadOnlyDictionary<string, AbilitySnapshot> _snapshots = RoleAbilityCodec.Decode("");

    internal static void Publish(IReadOnlyList<AbilitySnapshot> snapshots)
    {
        if (!SemiFunc.IsMasterClientOrSingleplayer()) return;
        string body = RoleAbilityCodec.Encode(snapshots);
        double now = PhotonNetwork.Time;
        object? room = PhotonNetwork.CurrentRoom;
        if (body == _published && ReferenceEquals(room, _publishedRoom) && now - _publishedAt < 5) return;
        string payload = now.ToString("R", CultureInfo.InvariantCulture) + "\n" + body;
        _local = payload;
        if (SemiFunc.IsMultiplayer() && PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom != null)
            PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable { [Key] = payload });
        _published = body;
        _publishedRoom = room;
        _publishedAt = now;
    }

    internal static AbilitySnapshot? Read(RoleSnapshot role)
    {
        string payload = _local;
        if (SemiFunc.IsMultiplayer())
        {
            payload = PhotonNetwork.CurrentRoom != null &&
                PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(Key, out object? raw) && raw is string remote
                ? remote : string.Empty;
        }
        if (payload.Length > RoleAbilityCodec.MaximumPayloadLength + 40) return null;
        int newline = payload.IndexOf('\n');
        if (newline < 0 || !double.TryParse(payload.Substring(0, newline), NumberStyles.Float,
            CultureInfo.InvariantCulture, out double sentAt) || double.IsNaN(sentAt) || double.IsInfinity(sentAt) ||
            PhotonNetwork.Time - sentAt > 15 || sentAt - PhotonNetwork.Time > 5) return null;
        if (_cached != payload)
        {
            _cached = payload;
            _snapshots = RoleAbilityCodec.Decode(payload.Substring(newline + 1));
        }
        return _snapshots.TryGetValue(role.SteamId, out AbilitySnapshot? state) &&
            state.Assigned == role.Role && state.Effective == role.EffectiveRole ? state : null;
    }

    internal static void Clear()
    {
        _local = _published = _cached = string.Empty;
        _publishedAt = 0;
        _publishedRoom = null;
        _snapshots = RoleAbilityCodec.Decode("");
        // Scene teardown can run after GameManager has been destroyed.
        if (PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom != null)
            PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable { [Key] = null });
    }
}
