"""Generate checks for production HP reset/upgrade dispatch and lifecycle hooks.

Run this script, then dotnet run --project tmp/health-reset-checks/Checks.csproj -c Release.
The fixture models the inspected vanilla RPC/save behavior; it is not a live game test.
"""
from pathlib import Path
import hashlib
import json

root = Path(__file__).resolve().parents[1]
out = root / 'tmp/health-reset-checks'
out.mkdir(parents=True, exist_ok=True)
sources = []


def extract(file, start, end):
    text = (root / file).read_text(encoding='utf-8-sig')
    begin = text.index(start)
    body = text[begin:text.index(end, begin)]
    sources.append(dict(file=file, method=start.strip(), sha256=hashlib.sha256(body.encode()).hexdigest()))
    return body


for file in ('HealthUpgradeReset.cs', 'UpgradeService.cs'):
    body = (root / file).read_text(encoding='utf-8-sig')
    (out / file).write_text(body, encoding='utf-8')
    sources.append(dict(file=file, sha256=hashlib.sha256(body.encode()).hexdigest()))

player = extract('PlayerState.cs', '    internal static bool TryGetCurrentHealth(', '    internal static bool SetHealthSynchronized(')
scene = extract('LifecyclePatches.cs', '    private static void SemiFuncOnSceneSwitchPrefix(', '    private static bool IsShopContext()')
observer = extract('LifecyclePatches.cs', '    private static void PlayerHealthUpdateHealthRpcPrefix(', '    [HarmonyPostfix]\n    [HarmonyPatch(typeof(PlayerHealth), nameof(PlayerHealth.UpdateHealthRPC))]')
(out / 'ProductionHooks.cs').write_text('''using System.Reflection;
namespace REPOJP.StageRoles;
internal static class PlayerState {
    private static readonly FieldInfo? HealthField = typeof(PlayerHealth).GetField("health");
    private static readonly FieldInfo? MaxHealthField = typeof(PlayerHealth).GetField("maxHealth");
''' + player + '''}
internal static class LifecyclePatches {
''' + scene.replace('private static', 'internal static') + observer.replace('private static', 'internal static') + '}\n', encoding='utf-8')
(out / 'Checks.csproj').write_text('''<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup>
<OutputType>Exe</OutputType><TargetFramework>net9.0</TargetFramework><Nullable>enable</Nullable>
<ImplicitUsings>enable</ImplicitUsings><TreatWarningsAsErrors>true</TreatWarningsAsErrors>
</PropertyGroup></Project>''', encoding='utf-8')

