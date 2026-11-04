using BepInEx.Configuration;
using Photon.Pun;
using System.Text;
using System.Security.Cryptography;
namespace REPOJP.StageRoles;
internal static class QualityOfLifeChecks
{
    internal static void Run(string directory)
    {
        int checks = 0;
        void Check(bool value, string label) { checks++; if (!value) throw new Exception(label); }
        SemiFunc.Multiplayer = false; GameManager.instance = new();
        string path = Path.Combine(directory, "quality.cfg");
        var file = new ConfigFile(path, false) { SaveOnConfigSet = true };
        var config = new StageRolesConfig(file); var service = new RoleSelectionSettings(config, file);
        StageRolesPlugin.Instance.RoleSettings = service;
        string presets = Path.ChangeExtension(path, ".presets.json");
        Check(!config.TeamCompositionEnabled.Value, "Team adjustment defaults OFF");
        config.TeamSupportMinimum.Value = 20;
        Check(service.TrySetTeamComposition(true) && config.SupportMinimumForPlayerCount(2) == 2, "Support target bounded by party size");
        config.TeamDangerMaximum.Value = 1;
        Check(!service.TryAdjustTeamTarget(false,-1), "Danger target cannot move below 1");
        Check(service.TryAdjustTeamTarget(false,1) && config.DangerRoleLimitForPlayerCount(3) == 2, "Menu saves danger target");
        Check(service.TrySetTeamComposition(false), "Team switch can be turned off");
        service.TrySaveCustomPreset(0); service.TrySetCustomPresetNote(0, "友人用");
        Check(service.TryRenameCustomPreset(0, " 初心者 ") && service.CustomPresets.Get(0)!.Name == "初心者", "Name normalized");
        Check(!service.TryRenameCustomPreset(1,"empty"), "Empty slot cannot be renamed");
        string live = File.ReadAllText(path); var saved = service.CustomPresets.Get(0)!;
        Check(service.TryCopyCustomPreset(0,1,false) && !service.TryCopyCustomPreset(0,0,false) && !service.TryCopyCustomPreset(0,1,false), "Copy respects occupied slot and source");
        Check(service.TrySaveCustomPreset(1,true) && service.CustomPresets.Get(1)!.Name == saved.Name && service.CustomPresets.Get(1)!.Note == saved.Note, "Overwrite selection retains metadata");
        Check(File.ReadAllText(path) == live, "Metadata and duplication never change active config");
        Check(new RoleSelectionSettings(config,file).CustomPresets.Get(1)!.Name == saved.Name, "Name survives restart");
        Check(service.TryRenameCustomPreset(1,new string('a',31)+"😀") && service.CustomPresets.Get(1)!.Name.Length == 31, "Name boundary preserves UTF16 pair");
        RoleMenu.Show(config, presets:true);
        RoleMenu.Entries.First(e=>e.Text=="Duplicate to: Custom 3").OnClick!();
        Check(service.CustomPresets.Get(2)==null && File.ReadAllText(path)==live, "Duplication only previews");
        Action stale=RoleMenu.Entries.Single(e=>e.Text=="Confirm save to this slot").OnClick!;
        service.TrySaveCustomPreset(2); var before=service.CustomPresets.Get(2); stale();
        Check(ReferenceEquals(before,service.CustomPresets.Get(2)) && !RoleMenu.Entries.Any(e=>e.Text=="Confirm save to this slot"), "Changed destination invalidates confirmation");
        RoleMenu.Entries.First(e=>e.Text=="Duplicate to: Custom 3").OnClick!();
        RoleMenu.Entries.Single(e=>e.Text=="Confirm save to this slot").OnClick!();
        Check(service.CustomPresets.Get(2)!.Name==saved.Name && File.ReadAllText(path)==live, "Confirmed duplicate stores only destination");
        string disk=File.ReadAllText(presets); int rev=service.CustomPresets.Revision;
        using(File.Open(presets,FileMode.Open,FileAccess.Read,FileShare.None))
        { bool failed=false;try{service.TryRenameCustomPreset(0,"lost");}catch(IOException){failed=true;} Check(failed&&service.CustomPresets.Revision==rev&&service.CustomPresets.Get(0)!.Name==saved.Name,"Rename disk failure is atomic"); }
        SemiFunc.Multiplayer=true;PhotonNetwork.IsMasterClient=false;
        Check(!service.TryRenameCustomPreset(0,"guest")&&!service.TryCopyCustomPreset(0,1,true)&&!service.TryStoreCustomPreset(0,saved,true)&&!service.TrySetTeamComposition(true)&&!service.TryAdjustTeamTarget(true,1),"Guest cannot mutate host presets or team settings");
        Check(File.ReadAllText(presets)==disk,"Guest denial leaves file intact");
        SemiFunc.Multiplayer=false; PhotonNetwork.IsMasterClient=true;
        foreach(int schema in new[]{1,2})
        {
            string legacy=Path.Combine(directory,$"legacy-{schema}.json");
            File.WriteAllText(legacy,"{\"version\":"+schema+",\"slots\":[[\"Tank\",\"FutureRole\"],null,null]"+(schema==2?",\"notes\":[\"old\",null,null]":"")+"}");
            var store=new CustomRolePresets(legacy); Check(store.Available && store.Get(0)!.Name=="","Legacy version loads without name");
            Check(store.TryRename(0,"new")&&new CustomRolePresets(legacy).Get(0)!.Names.Contains("FutureRole"),"Migration retains unknown stored roles");
        }
        foreach(StageRole role in RoleCatalog.AllRoles)
        {
            Check(RoleListFilter.Matches(role,"",RoleCategory.All,RoleListScope.All,false,DrawStatus.Unknown),"All includes off/unknown");
            Check(RoleListFilter.Matches(role,"",RoleListFilter.Category(role),RoleListScope.Eligible,true,DrawStatus.Conditional),"Conditional roles included");
            Check(!RoleListFilter.Matches(role,"",RoleCategory.All,RoleListScope.Eligible,true,DrawStatus.Unknown),"Unknown excluded from eligible");
            Check(!RoleListFilter.Matches(role,"",RoleCategory.All,RoleListScope.Enabled,false,DrawStatus.Eligible),"Disabled excluded from enabled");
        }
        Check(RoleListFilter.Matches(StageRole.Jobless," courIER ",RoleCategory.Hardship,RoleListScope.All,true,DrawStatus.Eligible),"Search uses displayed name and ignores case");
        Check(!RoleListFilter.Matches(StageRole.Superbot,"Superbot",RoleCategory.All,RoleListScope.All,true,DrawStatus.Eligible)&&RoleListFilter.Matches(StageRole.Superbot,"Superbot",RoleCategory.All,RoleListScope.All,true,DrawStatus.Eligible,true),"Hidden name searchable only after reveal");
        var ordered=RoleCatalog.AllRoles.Where(r=>RoleListFilter.Matches(r,"",RoleCategory.Support,RoleListScope.All,true,DrawStatus.Eligible)).ToArray();
        Check(ordered.SequenceEqual(ordered.OrderBy(r=>(int)r)),"Filtering preserves numerical order");
        var row=new UpgradeBreakdownRow("Health",2,3,20,-2,4,1,25);
        Check(row.Other==4,"Target includes retained amount without double addition");
        string payload=UpgradeBreakdownCodec.Encode(new(StageRole.Tank,new[]{row}));
        var value=UpgradeBreakdownCodec.Decode(payload);
        Check(value?.Role==StageRole.Tank&&value.Rows.Single().Other==4,"Breakdown round trip");
        Check(new UpgradeBreakdownRow("Health",0,0,int.MaxValue,int.MaxValue,int.MaxValue,0,0).Other==-3L*int.MaxValue,"Residual cannot overflow");
        foreach(string? bad in new string?[]{null,"",new('x',8193),"2|0|",payload.Replace("1|0|","1|999|"),payload+payload.Split('|')[2],payload.Replace("Health","<tag>"),payload.Replace(",25;",",-1;"),payload.Replace(",25;",",2147483648;")})
            Check(UpgradeBreakdownCodec.Decode(bad)==null,"Invalid breakdown rejected");
        Console.WriteLine($"PASS: {checks} duplication, migration, UI confirmation, filter and breakdown checks.");
    }
}
