using BepInEx.Configuration;
using REPOJP.StageRoles;

internal static class CombatMigrationChecks
{
    internal static int Run(string directory)
    {
        int checks=0;
        void Check(bool ok,string label) {checks++;if(!ok)throw new Exception(label);}
        foreach(var (schema,custom) in new[]{(40,false),(40,true),(41,false),(99,false)})
        {
            string path=Path.Combine(directory,$"combat-{schema}-{custom}.cfg");
            string text=$"[Migration]\nConfigVersion = {schema}\n[Sniper]\nReferenceDistance = {(custom ? "10" : "8.00")}\nMaximumMultiplierDistance = {(custom ? "35" : "24.0")}\n[Avenger]\nDurationSeconds = {(custom ? "45" : "20.00")}\n[Brawler]\nMeleeDamageMultiplier = 1.75\n";
            File.WriteAllText(path,text);
            var file=new ConfigFile(path,false){SaveOnConfigSet=false};var c=new StageRolesConfig(file);
            Check(c.SniperReferenceDistance.Value==(custom?10:schema<41?6:8),"reference default migration");
            Check(c.SniperMaximumMultiplierDistance.Value==(custom?35:schema<41?18:24),"maximum distance migration");
            Check(c.AvengerDurationSeconds.Value==(custom?45:schema<41?30:20),"Avenger duration migration");
            Check(c.BrawlerMeleeDamageMultiplier.Value==1.75f,"existing custom multiplier preserved");
            if(schema==40)Check(File.ReadAllText(path+".pre-v4.6.0-combat.bak")==text,"exact rollback backup");
            if(schema==99)Check(File.ReadAllText(path)==text,"future schema untouched");
            file.Save();string once=File.ReadAllText(path);RoleConfigMigration.Apply(file);Check(File.ReadAllText(path)==once,"migration idempotence");
        }
        return checks;
    }
}
