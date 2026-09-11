namespace UnityEngine { public static class Time { public static float unscaledTime; } }
namespace Photon.Pun { public sealed class PhotonView { public bool IsMine; } }
public sealed class PlayerAvatar
{
    public Photon.Pun.PhotonView photonView = new();
    public PlayerHealth playerHealth = new();
}
public sealed class PlayerHealth
{
    public int Health = 50;
    public int Maximum = 100;
    public Action<int>? OnHeal;
    public void HealOther(int amount, bool effect)
    {
        if (OnHeal != null) OnHeal(amount);
        else Health = Math.Min(Maximum, Health + amount);
    }
}
public static class SemiFunc
{
    public static bool Multiplayer;
    public static bool IsMultiplayer() => Multiplayer;
}
namespace REPOJP.StageRoles
{
    internal static class PlayerState
    {
        internal static bool TryGetCurrentHealth(PlayerAvatar player, out int health)
        { health = player.playerHealth.Health; return health >= 0; }
        internal static bool TryGetMaximumHealth(PlayerAvatar player, out int health)
        { health = player.playerHealth.Maximum; return health > 0; }
    }
}
