using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Mono.Cecil;
using REPOJP.StageRoles;

int checks = 0;
void Check(bool result, string label)
{
    if (!result) throw new Exception(label);
    checks++;
}
bool Close(float a, float b) => Math.Abs(a - b) < 0.01f;

// Read the installed game's actual Update IL, rather than assuming that
// MenuLib's obsolete scrollSpeed hook runs. Unity objects remain stand-ins.
string gamePath = args.Length > 0 ? args[0] : @"E:\SteamLibrary\steamapps\common\REPO\REPO_Data\Managed\Assembly-CSharp.dll";
using var game = AssemblyDefinition.ReadAssembly(gamePath);
var update = game.MainModule.Types.Single(t => t.Name == "MenuScrollBox").Methods.Single(m => m.Name == "Update");
var opcodes = typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
    .Where(f => f.FieldType == typeof(OpCode)).Select(f => (OpCode)f.GetValue(null)!)
    .GroupBy(o => o.Value).ToDictionary(g => g.Key, g => g.First());
object? Operand(object? value)
{
    if (value is MethodReference method && method.DeclaringType.Name == "SemiFunc" && method.Name == "InputScrollY")
        return typeof(SemiFunc).GetMethod(method.Name)!;
    if (value is FieldReference field && field.DeclaringType.Name == "MenuScrollBox")
        return (object?)typeof(MenuScrollBox).GetField(field.Name) ?? value;
    return value;
}
var original = update.Body.Instructions.Select(i => new CodeInstruction(opcodes[i.OpCode.Value], Operand(i.Operand))).ToList();
var patched = RoleGuideScrollPatch.Transpiler(original).ToList();
var helper = typeof(RoleGuideScrollPatch).GetMethod("WheelStep", BindingFlags.Static | BindingFlags.NonPublic)!;
Check(patched.Count == original.Count + 6, "Installed game matches exactly one wheel calculation");
Check(patched.Count(i => i.Calls(helper)) == 1, "Wheel helper is inserted exactly once");
Check(patched.Where(original.Contains).SequenceEqual(original), "All native gates, keyboard math, bounds and animation instructions are retained in order");
Check(original.All(i => patched.Contains(i) && i.labels.Count == 0), "Original instructions and branch targets are retained");

// Execute the actual wheel-expression IL after applying the production
// transpiler. This catches a patch that changes an unused speed property.
int helperIndex = patched.FindIndex(i => i.Calls(helper));
var dynamic = new DynamicMethod("PatchedNativeWheel", typeof(float), new[] { typeof(MenuScrollBox) }, typeof(Program).Module, true);
var il = dynamic.GetILGenerator();
foreach (var instruction in patched.GetRange(helperIndex - 11, 12))
{
    switch (instruction.operand)
    {
        case null: il.Emit(instruction.opcode); break;
        case float number: il.Emit(instruction.opcode, number); break;
        case FieldInfo field: il.Emit(instruction.opcode, field); break;
        case MethodInfo method: il.Emit(instruction.opcode, method); break;
        default: throw new Exception("Unexpected operand in installed wheel expression");
    }
}
il.Emit(OpCodes.Ret);
var wheelStep = dynamic.CreateDelegate<Func<MenuScrollBox, float>>();
var box = RoleMenu.Box;
foreach (float wheel in new[] { 0.01f, 1f, 120f, 600f, -0.01f, -120f })
foreach (var geometry in new[] { (1000f, 50000f), (320f, 4000f), (250f, 100f) })
foreach (float staleHeight in new[] { 200f, 4000f, 100000f })
{
    SemiFunc.Scroll = wheel;
    UnityEngine.Input.mouseScrollDelta = new UnityEngine.Vector2 { y = 0 };
    box.scrollHeight = staleHeight;
    box.scrollerStartPosition = geometry.Item2;
    box.scrollBarBackground.rect = new UnityEngine.Rect { height = geometry.Item1 };
    float delta = wheelStep(box);
    float contentDistance = delta * geometry.Item2 / (geometry.Item1 - box.scrollHandle.sizeDelta.y);
    Check(Close(contentDistance, wheel / 120f * 97.5f), "Actual patched expression gives three body lines per detent despite stale native height or page geometry");
}

SemiFunc.Scroll = 120;
float single = wheelStep(box);
SemiFunc.Scroll = 600;
Check(Close(wheelStep(box), single * 5), "Five detents in one low-FPS input frame equal five separate detents");
SemiFunc.Scroll = 60;
Check(Close(wheelStep(box) * 2, single), "Fractional high-resolution input retains its accumulated distance");

SemiFunc.Scroll = 120;
foreach (float invalid in new[] { 0f, float.NaN, float.PositiveInfinity, float.NegativeInfinity })
{
    SemiFunc.Scroll = invalid;
    Check(wheelStep(box) == 0, "Idle or invalid input adds no movement");
}
SemiFunc.Scroll = 120;
var otherBox = new MenuScrollBox();
Check(Close(wheelStep(otherBox), 120 / (otherBox.scrollHeight * 0.01f)), "Unrelated menus keep the native wheel calculation");
RoleMenu.Open = false;
Check(Close(wheelStep(box), 120 / (box.scrollHeight * 0.01f)), "Closed menu keeps the native calculation");
RoleMenu.Open = true;
box.scrollerStartPosition = 0;
Check(wheelStep(box) == 0, "Content with no travel adds no movement");
box.scrollerStartPosition = 100;
box.scrollBarBackground.rect = new UnityEngine.Rect { height = 40 };
Check(wheelStep(box) == 0, "A full-height handle adds no movement");
bool rejected = false;
try { RoleGuideScrollPatch.Transpiler(new[] { new CodeInstruction(OpCodes.Ret) }).ToList(); }
catch (InvalidOperationException) { rejected = true; }
Check(rejected, "Unsupported game IL fails explicitly instead of silently installing an ineffective patch");

// Verify why the previous implementation was ineffective with the pinned DLL.
using var menuLib = AssemblyDefinition.ReadAssembly(Path.GetFullPath("lib/MenuLib.dll"));
var entry = menuLib.MainModule.Types.Single(t => t.FullName == "MenuLib.Entry");
var awake = entry.Methods.Single(m => m.Name == "Awake");
Check(entry.Methods.Any(m => m.Name == "MenuScrollBox_UpdateILHook"), "Obsolete MenuLib hook still exists");
Check(!awake.Body.Instructions.Any(i => i.Operand is MethodReference m && m.Name == "MenuScrollBox_UpdateILHook"), "Installed MenuLib does not register the obsolete scrollSpeed hook");
Console.WriteLine($"PASS: {checks} scroll checks using installed-game IL and the production transpiler.");
Console.WriteLine("Unity input polling, patch installation, rendering and scene transitions still require in-game verification.");
