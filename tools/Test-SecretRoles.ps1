$ErrorActionPreference = 'Stop'
$models = Get-Content -LiteralPath (Join-Path $PSScriptRoot '../RoleModels.cs') -Raw
$planner = Get-Content -LiteralPath (Join-Path $PSScriptRoot '../RoleAssignmentPlanner.cs') -Raw
$migration = Get-Content -LiteralPath (Join-Path $PSScriptRoot '../RoleConfigMigration.cs') -Raw
$commands = Get-Content -LiteralPath (Join-Path $PSScriptRoot '../RoleTestCommandService.cs') -Raw
$parseRole = [regex]::Match($commands, '(?s)    private static bool TryParseRole\(.*?(?=    private static string BuildRoleList)').Value
$enumSource = Get-Content -LiteralPath (Join-Path $PSScriptRoot '../StageRole.cs') -Raw
$roleEnum = [regex]::Match($enumSource, '(?s)internal enum StageRole\s*\{.*?\}').Value
$catalog = [regex]::Match($models, '(?s)    internal static bool IsSecretRole.*?(?=    internal static IReadOnlyList<UpgradeGrant> BaseUpgrades)').Value
$move = [regex]::Match($migration, '(?s)    private static void MoveKeys\(.*?(?=    private static void MoveKey\()').Value
$planner = $planner.Substring($planner.IndexOf('internal sealed class RoleAssignmentPlanner'))
if (!$roleEnum -or !$catalog -or !$move -or !$planner) { throw 'Production source extraction failed' }
$source = @'
#nullable enable annotations
#nullable disable warnings
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
namespace UnityEngine {
    public static class Random { public static int Range(int min, int max) => min; }
    public static class Mathf { public static int Max(int a, int b) => Math.Max(a,b); public static int Clamp(int x,int a,int b)=>Math.Clamp(x,a,b); }
}
namespace REPOJP.StageRoles {
__ENUM__
internal class PlayerAvatar { public string Id = "p"; }
internal class Logger { public void LogDebug(string text) { } }
internal static class StageRolesPlugin { public static readonly Logger ModLogger = new(); }
internal static class PlayerIdentity { public static string SteamId(PlayerAvatar p)=>p.Id; }
internal class Setting<T>(T value) { public T Value = value; }
internal class StageRolesConfig {
    public const int MaximumSupportedPlayers = 30;
    public Setting<bool> UniqueRoles = new(true), PreventConsecutiveSameRole = new(true), RiskCombinationLimitsEnabled = new(false);
    public Setting<int> HardshipPersonalCooldownStages = new(2), SmallPartyCombinedRiskPlayerCount = new(4), SmallPartyCombinedRiskRoleLimit = new(1);
    public int ShowcaseMinimumForPlayerCount(int n)=>0;
    public int SupportMinimumForPlayerCount(int n)=>0;
    public int DangerRoleLimitForPlayerCount(int n)=>1;
    public int HardshipRoleLimitForPlayerCount(int n)=>1;
    public int RoleWeightValue(StageRole role)=>RoleCatalog.IsSecretRole(role)?1:100;
}
internal static class RoleCatalog {
    public static readonly IReadOnlyList<StageRole> AllRoles = Enum.GetValues<StageRole>();
__CATALOG__
    public static bool BaseUpgradeMeetsOrExceedsRoleTarget(StageRole role,StageRolesConfig config)=>false;
    public static bool InfluencerHasUpgradeBenefit(StageRolesConfig config,int count)=>true;
}
__PLANNER__
internal readonly record struct ConfigDefinition(string Section,string Key);
public static class SecretRoleChecks {
__MOVE__
__PARSE__
    public static void Run() {
        int count=0;
        void Check(bool value,string label) { if(!value)throw new Exception(label);count++; }
        Check((int)StageRole.Superbot==1001 && (int)StageRole.Disaster==1002,"secret IDs");
        Check(RoleCatalog.DisplayName(StageRole.Superbot)=="???1" && RoleCatalog.DisplayName(StageRole.Disaster)=="???2","hidden titles");
        Check(RoleCatalog.AssignmentName(StageRole.Disaster)=="Disaster","assignment identity");
        Check(TryParseRole("1001",out var parsed) && parsed==StageRole.Superbot,"command ID 1001");
        Check(TryParseRole("1002",out parsed) && parsed==StageRole.Disaster,"command ID 1002");
        Check(TryParseRole("???1",out parsed) && parsed==StageRole.Superbot,"command secret alias 1");
        Check(TryParseRole("???2",out parsed) && parsed==StageRole.Disaster,"command secret alias 2");
        Check(TryParseRole("Disaster",out parsed) && parsed==StageRole.Disaster,"command real name");
        Check(!TryParseRole("999",out parsed) && !TryParseRole("1000",out parsed),"old IDs no longer assigned");
        Check(!TryParseRole("42",out parsed),"hidden roles have no ordinary numeric alias");
        Check(TryParseRole("41",out parsed) && parsed==StageRole.Influenza,"Influenza appended without renumbering existing roles");
        Check(TryParseRole("40",out parsed) && parsed==StageRole.Brawler,"standard IDs preserved");
        foreach(StageRole role in Enum.GetValues<StageRole>())
            Check(RoleCatalog.HasCapability(StageRole.Disaster,role)==(role is StageRole.Disaster or StageRole.Bomber or StageRole.Stinker or StageRole.Tuna),"Disaster capability "+role);
        Check(!RoleCatalog.HasCapability(StageRole.Superbot,StageRole.Disaster),"Superbot excludes Disaster");
        Check(!RoleCatalog.HasCapability(StageRole.Superbot,StageRole.Influenza),"Superbot excludes Influenza");
        Check(!RoleCatalog.CanBeCopiedByImitator(StageRole.Influenza),"Influenza is acquired through infection, not copying");
        Check(!RoleCatalog.CanBeCopiedByImitator(StageRole.Disaster),"Disaster cannot be copied");
        Check(!RoleCatalog.CanBeCopiedByImitator(StageRole.Superbot),"Superbot cannot be copied");
        var config=new StageRolesConfig();
        var planner=new RoleAssignmentPlanner(config,new Dictionary<string,StageRole>(),new Dictionary<string,List<StageRole>>(),new Dictionary<string,int>(),1);
        var secrets=new[]{StageRole.Superbot,StageRole.Disaster};
        Check(planner.PlanJoinedAssignment("p",secrets,Array.Empty<StageRole>(),2)==null,"two secrets cannot fill empty pool");
        Check(planner.PlanJoinedAssignment("p",new[]{StageRole.Disaster},Array.Empty<StageRole>(),2)==null,"Disaster alone not fallback");
        var flags=BindingFlags.NonPublic|BindingFlags.Instance;
        var candidates=typeof(RoleAssignmentPlanner).GetMethod("Candidates",flags)!;
        List<StageRole> Pool(int n)=>(List<StageRole>)candidates.Invoke(planner,new object?[]{"p",new[]{StageRole.Tank,StageRole.Disaster},Array.Empty<StageRole>(),n,true,true,true,null})!;
        Check(!Pool(1).Contains(StageRole.Disaster),"solo excludes Disaster");
        Check(Pool(2).Contains(StageRole.Disaster),"multiplayer admits Disaster");
        var weight=typeof(RoleAssignmentPlanner).GetMethod("SelectionWeight",flags)!;
        int W(StageRole role)=>(int)weight.Invoke(planner,new object[]{"p",role})!;
        Check(W(StageRole.Disaster)*1000==W(StageRole.Tank),"fixed 1/1000 standard weight");
        Check(W(StageRole.Disaster)==W(StageRole.Superbot),"same secret rarity");
        config.RiskCombinationLimitsEnabled.Value=true;
        var riskPool=(List<StageRole>)candidates.Invoke(planner,new object?[]{"p",new[]{StageRole.Disaster},new[]{StageRole.Bomber},5,true,true,true,null})!;
        Check(!riskPool.Contains(StageRole.Disaster),"danger cap applies");
        riskPool=(List<StageRole>)candidates.Invoke(planner,new object?[]{"p",new[]{StageRole.Disaster},new[]{StageRole.Tuna},5,true,true,true,null})!;
        Check(!riskPool.Contains(StageRole.Disaster),"hardship cap applies");
        var prior=new Dictionary<string,StageRole>{{"p",StageRole.Tank}};
        var fallback=new RoleAssignmentPlanner(config,prior,new Dictionary<string,List<StageRole>>(),new Dictionary<string,int>(),2);
        Check(fallback.PlanJoinedAssignment("p",new[]{StageRole.Tank,StageRole.Disaster},Array.Empty<StageRole>(),2)==StageRole.Tank,"history fallback does not select lone secret");
        foreach(string enabled in new[]{"true","false"}) {
            var values=new Dictionary<ConfigDefinition,string>{{new("???","Enabled"),enabled}};
            int moved=0,removed=0;
            MoveKeys(values,"???","???1",ref moved,ref removed,"Enabled");
            Check(values[new("???1","Enabled")]==enabled && !values.ContainsKey(new("???","Enabled")),"preserve old Enabled "+enabled);
        }
        var existing=new Dictionary<ConfigDefinition,string>{{new("???","Enabled"),"true"},{new("???1","Enabled"),"false"}};
        int m=0,r=0;MoveKeys(existing,"???","???1",ref m,ref r,"Enabled");
        Check(existing[new("???1","Enabled")]=="false","new setting wins");
        Console.WriteLine($"PASS: {count} secret-role checks using production catalog/planner/migration methods with stand-in settings.");
    }
}
}
'@
Add-Type -TypeDefinition $source.Replace('__ENUM__',$roleEnum).Replace('__CATALOG__',$catalog).Replace('__PLANNER__',$planner).Replace('__MOVE__',$move).Replace('__PARSE__',$parseRole)
[REPOJP.StageRoles.SecretRoleChecks]::Run()
