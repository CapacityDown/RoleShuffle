using System;
using System.Collections.Generic;
using UnityEngine;

namespace REPOJP.StageRoles;

internal sealed class NegotiatorRuntime : MonoBehaviour
{
    private readonly NegotiatorState _state = new();
    private readonly HashSet<PlayerAvatar> _announced = new();
    private StageRolesConfig _config = null!;
    private ShopKeeper? _keeper;
    private PhysGrabObject? _item;
    private string _reservedId = string.Empty;
    private float _refreshAt;
    internal void Initialize(StageRolesConfig config) => _config = config;
    internal void BeginStage() => _state.BeginStage();
    internal void Capture(IReadOnlyList<RoleAssignment> assignments)
    {
        if (!_config.Enabled.Value || !SemiFunc.IsMasterClientOrSingleplayer()) return;
        foreach (RoleAssignment assignment in assignments)
        {
            if (RoleCatalog.HasCapability(assignment.Role, StageRole.Negotiator)) _state.Grant(assignment.SteamId);
            else RoleChanged(assignment);
        }
    }
    internal void RoleChanged(RoleAssignment assignment)
    {
        if (!RoleCatalog.HasCapability(assignment.Role, StageRole.Negotiator))
            Revoke(assignment.SteamId);
    }
    internal void PlayerDied(PlayerAvatar player)
    {
        if (player != null) Revoke(PlayerIdentity.SteamId(player));
    }
    private void Revoke(string id)
    {
        if (!SemiFunc.IsMasterClientOrSingleplayer() || !_state.Revoke(id)) return;
        if (string.Equals(_reservedId, id, StringComparison.Ordinal))
        {
            _keeper = null; _item = null; _reservedId = string.Empty;
        }
        _refreshAt = 0;
    }
    internal int Remaining(string id) => _state.Remaining(id);
    internal bool HasShopRole(PlayerAvatar player) => _state.InShop && _state.Has(PlayerIdentity.SteamId(player));
    internal bool CanUse(PlayerAvatar? player) => _config.Enabled.Value && _state.InShop &&
        SemiFunc.IsMasterClientOrSingleplayer() && PlayerState.IsLiving(player) &&
        _state.Remaining(PlayerIdentity.SteamId(player)) > 0 && _keeper == null;
    internal void Reserve(ShopKeeper keeper, PhysGrabObject item, PlayerAvatar player)
    {
        if (!CanUse(player)) return;
        _keeper = keeper; _item = item; _reservedId = PlayerIdentity.SteamId(player);
    }
    internal bool Guaranteed(ShopKeeper keeper, PhysGrabObject? item) => _config.Enabled.Value &&
        _state.InShop && SemiFunc.IsMasterClientOrSingleplayer() && _keeper == keeper &&
        _item != null && _item == item && _state.Remaining(_reservedId) > 0;
    internal void Complete(ShopKeeper keeper, PhysGrabObject? item)
    {
        if (!Guaranteed(keeper, item) || !_state.Consume(_reservedId)) return;
        foreach (PlayerAvatar player in SemiFunc.PlayerGetList())
            if (PlayerIdentity.SteamId(player) == _reservedId)
                StageRolesPlugin.Instance.Controller.NotifyShopMessage(player, "Discounted");
        _refreshAt = 0;
    }
    internal void Release(ShopKeeper keeper)
    {
        if (_keeper != keeper) return;
        _keeper = null; _item = null; _reservedId = string.Empty;
    }
    internal void SceneChanged()
    {
        _state.LeaveShop();
        _keeper = null; _item = null; _reservedId = string.Empty;
        _announced.Clear(); _refreshAt = 0;
    }
    internal void Reset()
    {
        _state.Reset(); SceneChanged();
    }
    private void Update()
    {
        if (_config == null || GameManager.instance == null || RunManager.instance == null) return;
        if (!_config.Enabled.Value || !SemiFunc.IsMasterClientOrSingleplayer() ||
            SemiFunc.IsMainMenu() || SemiFunc.RunIsLobbyMenu() || SemiFunc.RunIsArena())
        { Reset(); return; }
        if (!SemiFunc.RunIsShop()) return;
        // Wait for generation: the level descriptor may change before scene load.
        if (ShopKeeper.instance == null || LevelGenerator.Instance == null || !SemiFunc.LevelGenDone()) return;
        _state.EnterShop();
        if (Time.unscaledTime < _refreshAt) return;
        _refreshAt = Time.unscaledTime + 0.5f;
        List<RoleSnapshot> roles = new();
        List<AbilitySnapshot> abilities = new();
        foreach (PlayerAvatar player in SemiFunc.PlayerGetList())
        {
            if (player == null) continue;
            string id = PlayerIdentity.SteamId(player);
            if (!_state.Has(id)) continue;
            roles.Add(new RoleSnapshot(id, PlayerIdentity.Name(player), StageRole.Negotiator, StageRole.Negotiator));
            abilities.Add(new AbilitySnapshot(id, StageRole.Negotiator, StageRole.Negotiator,
                new[] { new AbilityValue(AbilityMetric.Negotiation, _state.Remaining(id), 1) }));
            if (PlayerState.IsLiving(player) && RoleNotifier.CanNotify() && _announced.Add(player))
                StageRolesPlugin.Instance.Controller.NotifyShopMessage(player, "Negotiator");
        }
        RoleAssignmentSync.PublishSnapshots(roles);
        RoleAbilitySync.Publish(abilities);
    }
}

internal sealed partial class StageRoleController
{
    internal void NotifyShopMessage(PlayerAvatar player, string message) =>
        _notifier.NotifyConditional(player, message, () =>
            StageRolesPlugin.Instance.Negotiator.HasShopRole(player));
}
