using System.Reflection;
using HarmonyLib;
using Mono.Cecil;
using REPOJP.StageRoles;
using UnityEngine;

int checks = 0;
void Check(bool ok, string message) { if (!ok) throw new Exception(message); checks++; }
string path = args.Length > 0 ? args[0] : @"E:\SteamLibrary\steamapps\common\REPO\REPO_Data\Managed\Assembly-CSharp.dll";
using var game = AssemblyDefinition.ReadAssembly(path);
var nativeCowboy = game.MainModule.Types.Single(t => t.Name == nameof(ValuableShootingCowboy));
Check(nativeCowboy.BaseType.Name == "MonoBehaviour", "Cowboy does not inherit the automatic Trap support");
var flags = BindingFlags.Static | BindingFlags.NonPublic;
foreach (var field in typeof(EngineerCowboySuppression).GetFields(flags))
{
    var bindings = field.GetValue(null) switch { FieldInfo f => new[] { f }, FieldInfo[] fs => fs, _ => Array.Empty<FieldInfo>() };
    foreach (var binding in bindings)
        Check(nativeCowboy.Fields.Any(f => f.Name == binding.Name && f.FieldType.FullName == binding.FieldType.FullName), "Installed native reset field: " + binding.Name);
}
foreach (string name in EngineerEffectCatalog.FrameEffectTypeNames)
{
    var native = game.MainModule.Types.Single(t => t.Name == name);
    Check(native != null, "Installed catalog type: " + name);
    foreach (var method in native!.Methods.Where(m => new[] { "Update", "FixedUpdate", "LateUpdate" }.Contains(m.Name)))
        Check(method.Parameters.Count == 0 && !method.IsStatic && method.ReturnType.FullName == "System.Void", "Installed frame signature: " + name + "." + method.Name);
}
var activations = ((string TypeName, string MethodName)[])typeof(EngineerEffectActivationPatches).GetField("Targets", flags)!.GetValue(null)!;
foreach (var target in activations)
    Check(game.MainModule.Types.Single(t => t.Name == target.TypeName).Methods.Any(m => m.Name == target.MethodName && !m.IsStatic), "Installed activation target: " + target);
var nativeFire = nativeCowboy.Methods.Single(m => m.Name == "FireBullet");
foreach (string method in new[] { "IsMasterClientOrSingleplayer", "LoseValue", "AddForce", "RPC" })
    Check(nativeFire.Body.Instructions.Any(i => i.Operand is MethodReference m && m.Name == method), "Native authoritative shot path: " + method);
Check(nativeCowboy.Methods.Single(m => m.Name == "Update").Body.Instructions.Any(i => i.Operand is MethodReference m && m.Name == "HurtColliderTimer"), "Skipped Update owns bullet cleanup");

using (var audit = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "repo050-valuable-audit.json"))))
{
    foreach (var entry in audit.RootElement.GetProperty("components").EnumerateArray())
    {
        string name = entry.GetProperty("type").GetString()!;
        var native = game.MainModule.Types.Single(t => t.Name == name);
        foreach (var method in entry.GetProperty("methods").EnumerateArray())
            Check(native.Methods.Any(m => m.Name == method.GetString()), "Audited native effect exists: " + name + "." + method.GetString());
        bool automatic = native.BaseType.Name == "Trap";
        bool explicitTarget = EngineerEffectCatalog.FrameEffectTypeNames.Contains(name) || EngineerEffectCatalog.ModernEffectTypeNames.Contains(name) || activations.Any(a => a.TypeName == name);
        string status = entry.GetProperty("status").GetString()!;
        Check(status switch { "automatic_trap" => automatic, "cowboy_fixed" or "modern" => explicitTarget, _ => !automatic && !explicitTarget }, "Audit matches current coverage: " + name);
    }
}

var obj = new PhysGrabObject();
var cowboy = obj.Add(new ValuableShootingCowboy());
Check(EngineerEffectCatalog.IsEffectValuable(obj), "Cowboy alone is recognized without Trap");
var ordinary = new PhysGrabObject(); ordinary.Add(new DecorativeValuable());
Check(!EngineerEffectCatalog.IsEffectValuable(ordinary), "Ordinary decoration remains native");
var horse = new PhysGrabObject(); horse.Add(new ValuableHorseOnWheels());
Check(EngineerEffectCatalog.IsEffectValuable(horse), "New Trap-derived horse remains covered");
var excluded = new PhysGrabObject(); excluded.Add(new Trap()); excluded.Add(new ValuableCamera());
Check(!EngineerEffectCatalog.IsEffectValuable(excluded), "Existing host-only exclusions take priority");

