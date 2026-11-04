using REPOJP.StageRoles;
namespace UnityEngine { public class MonoBehaviour {} public static class Time { public static float unscaledTime; } }
namespace UnityEngine.SceneManagement { public record Scene(string name); public static class SceneManager { public static string Name="stage"; public static Scene GetActiveScene()=>new(Name); } }
namespace ExitGames.Client.Photon { public class EventData { public byte Code=191; public object CustomData=null!; public int Sender; } public struct SendOptions { public static SendOptions SendReliable; } }
namespace Photon.Realtime { public interface IOnEventCallback { void OnEvent(ExitGames.Client.Photon.EventData data); } public class RaiseEventOptions { public int[] TargetActors=null!; } }
namespace Photon.Pun {
public class Peer(int id) { public int ActorNumber=id; }
public class Room { public Dictionary<int,Peer> Players=new() { [1]=new(1),[2]=new(2),[3]=new(3) }; }
public static class PhotonNetwork {
public static bool IsMasterClient=true; public static Peer? MasterClient=new(1); public static Room? CurrentRoom=new(); public static bool InRoom=>CurrentRoom!=null;
public static List<(int Actor,object[] Data)> Sent=new();
public static bool RaiseEvent(byte code,object data,Photon.Realtime.RaiseEventOptions options,ExitGames.Client.Photon.SendOptions send) { if(code!=191||options.TargetActors.Length!=1)throw new Exception("Not targeted"); Sent.Add((options.TargetActors.Single(),(object[])data));return true; }
public static void AddCallbackTarget(object target) {} public static void RemoveCallbackTarget(object target) {}
}}
public class PlayerAvatar { public int Actor=1; }
public class GameManager { public static GameManager? instance=new(); }
public static class SemiFunc { public static bool Multiplayer=true; public static PlayerAvatar Local=new(); public static bool IsMultiplayer()=>Multiplayer;public static bool IsMasterClientOrSingleplayer()=>!Multiplayer||Photon.Pun.PhotonNetwork.IsMasterClient;public static PlayerAvatar PlayerGetLocal()=>Local; }
namespace REPOJP.StageRoles {
internal static class StageRolesPlugin { public static Logger ModLogger=new(); }
internal class Logger { public void LogDebug(string s) {} }
internal class StageRoleController {
internal List<int> Requested=new(); internal int Reads;
internal UpgradeBreakdownSnapshot? BuildUpgradeBreakdownForActor(int actor) { Requested.Add(actor); return BuildUpgradeBreakdown(new(){Actor=actor}); }
internal UpgradeBreakdownSnapshot? BuildUpgradeBreakdown(PlayerAvatar player) { Reads++;return new(StageRole.Runner,new[]{new UpgradeBreakdownRow("Speed",1,0,3,0,0,0,player.Actor+3)}); }
}}