stubs = r'''
using REPOJP.StageRoles;
public static class SemiFunc {
    public static bool Multiplayer, Authority = true, Shop, Gameplay = true;
    public static Dictionary<string, PlayerAvatar> Players = new();
    public static bool IsMultiplayer() => Multiplayer;
    public static bool IsMasterClientOrSingleplayer() => Authority;
    public static PlayerAvatar? PlayerAvatarGetFromSteamID(string id) => Players.GetValueOrDefault(id);
}
public sealed class PhysGrabber { public float grabStrength = 1f; }
public sealed class PlayerAvatar {
    public string Id = "p";
    public PhysGrabber physGrabber = new();
    public PlayerHealth playerHealth;
    public Photon.Pun.PhotonView? photonView;
    public PlayerAvatar() { playerHealth = new(this); photonView = new(this); }
}
public sealed class PlayerHealth(PlayerAvatar owner) {
    public int health = 300, maxHealth = 520;
    public int DamageCalls, HealCalls, Deaths;
    public T? GetComponent<T>() where T:class => (owner as T) ?? (owner.photonView as T);
    public void UpdateHealthRPC(int healthNew, int healthMax, bool effect, bool hurtByHeal) {
        LifecyclePatches.PlayerHealthUpdateHealthRpcPrefix(this, healthMax, out var before);
        health = healthNew; maxHealth = healthMax;
        if (before >= 0 && health < before) Fixture.ObservedDamage++;
        StatsManager.instance.SetPlayerHealth(owner.Id, health);
    }
    public void Upgrade(int delta) {
        maxHealth += 20 * delta;
        if (delta < 0) {
            DamageCalls++;
            if (SemiFunc.Gameplay) { health = Math.Max(0, health + 20 * delta); if (health == 0) Deaths++; }
        } else { HealCalls++; health = Math.Clamp(health + 20 * delta, 0, maxHealth); }
        StatsManager.instance.SetPlayerHealth(owner.Id, health);
    }
}
public sealed class StatsManager {
    public static StatsManager instance = new();
    public Dictionary<string, Dictionary<string, int>> Values = new() {
        ["playerUpgradeHealth"] = new(), ["playerHealth"] = new()
    };
    public Dictionary<string, int> FetchPlayerUpgrades(string id) => Values
        .Where(p => p.Key.StartsWith("playerUpgrade")).ToDictionary(p => p.Key, p => p.Value.GetValueOrDefault(id));
    public int GetPlayerHealth(string id) => Values["playerHealth"].GetValueOrDefault(id, 100);
    public void SetPlayerHealth(string id, int hp) { if (!SemiFunc.Shop) Set("playerHealth", id, hp); }
    public void Set(string key, string id, int value) {
        if (!Values.TryGetValue(key, out var map)) Values[key] = map = new();
        map[id] = value;
    }
}
public sealed class PunManager {
    public static PunManager instance = new();
    public T GetComponent<T>() where T:new() => new();
    public void UpdateStat(string key, string id, int value) {
        StatsManager.instance.Set(key, id, value);
        if (SemiFunc.Multiplayer) Fixture.Messages.Add("UpdateStatRPC:" + key);
        if (SemiFunc.Multiplayer) Fixture.GuestStats[key] = value;
    }
    public void TesterUpgradeCommandRPC(string id, string command, int delta) {
        string key = "playerUpgrade" + command;
        int old = StatsManager.instance.FetchPlayerUpgrades(id).GetValueOrDefault(key);
        int next = Math.Max(0, old + delta);
        StatsManager.instance.Set(key, id, next);
        if (command == "Health") SemiFunc.PlayerAvatarGetFromSteamID(id)?.playerHealth.Upgrade(next - old);
    }
    public int UpgradePlayerHealth(string id, int delta) { TesterUpgradeCommandRPC(id, "Health", delta); return 0; }
    public int UpgradePlayerCrouchRest(string id, int delta) => 0;
    public int UpgradePlayerExtraJump(string id, int delta) => 0;
    public int UpgradePlayerTumbleLaunch(string id, int delta) => 0;
    public int UpgradeMapPlayerCount(string id, int delta) => 0;
    public int UpgradePlayerGrabRange(string id, int delta) => 0;
    public int UpgradePlayerSprintSpeed(string id, int delta) => 0;
    public int UpgradePlayerEnergy(string id, int delta) => 0;
    public int UpgradePlayerGrabStrength(string id, int delta) => 0;
    public int UpgradePlayerThrowStrength(string id, int delta) => 0;
    public int UpgradePlayerTumbleWings(string id, int delta) => 0;
    public int UpgradePlayerTumbleClimb(string id, int delta) => 0;
    public int UpgradeDeathHeadBattery(string id, int delta) => 0;
}
namespace Photon.Pun {
    public enum RpcTarget { All, Others }
    public sealed class PhotonView {
        private PlayerAvatar? owner;
        public PhotonView() { }
        public PhotonView(PlayerAvatar avatar) { owner = avatar; }
        public void RPC(string name, RpcTarget target, params object[] args) {
            Fixture.Messages.Add(name);
            if (name == nameof(PunManager.TesterUpgradeCommandRPC)) {
                if (target == RpcTarget.All) PunManager.instance.TesterUpgradeCommandRPC((string)args[0], (string)args[1], (int)args[2]);
                if ((string)args[1] == "Health") Fixture.GuestUpgradeCalls++;
            } else if (name == nameof(PlayerHealth.UpdateHealthRPC)) {
                if (target != RpcTarget.All || (bool)args[2] || (bool)args[3]) throw new Exception("Unexpected health RPC flags");
                owner!.playerHealth.UpdateHealthRPC((int)args[0], (int)args[1], false, false);
                Fixture.GuestHealth = (int)args[0]; Fixture.GuestMaximum = (int)args[1];
            } else throw new Exception("Non-vanilla RPC " + name);
        }
    }
}
namespace REPOJP.StageRoles {
    internal readonly record struct UpgradeGrant(string CommandName, string DictionaryName, int Level);
    internal static class KingUpgradeAura {
        internal static int WithoutBonus(string id, string key, int level) => level;
        internal static void Forget(string id, string key) { }
    }
    internal static class UpgradeItemRetentionPatch { internal static void ApplyPendingThrow(string id) { } }
    internal class TestLogger {
        internal void LogDebug(string text) { }
        internal void LogWarning(string text) => Fixture.Warnings.Add(text);
    }
    internal sealed class StageRolesPlugin {
        internal static StageRolesPlugin Instance = new();
        internal static TestLogger ModLogger = new();
        internal TestController Controller = new();
    }
    internal sealed class TestController {
        internal bool Assigned = true;
        internal void StageEnding() {
            if (!Assigned) return;
            UpgradeService.SetLevels("p", new[] { new UpgradeGrant("Health", "playerUpgradeHealth", 1) });
            Assigned = false;
        }
    }
}
public static class Fixture {
    public static int GuestHealth, GuestMaximum, GuestUpgradeCalls, ObservedDamage;
    public static Dictionary<string, int> GuestStats = new();
    public static List<string> Messages = new(), Warnings = new();
    public static PlayerAvatar Reset(int hp = 300, int level = 21, bool multiplayer = false) {
        SemiFunc.Multiplayer = multiplayer; SemiFunc.Authority = true;
        SemiFunc.Shop = false; SemiFunc.Gameplay = true;
        var p = new PlayerAvatar(); p.playerHealth.health = hp; p.playerHealth.maxHealth = 100 + 20 * level;
        SemiFunc.Players = new() { ["p"] = p };
        StatsManager.instance = new();
        StatsManager.instance.Set("playerUpgradeHealth", "p", level);
        StatsManager.instance.Set("playerHealth", "p", hp);
        StageRolesPlugin.Instance.Controller = new();
        GuestHealth = hp; GuestMaximum = p.playerHealth.maxHealth; GuestUpgradeCalls = ObservedDamage = 0;
        GuestStats = new() { ["playerUpgradeHealth"] = level, ["playerHealth"] = hp };
        Messages.Clear(); Warnings.Clear(); return p;
    }
}
'''
(out / 'Stubs.cs').write_text(stubs, encoding='utf-8')
(out / 'Program.cs').write_text(r'''
using REPOJP.StageRoles;
int checks = 0;
void Check(bool result, string name) { checks++; if (!result) throw new Exception(name); }
void Set(int level) => UpgradeService.SetLevels("p", new[] { new UpgradeGrant("Health", "playerUpgradeHealth", level) });
foreach (bool multi in new[] { false, true })
foreach (bool shop in new[] { false, true })
foreach (int hp in new[] { 0, 1, 2, 50, 119, 120, 300, 519, 520 }) {
    var p = Fixture.Reset(hp, multiplayer: multi);
    SemiFunc.Shop = shop; SemiFunc.Gameplay = !shop;
    int expected = hp == 0 ? 0 : Math.Max(1, hp * 120 / 520);
    Set(1);
    Check(p.playerHealth.health == expected && p.playerHealth.maxHealth == 120, "Current/max ratio");
    Check(StatsManager.instance.GetPlayerHealth("p") == expected, "Saved HP including shop");
    Check(p.playerHealth.DamageCalls == 0 && p.playerHealth.Deaths == 0 && p.playerHealth.HealCalls == 0, "No damage/heal/death calls");
    Check(Fixture.ObservedDamage == 0, "Maximum conversion is not observed combat damage");
    Check(Fixture.Warnings.Count == 0, "No reset warnings");
    if (multi) {
        Check(Fixture.GuestHealth == expected && Fixture.GuestMaximum == 120, "Vanilla guest current/max");
        Check(Fixture.GuestStats["playerHealth"] == expected && Fixture.GuestStats["playerUpgradeHealth"] == 1, "Vanilla guest persistence");
        Check(Fixture.GuestUpgradeCalls == 0, "No negative health upgrade to guest");
        Check(Fixture.Messages.SequenceEqual(new[] { "UpdateStatRPC:playerUpgradeHealth", "UpdateStatRPC:playerHealth", "UpdateHealthRPC" }), "Native RPC order");
    }
    int messages = Fixture.Messages.Count;
    Set(1);
    Check(p.playerHealth.health == expected && Fixture.Messages.Count == messages, "Repeated reset is idempotent");
}
// Every permitted Health level decrease, including retained/purchased Base levels.
for (int oldLevel = 1; oldLevel <= 200; oldLevel++)
for (int nextLevel = 0; nextLevel < oldLevel; nextLevel++) {
    int max = 100 + 20 * oldLevel, nextMax = 100 + 20 * nextLevel;
    int hp = max * 7 / 13;
    var p = Fixture.Reset(hp, oldLevel);
    Set(nextLevel);
    Check(p.playerHealth.health == hp * nextMax / max && p.playerHealth.maxHealth == nextMax, "Configured levels preserve ratio");
}
{
    var p = Fixture.Reset(); SemiFunc.Shop = true;
    LifecyclePatches.SemiFuncOnSceneSwitchPrefix(false);
    Check(p.playerHealth.health == 300 && StageRolesPlugin.Instance.Controller.Assigned, "Canceled switch keeps role and health");
    LifecyclePatches.SemiFuncOnSceneSwitchPrefix(true);
    int savedHp = StatsManager.instance.GetPlayerHealth("p");
    int savedLevel = StatsManager.instance.FetchPlayerUpgrades("p")["playerUpgradeHealth"];
    Check(savedHp == 69 && savedLevel == 1, "Native scene save receives converted values");
    // Vanilla Fetch on the next scene/reload uses saved HP and 100 + 20*level.
    Check(Math.Clamp(savedHp, 1, 100 + 20 * savedLevel) == 69, "Scene/reload retains 69 HP");
    LifecyclePatches.SemiFuncOnSceneSwitchPrefix(true);
    Check(StatsManager.instance.GetPlayerHealth("p") == 69, "Second scene callback cannot rescale again");
}
foreach (bool multi in new[] { false, true }) {
    Fixture.Reset(multiplayer: multi); SemiFunc.Players.Clear(); Set(1);
    Check(StatsManager.instance.GetPlayerHealth("p") == 69, "Departed player saved health");
    if (multi) Check(Fixture.GuestStats["playerHealth"] == 69, "Departed player guest stats");
}
{
    var p = Fixture.Reset(60, 1); Set(2);
    Check(p.playerHealth.health == 80 && p.playerHealth.maxHealth == 140 && p.playerHealth.HealCalls == 1, "Positive upgrades retain vanilla behavior");
    UpgradeService.AddLevels("p", "Health", 1);
    Check(p.playerHealth.health == 100 && p.playerHealth.HealCalls == 2, "Consumed upgrade still grants HP");
    UpgradeService.AddLevelsHostAuthoritative("p", "Health", 1);
    Check(p.playerHealth.health == 120 && p.playerHealth.HealCalls == 3, "Host-authoritative grants unchanged");
}
{
    var p = Fixture.Reset();
    UpgradeService.EnsureAtLeastLevels("p", new[] { new UpgradeGrant("Health", "playerUpgradeHealth", 1) });
    Check(p.playerHealth.health == 300 && p.playerHealth.maxHealth == 520, "EnsureAtLeast does not remove HP");
    SemiFunc.Authority = false; Set(1);
    Check(p.playerHealth.health == 300 && StatsManager.instance.FetchPlayerUpgrades("p")["playerUpgradeHealth"] == 21, "Only authority can reset health");
}
{
    var p = Fixture.Reset(multiplayer: true); p.photonView = null; Set(1);
    Check(StatsManager.instance.GetPlayerHealth("p") == 300 && StatsManager.instance.FetchPlayerUpgrades("p")["playerUpgradeHealth"] == 21, "Missing transport leaves level/health intact");
    Check(Fixture.Warnings.Count == 1, "Missing transport diagnosed");
}
{
    var p = Fixture.Reset(); p.playerHealth.maxHealth = 620; Set(1);
    Check(p.playerHealth.maxHealth == 220 && p.playerHealth.health == 106, "Preserve unrelated maximum HP offset");
    p.playerHealth.UpdateHealthRPC(90, 220, true, false);
    Check(Fixture.ObservedDamage == 1, "Normal damage observation remains enabled");
}
Check(HealthUpgradeReset.ScaleRemaining(int.MaxValue, int.MaxValue, int.MaxValue) == int.MaxValue, "No multiplication overflow");
Console.WriteLine($"Health reset checks passed: {checks}");
''', encoding='utf-8')
(out / 'sources.json').write_text(json.dumps(sources, indent=2) + '\n', encoding='utf-8')
print(f'Generated {out}; {len(sources)} production source sections recorded.')
