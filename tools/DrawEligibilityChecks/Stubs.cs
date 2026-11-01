using REPOJP.StageRoles;
namespace UnityEngine
{
    public static class Time { public static float unscaledTime; }
    public static class Random { public static int Calls; private static System.Random _random = new(1); public static void Reset() { Calls=0; _random=new(1); } public static int Range(int a,int b) { Calls++; return _random.Next(a,b); } }
    public static class Mathf { public static int Max(int a,int b)=>Math.Max(a,b); public static int Clamp(int x,int a,int b)=>Math.Clamp(x,a,b); }
}
namespace UnityEngine.SceneManagement
{
    public sealed class Scene { public string name="stage"; }
    public static class SceneManager { public static Scene Active=new(); public static Scene GetActiveScene()=>Active; }
}
namespace ExitGames.Client.Photon { public sealed class Hashtable : Dictionary<object,object?> { } }
namespace Photon.Pun
{
    public sealed class Peer { public int ActorNumber=1; }
    public sealed class Room
    {
        public Dictionary<object,object> CustomProperties=new(); public int Publishes;
        public void SetCustomProperties(ExitGames.Client.Photon.Hashtable properties)
        { Publishes++; foreach(var p in properties) { if(p.Value==null)CustomProperties.Remove(p.Key); else CustomProperties[p.Key]=p.Value; } }
    }
    public static class PhotonNetwork { public static bool IsMasterClient=true; public static Peer? MasterClient=new(); public static Room? CurrentRoom=new(); }
}
public sealed class PlayerAvatar(string id) { public string Id=id; public bool Living=true; }
public sealed class GameManager { public static GameManager? instance=new(); }
public static class SemiFunc
{
    public static bool Multiplayer=true, Generated=true;
    public static bool IsMultiplayer()=>Multiplayer;
    public static bool LevelGenDone()=>Generated;
    public static bool IsMasterClientOrSingleplayer()=>!Multiplayer || Photon.Pun.PhotonNetwork.IsMasterClient;
    public static PlayerAvatar PlayerGetLocal()=>StageRoleController.Players[0];
}
namespace MenuLib.MonoBehaviors { public sealed class REPOPopupPage { } }
namespace TMPro { public enum FontStyles { Normal, Bold } }
namespace REPOJP.StageRoles
{
    internal class Setting<T>(T value) { public T Value=value; }
    internal sealed class StageRolesConfig
    {
        public const int MaximumSupportedPlayers=30;
        public Setting<bool> Enabled=new(true), ExcludeUnavailableContextRoles=new(true), UniqueRoles=new(true), PreventConsecutiveSameRole=new(true), RiskCombinationLimitsEnabled=new(false);
        public Setting<int> HardshipPersonalCooldownStages=new(2), SmallPartyCombinedRiskPlayerCount=new(4), SmallPartyCombinedRiskRoleLimit=new(1);
        public HashSet<StageRole> Off=new(); public Dictionary<StageRole,int> Weights=new();
        public int ShowcaseMinimumForPlayerCount(int n)=>0;
        public int SupportMinimumForPlayerCount(int n)=>0;
        public int DangerRoleLimitForPlayerCount(int n)=>1;
        public int HardshipRoleLimitForPlayerCount(int n)=>1;
        public bool RoleIsEnabled(StageRole r)=>!Off.Contains(r);
        public int RoleWeightValue(StageRole r)=>Weights.GetValueOrDefault(r,100);
    }
    internal static partial class RoleCatalog
    {
        internal static readonly IReadOnlyList<StageRole> AllRoles=Enum.GetValues<StageRole>();
        internal static HashSet<(string?,StageRole)> AtCap=new();
        internal static bool Influence=true;
        internal static bool BaseUpgradeMeetsOrExceedsRoleTarget(StageRole role,StageRolesConfig c,string? id=null)=>AtCap.Contains((id,role));
        internal static bool InfluencerHasUpgradeBenefit(StageRolesConfig c,int count,string? id=null)=>Influence;
    }
    internal static class PlayerIdentity { internal static string SteamId(PlayerAvatar p)=>p.Id; }
    internal static class PlayerState { internal static bool IsLiving(PlayerAvatar p)=>p.Living; }
    internal static class UpgradeService { internal static bool Ready=true; }
    internal sealed class Logger { public void LogDebug(string s) { } }
    internal static class StageRolesPlugin { public static Logger ModLogger=new(); }
    internal sealed record RoleAssignment(string SteamId, StageRole AssignedRole);
    internal sealed partial class StageRoleController
    {
        internal readonly StageRolesConfig _config=new();
        internal static List<PlayerAvatar> Players=new();
        internal bool _stageReady=false, _assignmentsInitialized=false;
        internal int _completedAssignmentStages=0, _currentAssignmentStage=0;
        private const int RecentRoleHistoryLimit=5;
        internal Dictionary<string,StageRole> _previousRoles=new();
        internal Dictionary<string,List<StageRole>> _recentRolesByPlayer=new();
        internal Dictionary<string,int> _lastHardshipStages=new();
        internal List<RoleAssignment> _assignments=new();
        internal bool Playable=true, ContextCheck=true, Throw=false;
        internal HashSet<StageRole> Missing=new();
        private static List<PlayerAvatar> CollectPlayers()=>new(Players);
        private bool IsPlayableStageContext() { if(Throw)throw new InvalidOperationException("world unloaded"); return Playable; }
        private bool RemoveUnavailableContextRoles(List<StageRole> roles)
        { if(!_config.ExcludeUnavailableContextRoles.Value)return true; roles.RemoveAll(Missing.Contains); return ContextCheck; }
    }
    internal enum RoleMenuView { Guide }
    internal sealed class RoleMenuEntry(string text,float size,TMPro.FontStyles style,float height,bool wrap,bool useLanguageFont=false,StageRole? emblemRole=null,bool unrevealedEmblem=false,bool emblemGrayedOut=false)
    {
        internal string Text=text; internal bool Hidden=unrevealedEmblem, Gray=emblemGrayedOut;
        internal object[] Unused={size,style,height,wrap,useLanguageFont,emblemRole??StageRole.Tank};
    }
    internal sealed partial class RoleMenu
    {
        private static RoleGuideLanguage _guideLanguage=RoleGuideLanguage.Japanese;
        private const float GuideFontSize=16,GuideLineHeight=22;
        private static bool UseLanguageFont=>true;
        private static object MeasurementText(MenuLib.MonoBehaviors.REPOPopupPage p)=>p;
        private static float ContentWidth(MenuLib.MonoBehaviors.REPOPopupPage p)=>500;
        private static IEnumerable<string> WrapGuideText(object obj,string text,float width,RoleGuideLanguage lang)=>new[]{text};
        private static string Localized(string text)=>RoleText.Get(text,_guideLanguage);
        private static void SwitchView(MenuLib.MonoBehaviors.REPOPopupPage p,RoleMenuView view) { }
        internal static List<RoleMenuEntry> Rows()
        { var result=new List<RoleMenuEntry>(); var page=new MenuLib.MonoBehaviors.REPOPopupPage();OpenDrawEligibility(page); if(!_guideEligibility)throw new Exception(); AddDrawEligibilityRows(page,result);return result; }
    }
}
