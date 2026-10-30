using System.Reflection;
using HarmonyLib;
using Mono.Cecil;
using Photon.Pun;
using REPOJP.StageRoles;
using UnityEngine;

int checks = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new Exception(name);
    checks++;
}
var hooks = typeof(RoleExpressionPatches).GetMethods(BindingFlags.NonPublic | BindingFlags.Static);
void Call(string hook, params object[] args) => hooks.Single(m => m.Name == hook).Invoke(null, args);
var casts = StageRolesPlugin.Instance.Controller.Calls;
void Toggle(PlayerAvatar p, int index) => Call("TogglePrefix", p.playerExpression, index);
void Hold(PlayerAvatar p, int index, bool input = true) => Call("HoldPrefix", p.playerExpression, index, input);
void Set(PlayerAvatar p, int index, float percent = 100, bool owner = true) =>
    Call("SetPostfix", p, index, percent, new PhotonMessageInfo { Rejected = !owner });
void Stop(PlayerAvatar p, int index) => Call("StopPostfix", p, index, new PhotonMessageInfo());

// Exercise the production callbacks in native toggle, held-key and menu event
// sequences. A controller stand-in counts requests before cooldowns can hide a
// spurious cast (or make a regression test pass accidentally).
foreach (int index in Enumerable.Range(1, 6))
{
    var local = new PlayerAvatar();
    MenuManager.instance = new();
    Time.unscaledTime = 0;
    int before = casts.Count;
    Toggle(local, index);
    Time.unscaledTime = 0.1f;
    Set(local, index);
    Check(casts.Count == before + 1 && casts[^1] == (local, index), "Quick toggle tap casts once after native delay");
    Set(local, index);
    Check(casts.Count == before + 1, "Repeated set without input does not recast");

    Time.unscaledTime = 10; // well beyond Mage's cooldown
    MenuManager.instance.PageForTests = new();
    Stop(local, index);
    Check(casts.Count == before + 1, "Escape opening / expression expiry cannot cast");
    MenuManager.instance.PageForTests = null;
    Hold(local, index, input: false);
    Set(local, index);
    Check(casts.Count == before + 1, "Menu close restores toggled expression without casting");

    Toggle(local, index);
    Stop(local, index);
    Set(local, index);
    Check(casts.Count == before + 1, "Toggle off clears intent and cannot cast on later restoration");
    Toggle(local, index);
    Set(local, index, 0);
    Set(local, index);
    Check(casts.Count == before + 1, "Zero-percent reset does not leave a pending cast");

    Toggle(local, index);
    Time.unscaledTime += 1;
    Set(local, index);
    Check(casts.Count == before + 1, "Expired toggle cannot authorize an automatic expression");
    Hold(local, index);
    Set(local, index);
    Check(casts.Count == before + 2, "Deliberate held expression still casts after menu closes");
    Hold(local, index); // native hold repeats each frame without new set RPC
    MenuManager.instance.PageForTests = new();
    Stop(local, index);
    MenuManager.instance.PageForTests = null;
    Hold(local, index, input: false);
    Set(local, index);
    Check(casts.Count == before + 2, "Menu stop clears repeated hold intent");

    Toggle(local, index);
    Set(local, index, owner: false);
    Check(casts.Count == before + 2, "Unowned RPC cannot cast");
    Set(local, index);
    Check(casts.Count == before + 3, "Unowned RPC cannot consume the owner's valid input");
    Toggle(local, index);
    MenuManager.instance.PageForTests = new();
    Set(local, index);
    Check(casts.Count == before + 3, "Menu opening before delayed toggle blocks cast");
    Toggle(local, index);
    MenuManager.instance.PageForTests = null;
    Set(local, index);
    Check(casts.Count == before + 3, "Input inside menu cannot leak into gameplay");
    Call("TogglePrefix", new PlayerExpression(local), index);
    Set(local, index);
    Check(casts.Count == before + 3, "Menu preview avatar cannot authorize gameplay casts");

    var peer = new PlayerAvatar { IsLocalForTests = false };
    MenuManager.instance.PageForTests = new();
    Set(peer, index);
    Check(casts.Count == before + 4 && casts[^1] == (peer, index), "Vanilla peer can cast while host has menu open");
    Stop(peer, index);
    Set(peer, index, 0);
    Set(peer, index, owner: false);
    Check(casts.Count == before + 4, "Peer stop, reset and unowned RPC cannot cast");
}

// v0.5.0 wheel selections are committed once, not every animation frame.
var wheelPlayer = new PlayerAvatar(); MenuManager.instance = new(); Time.unscaledTime = 0;
int wheelBefore = casts.Count;
Call("WheelPostfix", wheelPlayer.playerExpression, new List<int> { 1, 3, 0, 99 });
Set(wheelPlayer, 1); Set(wheelPlayer, 3); Set(wheelPlayer, 1); Set(wheelPlayer, 99);
Check(casts.Count == wheelBefore + 2, "Committed wheel expressions each authorize one cast; invalid selections cannot");
Call("WheelPostfix", new PlayerExpression(wheelPlayer), new List<int> { 2 }); Set(wheelPlayer, 2);
Check(casts.Count == wheelBefore + 2, "Wheel preview avatar cannot authorize a cast");
MenuManager.instance.PageForTests = new();
Call("WheelPostfix", wheelPlayer.playerExpression, new List<int> { 2 });
MenuManager.instance.PageForTests = null; Set(wheelPlayer, 2);
Check(casts.Count == wheelBefore + 2, "Menu wheel input cannot leak into gameplay");
Call("WheelPostfix", wheelPlayer.playerExpression, new List<int> { 2 }); Time.unscaledTime = 1; Set(wheelPlayer, 2);
Check(casts.Count == wheelBefore + 2, "Expired wheel intent cannot authorize later restoration");

bool SameType(TypeReference native, Type managed) => native is GenericInstanceType generic
    ? managed.IsGenericType && generic.ElementType.FullName == managed.GetGenericTypeDefinition().FullName &&
        generic.GenericArguments.Zip(managed.GetGenericArguments()).All(pair => SameType(pair.First, pair.Second))
    : native.FullName == managed.FullName;

// Confirm the patch target names and parameter bindings against the installed
// game, not just against the stand-ins above. No proprietary code is copied.
string path = args.Length > 0 ? args[0] : @"E:\SteamLibrary\steamapps\common\REPO\REPO_Data\Managed\Assembly-CSharp.dll";
using var game = AssemblyDefinition.ReadAssembly(path);
foreach (var hook in hooks)
foreach (var patch in hook.GetCustomAttributes<HarmonyPatch>())
{
    var target = game.MainModule.Types.Single(t => t.Name == patch.info.declaringType.Name)
        .Methods.Single(m => m.Name == patch.info.methodName);
    foreach (var parameter in hook.GetParameters().Where(p => p.Name != "__instance"))
        Check(target.Parameters.Any(p => p.Name == parameter.Name && SameType(p.ParameterType, parameter.ParameterType)),
            $"Installed {target.Name} accepts {parameter.Name}");
}
var expression = game.MainModule.Types.Single(t => t.Name == "PlayerExpression");
var update = expression.Methods.Single(m => m.Name == "Update");
Check(update.Body.Instructions.Any(i => i.Operand is FieldReference f && f.Name == "currentMenuPage"), "Native expression update gates on menu page");
Check(expression.Methods.Single(m => m.Name == "StopExpression").Body.Instructions.Any(i => i.Operand is MethodReference m && m.Name == "PlayerExpressionStop"), "Native expression expiry emits the stop notification");
Console.WriteLine($"Expression checks passed: {checks}");
