$ErrorActionPreference = 'Stop'
$commandSource = Get-Content -LiteralPath (Join-Path $PSScriptRoot '../RoleTestCommandService.cs') -Raw
$controllerSource = Get-Content -LiteralPath (Join-Path $PSScriptRoot '../StageRoleController.cs') -Raw
function Read-Methods($source, $names) {
    foreach ($name in $names) {
        $match = [regex]::Match($source, "(?ms)^    (?:private|internal) (?:static )?[^\r\n]+\b$name\(.*?(?=^    (?:private|internal|public) )")
        if (!$match.Success) { throw "Production method not found: $name" }
        $match.Value
    }
}
$commands = Read-Methods $commandSource @('Execute', 'Suggest', 'IsRandomRoleArgument', 'AddSuggestion', 'TryParseRole', 'BuildRoleList')
$controller = Read-Methods $controllerSource @('TrySetRole', 'TrySetTwins', 'TrySetRoleForAll', 'ResolveAssignment')
$usage = [regex]::Match($commandSource, '(?m)^    internal const string TwinsCommandUsage = [^\r\n]+').Value
$enum = [regex]::Match((Get-Content -LiteralPath (Join-Path $PSScriptRoot '../StageRole.cs') -Raw), '(?s)internal enum StageRole\s*\{.*?\}').Value
if (!$usage -or !$enum) { throw 'Production constants not found.' }
$harness = @'
#nullable enable annotations
#nullable disable warnings
using System;
using System.Linq;
using System.Collections.Generic;
__ENUM__
internal class PlayerAvatar(string id, string name) { public string Id=id, Name=name; public bool Living=true; }
internal class RoleAssignment(string id, string name) {
    public string SteamId=id; public PlayerAvatar Player=new(id,name);
    public StageRole AssignedRole=StageRole.Tank, Role=StageRole.Tank;
}
internal static class PlayerIdentity {
    public static string SteamId(PlayerAvatar player)=>player.Id;
    public static string Name(PlayerAvatar player)=>player.Name;
}
internal static class PlayerState { public static bool IsLiving(PlayerAvatar player)=>player.Living; }
internal static class SemiFunc { public static PlayerAvatar PlayerGetLocal()=>new("76561190000000001","Local"); }
internal static class RoleCatalog {
    public static IReadOnlyList<StageRole> AllRoles=Enum.GetValues<StageRole>();
    public static string AssignmentName(StageRole role)=>role.ToString();
    public static bool IsSecretRole(StageRole role)=>(int)role>=1001;
}
internal class Logger { public void LogInfo(string text){} }
internal static class StageRolesPlugin { public static Logger ModLogger=new(); }
internal record RoleSnapshot(int PlayerNumber,string SteamId);
internal static class RoleAssignmentSync {
    public static IEnumerable<RoleSnapshot> Read()=>Enumerable.Range(1,4).Select(i=>new RoleSnapshot(i,$"7656119000000000{i}"));
}
internal class StageRoleController {
    internal List<RoleAssignment> _assignments=Enumerable.Range(1,4).Select(i=>new RoleAssignment($"7656119000000000{i}",$"Player{i}")).ToList();
    private Dictionary<string,RoleAssignment> _assignmentsBySteamId;
    private sealed class Pair(RoleAssignment[] members) { public RoleAssignment[] Members=members; }
    private Pair? _twins;
    internal bool Allowed=true;
    internal int Applied;
    internal string RandomTarget="";
    internal StageRoleController() { _assignmentsBySteamId=_assignments.ToDictionary(a=>a.SteamId); }
    private bool CanChangeRoles(out string response) { response="Not allowed";return Allowed; }
    private bool IsTwin(PlayerAvatar player)=>_twins?.Members.Any(a=>a.Player==player)==true;
    private int TwinIndex(PlayerAvatar player)=>_twins!.Members[0].Player==player?0:1;
    private void ApplyRoleChanges(Dictionary<string,StageRole> changes) {
        Applied++;
        foreach(var a in _assignments) if(changes.TryGetValue(a.SteamId,out var role)) a.Role=a.AssignedRole=role;
        var twins=_assignments.Where(a=>a.Role==StageRole.Twins).ToArray();
        if(twins.Length!=0 && twins.Length!=2)throw new Exception("Partial Twins mutation");
        _twins=twins.Length==2?new Pair(twins):null;
    }
    internal bool TryRandomizeAllRoles(out string response) { RandomTarget="all";response="random";return true; }
    internal bool TryRandomizeRole(string target,out string response) { RandomTarget=target;response="random";return true; }
__CONTROLLER__
}
internal class RoleTestCommandService(StageRoleController controller) {
    private StageRoleController _controller=controller;
    internal static bool Success;
    internal static string Response="";
    private static void Respond(string text,bool success,float duration=4f) { Success=success;Response=text; }
__USAGE__
__COMMANDS__
    internal void Run(params string[] args)=>Execute(false,args);
    internal static List<string> Complete(params string[] args)=>Suggest(false,"",args);
}
public static class TwinsCommandChecks {
    public static void Run() {
        int checks=0;
        void Check(bool value,string label) { if(!value)throw new Exception(label);checks++; }
        StageRoleController c=new();RoleTestCommandService cmd=new(c);
        void Fresh() { c=new();cmd=new(c); }
        void Rejected(params string[] args) {
            int before=c.Applied;var roles=c._assignments.Select(a=>a.AssignedRole).ToArray();
            cmd.Run(args);
            Check(!RoleTestCommandService.Success && c.Applied==before && roles.SequenceEqual(c._assignments.Select(a=>a.AssignedRole)),"Rejected without mutation: "+string.Join(" ",args));
        }
        cmd.Run("Twins","2","4");
        Check(RoleTestCommandService.Success && c.Applied==1,"Both targets applied in one mutation");
        Check(c._assignments[1].Role==StageRole.Twins && c._assignments[3].Role==StageRole.Twins && c._assignments[0].Role==StageRole.Tank && c._assignments[2].Role==StageRole.Tank,"Only explicit pair selected");
        Check(RoleTestCommandService.Response.Contains("2 + 4"),"Response identifies both players");
        Rejected("Twins","1","3");Rejected("Twins","2","3");
        cmd.Run("Twins","4","2");Check(RoleTestCommandService.Success && c.Applied==2,"Same pair accepted in reverse order");
        Fresh();cmd.Run("43","1","3");Check(RoleTestCommandService.Success && c._assignments[0].Role==StageRole.Twins && c._assignments[2].Role==StageRole.Twins,"Numeric role alias uses pair route");
        Fresh();cmd.Run("tWiNs","76561190000000002","4");Check(RoleTestCommandService.Success && c._assignments[1].Role==StageRole.Twins,"Steam ID and player number can be mixed");
        Fresh();cmd.Run("Twins","Player1","player3");Check(RoleTestCommandService.Success && c._assignments[2].Role==StageRole.Twins,"Distinct single-token names work");
        Fresh();
        Rejected("Twins");Rejected("Twins","1");Rejected("Twins","all");Rejected("Twins","1","2","3");
        Rejected("Twins","1","1");Rejected("Twins","1","76561190000000001");
        Rejected("Twins","0","2");Rejected("Twins","1","5");Rejected("Twins","missing","2");
        Rejected("Twins","1","");Rejected("Twins"," ","2");Rejected("Twins","all","2");Rejected("Twins","1","ALL");
        Rejected("Twins","Player","2");
        c._assignments[0].Player.Name="Same";c._assignments[2].Player.Name="Same";Rejected("Twins","Same","2");
        cmd.Run("Twins","1","3");Check(RoleTestCommandService.Success,"Player numbers disambiguate duplicate names");
        Fresh();c._assignments[1].Player.Living=false;Rejected("Twins","1","2");Rejected("Twins","2","1");
        Fresh();c.Allowed=false;Rejected("Twins","1","2");
        Fresh();c._assignments.RemoveRange(1,3);Rejected("Twins","1","2");
        Fresh();Check(!c.TrySetRole("1",StageRole.Twins,out _) && c.Applied==0,"Single-target API cannot auto-pick a partner");
        c._assignments.RemoveRange(2,2);Check(!c.TrySetRoleForAll(StageRole.Twins,out _) && c.Applied==0,"All API rejected even with exactly two players");
        Fresh();cmd.Run("Runner","2");Check(RoleTestCommandService.Success && c._assignments[1].Role==StageRole.Runner && c._assignments[0].Role==StageRole.Tank,"Ordinary target command preserved");
        c._assignments[0].Player.Name="Name With Spaces";cmd.Run("Runner","Name","With","Spaces");Check(RoleTestCommandService.Success && c._assignments[0].Role==StageRole.Runner,"Ordinary multi-word names preserved");
        cmd.Run("Tank","all");Check(RoleTestCommandService.Success && c._assignments.All(a=>a.Role==StageRole.Tank),"Ordinary all command preserved");
        cmd.Run("random","all");Check(c.RandomTarget=="all","Random all preserved");
        cmd.Run("rd","3");Check(c.RandomTarget=="3","Single random preserved");
        Check(RoleTestCommandService.Complete("Twins","").SequenceEqual(new[]{"1","2","3","4"}),"First target suggestions exclude all");
        Check(RoleTestCommandService.Complete("43","2","").SequenceEqual(new[]{"1","3","4"}),"Second target suggestions exclude first number");
        Check(RoleTestCommandService.Complete("Twins","76561190000000001","").SequenceEqual(new[]{"2","3","4"}),"Second target suggestions exclude first Steam ID");
        Check(RoleTestCommandService.Complete("Twins","1","2","").Count==0,"No third target suggested");
        Check(RoleTestCommandService.Complete("Runner","").Contains("all"),"Ordinary all suggestion preserved");
        Console.WriteLine($"PASS: {checks} production Twins command/target checks (game stand-ins).");
    }
}
'@
Add-Type -TypeDefinition $harness.Replace('__ENUM__',$enum).Replace('__USAGE__',$usage).Replace('__COMMANDS__',($commands -join "`n")).Replace('__CONTROLLER__',($controller -join "`n"))
[TwinsCommandChecks]::Run()
