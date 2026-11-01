using System;
using ExitGames.Client.Photon;
using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace REPOJP.StageRoles;

internal static class RoleDrawEligibilitySync
{
    private const string Property = "RoleShuffleDrawEligibilityV1";
    private static float _nextUpdate;
    private static string? _local, _decodedText;
    private static RoleDrawSnapshot? _decoded;
    private static object? _room;
    private static string _source = string.Empty;

    private static string Source => SceneManager.GetActiveScene().name + ":" +
        (SemiFunc.IsMultiplayer() ? PhotonNetwork.MasterClient?.ActorNumber ?? -1 : 0);
    internal static void Invalidate() { _nextUpdate = 0; _local = null; _decodedText = null; _decoded = null; }
    internal static void Tick(StageRoleController controller)
    {
        if (GameManager.instance == null) return;
        string source = Source;
        if (!ReferenceEquals(_room, PhotonNetwork.CurrentRoom) || _source != source)
        { _room = PhotonNetwork.CurrentRoom; _source = source; Invalidate(); }
        if (!SemiFunc.IsMasterClientOrSingleplayer() || Time.unscaledTime < _nextUpdate) return;
        _nextUpdate = Time.unscaledTime + 2f;
        try
        {
            string next = RoleDrawCodec.Encode(controller.BuildDrawEligibility(source));
            _local = next;
            if (SemiFunc.IsMultiplayer() && PhotonNetwork.CurrentRoom != null &&
                (!PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(Property, out object? old) || !(old is string text) || text != next))
                PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable { [Property] = next });
        }
        catch (Exception exception)
        {
            _local = null;
            StageRolesPlugin.ModLogger.LogDebug($"Draw eligibility preview unavailable: {exception.Message}");
            if (SemiFunc.IsMultiplayer() && PhotonNetwork.CurrentRoom != null && PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(Property))
            {
                try { PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable { [Property] = null }); }
                catch (Exception) { /* Retry on the next refresh. */ }
            }
        }
    }
    internal static RoleDrawSnapshot? Read()
    {
        string? text = _local;
        if (SemiFunc.IsMultiplayer() && !PhotonNetwork.IsMasterClient)
            text = PhotonNetwork.CurrentRoom != null && PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(Property, out object? raw)
                ? raw as string : null;
        if (text != _decodedText) { _decodedText = text; _decoded = RoleDrawCodec.Decode(text); }
        return _decoded?.Source == Source && (!_decoded.StageKnown || SemiFunc.LevelGenDone()) ? _decoded : null;
    }
    internal static string Signature
    {
        get { RoleDrawSnapshot? snapshot = Read(); return snapshot == null ? "unavailable" : _decodedText!; }
    }
}
