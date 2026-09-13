namespace UnityEngine { public static class Time { public static float unscaledTime; } }
namespace Photon.Pun
{
    public sealed class PhotonView { }
    public struct PhotonMessageInfo { public bool Rejected; }
}
public sealed class MenuPage { }
public sealed class MenuManager
{
    public static MenuManager? instance;
    private MenuPage? currentMenuPage;
    public MenuPage? PageForTests { get => currentMenuPage; set => currentMenuPage = value; }
}
public sealed class PlayerExpression
{
    public PlayerAvatar playerAvatar;
    public PlayerExpression(PlayerAvatar avatar) { playerAvatar = avatar; }
}
public sealed class PlayerAvatar
{
    private bool isLocal = true;
    public bool IsLocalForTests { get => isLocal; set => isLocal = value; }
    public Photon.Pun.PhotonView photonView = new();
    public PlayerExpression playerExpression;
    public PlayerAvatar() { playerExpression = new(this); }
    public void PlayerExpressionSetRPC(int index, float percent, Photon.Pun.PhotonMessageInfo info) { }
    public void PlayerExpressionStopRPC(int index, Photon.Pun.PhotonMessageInfo info) { }
}
public static class SemiFunc
{
    public static bool OwnerOnlyRPC(Photon.Pun.PhotonMessageInfo info, Photon.Pun.PhotonView view) => !info.Rejected;
}
namespace REPOJP.StageRoles
{
    internal sealed class StageRolesPlugin
    {
        internal static StageRolesPlugin Instance = new();
        internal StageRoleController Controller = new();
    }
    internal sealed class StageRoleController
    {
        internal List<(PlayerAvatar Player, int Index)> Calls = new();
        internal void TryHandleRoleExpression(PlayerAvatar player, int index) => Calls.Add((player, index));
    }
}
