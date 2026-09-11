using Mono.Cecil;
using Mono.Cecil.Cil;

int checks = 0;
void Check(bool pass, string label)
{
    if (!pass) throw new Exception(label);
    checks++;
}
using var game = AssemblyDefinition.ReadAssembly(@"E:\SteamLibrary\steamapps\common\REPO\REPO_Data\Managed\Assembly-CSharp.dll");
using var plugin = AssemblyDefinition.ReadAssembly(Path.GetFullPath("bin/Release/netstandard2.1/RoleShuffle.dll"));
TypeDefinition GameType(string name) => game.MainModule.Types.Single(t => t.Name == name);
MethodDefinition Method(string type, string name) => GameType(type).Methods.Single(m => m.Name == name);
TypeDefinition PluginType(string name) => plugin.MainModule.Types.Single(t => t.Name == name);
IEnumerable<MethodReference> Calls(MethodDefinition method) => method.Body.Instructions
    .Where(i => i.OpCode == OpCodes.Call || i.OpCode == OpCodes.Callvirt)
    .Select(i => (MethodReference)i.Operand);
var runtime = PluginType("MageStaffRuntime");
Check((float)runtime.Fields.Single(f => f.Name == "DurationMultiplier").Constant == 1.3f, "Multiplier is exactly 1.3");
Check(Method("SlowProjectile", "SetSpawner").Parameters.Single().Name == "spawnerObject", "Spawner patch parameter matches game");
foreach (string staff in new[] { "ItemStaffTorque", "ItemStaffZeroGravity", "ItemStaffVoid" })
{
    var calls = Calls(Method(staff, "CastSpell")).ToList();
    Check(calls.Any(m => m.DeclaringType.Name == "SlowProjectile" && m.Name == "SetSpawner"), staff + " identifies actual staff");
    Check(calls.Any(m => m.DeclaringType.Name == "SlowProjectile" && m.Name == "Launch"), staff + " launches after setup");
}
foreach (string name in new[] { "DestroyRPC", "ExplosionRPC" })
{
    var calls = Calls(Method("SlowProjectile", name)).ToList();
    Check(calls.Any(m => m.DeclaringType.FullName == "Photon.Pun.PhotonNetwork" &&
        m.Name == "Instantiate" && m.Parameters.Count == 5 &&
        m.Parameters[0].ParameterType.FullName == "System.String" &&
        m.Parameters[3].ParameterType.FullName == "System.Byte"), name + " network spawn matches transpiler");
    Check(calls.Any(m => m.DeclaringType.FullName == "UnityEngine.Object" &&
        m.Name == "Instantiate" && m is GenericInstanceMethod generic &&
        generic.GenericArguments[0].FullName == "UnityEngine.GameObject" &&
        m.Parameters.Count == 3 && m.Parameters[1].ParameterType.FullName == "UnityEngine.Vector3" &&
        m.Parameters[2].ParameterType.FullName == "UnityEngine.Quaternion"), name + " local spawn matches transpiler");
}
foreach (string name in new[] { "affectTimeMin", "affectTimeMax", "enemyHighestDifficultyTimeMin", "enemyHighestDifficultyTimeMax", "enemyLowestDifficultyTimeMin", "enemyLowestDifficultyTimeMax" })
    Check(GameType("SemiAreaOfEffect").Fields.Any(f => f.Name == name && f.IsPublic && f.FieldType.FullName == "System.Single"), name + " is public float");
Check(Calls(Method("SemiAreaOfEffect", "Update")).Any(m => m.Name == "RPC" &&
    m.Parameters.Any(p => p.ParameterType.FullName == "System.Object[]")), "Effect duration goes through vanilla RPC");
foreach (var pair in new[] { ("stateStart", "System.Boolean"), ("stateFixed", "System.Boolean"), ("stateTimer", "System.Single") })
    Check(GameType("SlowWalkerAttack").Fields.Any(f => f.Name == pair.Item1 && f.FieldType.FullName == pair.Item2), pair.Item1 + " injection matches actual field");
Check(Method("SlowWalkerAttack", "StateImplosion").Body.Instructions.Any(i => i.OpCode == OpCodes.Ldc_R4 && (float)i.Operand == 5f), "Void base duration is five seconds");
Check(Calls(Method("ValuableWizardStaff", "StaffLaser")).Count(m => m.DeclaringType.FullName == "UnityEngine.Random" && m.Name == "Range" && m.ReturnType.FullName == "System.Single") == 1, "Laser scales single sampled duration before RPC");
Check(!Calls(PluginType("MageRoleRuntime").Methods.Single(m => m.Name == "TryCastProjectile")).Any(m => m.DeclaringType.Name == "MageStaffRuntime"), "Chat/expression projectile path does not opt into staff bonus");
Check(PluginType("MageStaffRuntime").Methods.Single(m => m.Name == "IsMageStaff").Body.Instructions.Any(i =>
    i.Operand is MethodReference m && m.Name == "PlayerHasRole"), "Staff bonus requires active Mage capability");
Console.WriteLine($"PASS: {checks} installed-game IL and staff integration contract checks.");
Console.WriteLine("Static contract validation only; Unity gameplay and vanilla-client visuals need in-game testing.");