foreach (var field in typeof(ValuableShootingCowboy).GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
{
    if (field.FieldType == typeof(float)) field.SetValue(cowboy, 0.8f);
    else if (field.FieldType == typeof(bool)) field.SetValue(cowboy, true);
}
EngineerCowboySuppression.Reset(cowboy);
Check(!cowboy.hurtCollider!.gameObject.activeSelf && !cowboy.lineRenderer!.gameObject.activeSelf, "Taking over an active shot removes its hitbox and beam");
Check(cowboy.shootEffects.All(p => !p.isPlaying), "Fired shot particles are cleared");
Check(!cowboy.armRotationLoop!.Playing && !cowboy.mechanicWhirringLoop!.Playing, "Paused Update does not strand looping audio");
Check(cowboy.arm!.localRotation == Quaternion.Euler(100, 0, 0) && cowboy.cowboyNeck!.localRotation == Quaternion.identity, "Native rest pose restored");
Check(cowboy.scarf1!.localRotation == Quaternion.identity && cowboy.scarf2!.localRotation == Quaternion.identity && cowboy.armShakeTransform!.localRotation == Quaternion.identity, "Animated attachments rest");
object Read(string name) => typeof(ValuableShootingCowboy).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(cowboy)!;
Check(!(bool)Read("grabbed") && !(bool)Read("shot") && (float)Read("reloadTimer") == 0 && (float)Read("armRotationTimer") == 0 && (float)Read("currentArmAngle") == 100, "Native next grab starts a full ordinary wind-up");
Check((float)Read("armReturnTimer") == (float)Read("armReturnTime"), "Configured return timing preserved");
Check(cowboy.recoilForce == 5, "Native weapon tuning is unchanged");
var fresh = new ValuableShootingCowboy();
Check(fresh.hurtCollider!.gameObject.activeSelf, "Reset does not alter a different object");

var framePatch = typeof(EngineerTrapUpdatePatches);
var activationPatch = typeof(EngineerEffectActivationPatches);
var frameTargets = ((IEnumerable<MethodBase>)framePatch.GetMethod("TargetMethods", flags)!.Invoke(null, null)!).ToArray();
var activationTargets = ((IEnumerable<MethodBase>)activationPatch.GetMethod("TargetMethods", flags)!.Invoke(null, null)!).ToArray();
Check(frameTargets.Any(m => m.DeclaringType == typeof(ValuableShootingCowboy) && m.Name == "Update"), "Cowboy frame hook selected");
Check(frameTargets.Count(m => m.DeclaringType == typeof(ValuableHorseOnWheels)) == 2, "Horse frame hooks selected automatically");
Check(activationTargets.Count(m => m.DeclaringType == typeof(ValuableShootingCowboy)) == 2, "Both Cowboy activation hooks selected");
// Invoke the production prefixes directly. The bundled Mono runtime's Harmony
// detours cannot execute under the .NET 9 check runner.
void Dispatch(string method, bool frame = false)
{
    var patch = frame ? framePatch : activationPatch;
    var prefix = patch.GetMethod(frame ? "EffectFramePrefix" : "EffectActivationPrefix", flags)!;
    if ((bool)prefix.Invoke(null, new object[] { cowboy })!) typeof(ValuableShootingCowboy).GetMethod(method)!.Invoke(cowboy, null);
}
var controller = StageRolesPlugin.Instance!.Controller!;
controller.Suppress = true;
Dispatch("Update", true); Dispatch("Shoot"); Dispatch("FireBullet");
Check(cowboy.FrameCalls == 0 && cowboy.ShootCalls == 0 && cowboy.FireCalls == 0, "Suppression blocks animation, shooting and direct bullet entry points");
controller.Suppress = false;
Dispatch("Update", true); Dispatch("Shoot"); Dispatch("FireBullet");
Check(cowboy.FrameCalls == 1 && cowboy.ShootCalls == 1 && cowboy.FireCalls == 1, "Release or loss of eligibility resumes all native entry points");
controller.Suppress = true;
cowboy.hurtCollider = null; cowboy.lineRenderer = null; cowboy.arm = null; cowboy.cowboyNeck = null; cowboy.armShakeTransform = null; cowboy.scarf1 = null; cowboy.scarf2 = null;
Dispatch("Update", true);
Check(cowboy.FrameCalls == 1, "Missing optional visuals do not break suppression");
StageRolesPlugin.Instance = null;
Dispatch("Update", true);
Check(cowboy.FrameCalls == 2, "Absent controller leaves native behavior alone");
ModernChecks.Run(game, Check);
Console.WriteLine($"PASS: {checks} Engineer catalog, native bindings, cleanup and prefix lifecycle checks.");
