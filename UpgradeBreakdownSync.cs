using System;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace REPOJP.StageRoles;
// Requests/replies are targeted, uncached Photon events. Never broadcast account IDs
// or the party's upgrade details; native guests need no additional component.
internal sealed class UpgradeBreakdownSync : MonoBehaviour, IOnEventCallback
{
    private const byte EventCode = 191;
    private const string Magic = "RoleShuffle.UpgradeDetails.1";
    private static UpgradeBreakdownSync? _instance;
    private StageRoleController _controller = null!;
    private object? _room;
    private string _scene = "", _nonce = "", _payload = "";
    private int _host;
    private float _nextRequest, _receivedAt;
    private UpgradeBreakdownSnapshot? _snapshot;
    private readonly Dictionary<int, float> _replies = new();
    internal void Initialize(StageRoleController controller) { _instance = this; _controller = controller; }
    private void OnEnable() { PhotonNetwork.AddCallbackTarget(this); if (_controller != null) _instance = this; }
    private void OnDisable() { PhotonNetwork.RemoveCallbackTarget(this); if (_instance == this) _instance = null; _snapshot = null; _nonce = _payload = ""; _nextRequest = 0; }
    private void CheckContext()
    {
        object? room = PhotonNetwork.CurrentRoom;
        string scene = SceneManager.GetActiveScene().name;
        int host = SemiFunc.IsMultiplayer() ? PhotonNetwork.MasterClient?.ActorNumber ?? -1 : 0;
        if (!ReferenceEquals(room, _room) || scene != _scene || host != _host)
        { _room = room; _scene = scene; _host = host; _nonce = _payload = ""; _snapshot = null; _nextRequest = 0; _replies.Clear(); }
    }
    internal static UpgradeBreakdownSnapshot? ReadLocalPlayer()
    {
        var self = _instance;
        if (self == null || GameManager.instance == null) return null;
        self.CheckContext();
        if (Time.unscaledTime >= self._nextRequest)
        {
            self._nextRequest = Time.unscaledTime + 1f;
            try
            {
                if (SemiFunc.IsMasterClientOrSingleplayer())
                {
                    self._snapshot = self._controller.BuildUpgradeBreakdown(SemiFunc.PlayerGetLocal());
                    self._payload = self._snapshot == null ? "" : UpgradeBreakdownCodec.Encode(self._snapshot);
                    self._receivedAt = Time.unscaledTime;
                }
                else if (PhotonNetwork.InRoom && self._host > 0)
                {
                    self._nonce = Guid.NewGuid().ToString("N");
                    Send(self._host, new object[] { Magic, "request", self._nonce, self._scene });
                    self._nextRequest = Time.unscaledTime + 3f; // Allow a slow reply before replacing its nonce.
                }
            }
            catch (Exception error) { StageRolesPlugin.ModLogger.LogDebug("Upgrade details unavailable: " + error.Message); }
        }
        return Time.unscaledTime - self._receivedAt <= 5f ? self._snapshot : null;
    }
    internal static string Signature => ReadLocalPlayer() == null ? "unavailable" : _instance!._payload;
    private static void Send(int actor, object[] data) => PhotonNetwork.RaiseEvent(EventCode, data,
        new RaiseEventOptions { TargetActors = new[] { actor } }, SendOptions.SendReliable);
    public void OnEvent(EventData data)
    {
        if (data.Code != EventCode || data.CustomData is not object[] args || args.Length != 4 ||
            args[0] is not string magic || magic != Magic || args[1] is not string kind ||
            args[2] is not string nonce || nonce.Length != 32 || args[3] is not string body) return;
        CheckContext();
        try
        {
            if (kind == "request" && PhotonNetwork.IsMasterClient && body == _scene && PhotonNetwork.CurrentRoom != null &&
                PhotonNetwork.CurrentRoom.Players.ContainsKey(data.Sender))
            {
                if (_replies.TryGetValue(data.Sender, out float after) && Time.unscaledTime < after) return;
                _replies[data.Sender] = Time.unscaledTime + 0.8f;
                var snapshot = _controller.BuildUpgradeBreakdownForActor(data.Sender);
                if (snapshot != null) Send(data.Sender, new object[] { Magic, "reply", nonce, UpgradeBreakdownCodec.Encode(snapshot) });
            }
            else if (kind == "reply" && !PhotonNetwork.IsMasterClient && data.Sender == _host && nonce == _nonce)
            {
                var snapshot = UpgradeBreakdownCodec.Decode(body);
                if (snapshot == null) return;
                _snapshot = snapshot; _payload = body; _receivedAt = Time.unscaledTime;
                _nonce = ""; _nextRequest = Time.unscaledTime + 1f;
            }
        }
        catch (Exception error) { StageRolesPlugin.ModLogger.LogDebug("Upgrade detail message ignored: " + error.Message); }
    }
}
