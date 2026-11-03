using System.Reflection;
using HarmonyLib;
using Mono.Cecil;
using REPOJP.StageRoles;
using UnityEngine;
using Photon.Pun;

internal static class ModernChecks
{
    private const BindingFlags InstanceFields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private static void Set(object target, string name, object value) => target.GetType().GetField(name, InstanceFields)!.SetValue(target, value);
    private static T Get<T>(object target, string name) => (T)target.GetType().GetField(name, InstanceFields)!.GetValue(target)!;
    private static T Add<T>() where T : Component, new() => new PhysGrabObject().Add(new T());

    internal static void Run(AssemblyDefinition game, Action<bool, string> check)
    {
        var nativeTypes = game.MainModule.Types.ToDictionary(t => t.Name);
        foreach (var binding in typeof(EngineerModernEffects).GetFields(BindingFlags.Static | BindingFlags.NonPublic))
        {
            if (binding.GetValue(null) is FieldInfo field)
                check(nativeTypes[field.DeclaringType!.Name].Fields.Any(f => f.Name == field.Name && f.FieldType.Name == field.FieldType.Name), "Modern native field binding: " + field.Name);
            if (binding.GetValue(null) is MethodInfo method)
                check(nativeTypes[method.DeclaringType!.Name].Methods.Any(m => m.Name == method.Name && !m.IsStatic && m.Parameters.Count == method.GetParameters().Length), "Modern native cleanup method: " + method.Name);
        }
        foreach (var (type, method) in EngineerModernActionPatch.Targets)
            check(nativeTypes[type].Methods.Any(m => m.Name == method && !m.IsStatic && m.ReturnType.FullName == "System.Void"), "Void activation contract: " + type + "." + method);
        foreach (var (type, method) in EngineerModernConditionPatch.Targets)
            check(nativeTypes[type].Methods.Any(m => m.Name == method && !m.IsStatic && m.ReturnType.FullName == "System.Boolean"), "Boolean activation contract: " + type + "." + method);
        check(nativeTypes["ValuableDesertGolfcart"].Methods.Single(m => m.Name == "Driver").ReturnType.Name == nameof(PlayerAvatar), "Driver patch has the native result type");
        check(EngineerModernActionPatch.Resolve(EngineerModernActionPatch.Targets).Count() == EngineerModernActionPatch.Targets.Length, "All void hook selectors resolve");
        check(EngineerModernActionPatch.Resolve(EngineerModernConditionPatch.Targets).Count() == EngineerModernConditionPatch.Targets.Length, "All condition hook selectors resolve");
        var frameTargets = ((IEnumerable<MethodBase>)typeof(EngineerModernFramePatch).GetMethod("TargetMethods", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, null)!).ToArray();
        check(frameTargets.Any(m => m.DeclaringType == typeof(EnemyBangFuse)), "Standalone fuse gets its own frame hook");
        foreach (var method in frameTargets)
            check(nativeTypes[method.DeclaringType!.Name].Methods.Any(m => m.Name == method.Name && m.Parameters.Count == 0 && m.ReturnType.FullName == "System.Void"), "Native modern frame exists: " + method);

        bool Calls(string type, string method, string called) => nativeTypes[type].Methods.Single(m => m.Name == method).Body.Instructions.Any(i => i.Operand is MethodReference r && r.Name == called);
        bool Writes(string type, string method, string field) => nativeTypes[type].Methods.Single(m => m.Name == method).Body.Instructions.Any(i => i.OpCode.Code == Mono.Cecil.Cil.Code.Stfld && i.Operand is FieldReference r && r.Name == field);
        foreach (string method in new[] { "GrabPlayerAddRPC", "GrabPlayerRemoveRPC" })
            check(Calls("PhysGrabObject", method, "MasterOnlyRPC"), "Visibility uses master-authenticated RPC: " + method);
        check(Calls("SnakeInACanValuable", "SnakeIn", "RPC") && Writes("SnakeInACanValuable", "SnakeInRPC", "hurtColliderTimer"), "Native snake retraction broadcasts and expires the hitbox");
        check(Calls("ValuableDesertEnergyCrystalChunk", "SetState", "RPC"), "Energy idle state reaches vanilla clients");
        check(Calls("ValuableDesertExplosiveTower", "StateReset", "SetHandleCollider"), "Tower reset restores handle collider");
        check(Calls("ValuableDesertExplosiveTowerDynamite", "StandaloneSetup", "RPC") && Writes("ValuableDesertExplosiveTowerDynamite", "StandaloneSetupRPC", "standaloneFuseSpeed"), "Fuse pause speed is natively synchronized");
        check(Calls("ValuableMusicInstrument", "StateAttached", "IsGrabbed") && Writes("ValuableMusicInstrument", "StateDetaching", "active"), "False grab condition reaches native instrument cleanup");
        check(Calls("ValuableMusicInstrument", "StateDetaching", "SetInvestigate"), "Direct detachment noise must be scoped out");
        check(Calls("ValuableDesertGolfcart", "StateMachine", "Driver") && Calls("ValuableDesertGolfcart", "SetThrottleRPC", "Driver"), "Driver gate covers state and owner throttle reception");
        check(Calls("ValuableDesertZeroGravCrystal", "AffectRenewLogic", "AffectTimerSetFull"), "Zero gravity renewal entry point checked");
        check(Writes("SemiAffect", "Update", "timer"), "Existing affects have independent native expiry");
        check(Calls("ValuableWizardCrystal", "Destroyed", "AffectMultiplayer"), "Wizard destruction gate precedes effect propagation");
        foreach (string method in new[] { "GrowthLogic", "GrowthConfirm", "FinalBurst", "OnDestroy", "OnDisable", "PlatformBindingsTick" })
            check(!EngineerModernActionPatch.Targets.Any(t => t.TypeName == "ValuableCubeOfKnowledge" && t.MethodName == method), "Cube bookkeeping stays native: " + method);

        StageRolesPlugin.Instance = new();
        StageRolesPlugin.Instance.Controller!.Suppress = true;
        foreach (string name in EngineerEffectCatalog.ModernEffectTypeNames)
        {
            var component = (Component)Activator.CreateInstance(AccessTools.TypeByName(name))!;
            var obj = new PhysGrabObject(); obj.Add(component);
            check(EngineerEffectCatalog.IsEffectValuable(obj), "Modern item recognized: " + name);
            check(!EngineerModernActionPatch.Prefix(component), "Actions blocked for Engineer: " + name);
            bool result = true;
            check(!EngineerModernConditionPatch.Prefix(component, ref result) && !result, "Conditions explicitly return false: " + name);
            StageRolesPlugin.Instance.Controller.Suppress = false;
            check(EngineerModernActionPatch.Prefix(component), "Actions resume after role loss/release: " + name);
            result = true;
            check(EngineerModernConditionPatch.Prefix(component, ref result) && result, "Native conditions untouched on release: " + name);
            StageRolesPlugin.Instance.Controller.Suppress = true;
        }
        var horse = Add<ValuableHorseOnWheels>(); Set(horse, "currentState", ValuableHorseOnWheels.State.MoveForward);
        EngineerModernEffects.RestHorse(horse);
        check(Get<ValuableHorseOnWheels.State>(horse, "currentState") == ValuableHorseOnWheels.State.Idle && horse.MaterialReset && !horse.soundMove!.Playing, "Running horse returns to idle, normal friction and quiet loop");
        Set(horse, "currentState", ValuableHorseOnWheels.State.Inactive); EngineerModernEffects.RestHorse(horse);
        check(Get<ValuableHorseOnWheels.State>(horse, "currentState") == ValuableHorseOnWheels.State.Inactive, "Inactive horse is not awakened");
        check(Calls("ValuableHorseOnWheels", "UpdateState", "RPC"), "Horse rest reaches native guests");
        check(Calls("ValuableDesertExplosiveTowerDynamite", "StandaloneSetupRPC", "MasterOnlyRPC"), "Fuse changes require native master authentication");
        check(nativeTypes["ValuableDesertEnergyCrystalChunk"].Methods.Single(m => m.Name == "Energized").Body.Instructions.Any(i => i.Operand is FieldReference f && f.Name == "currentState"), "Energy benefits stop through synchronized idle state even when grabbedLocal remains true");
        var snake = Add<SnakeInACanValuable>(); Set(snake, "snakeOut", true);
        check(EngineerModernEffects.BeforeFrame(snake, true) && snake.Retracted && !snake.Hitbox, "Active snake retracts through native cleanup; frame keeps running");
        var bird = Add<ValuableDesertBirdCage>(); Set(bird, "stateCurrent", ValuableDesertBirdCage.State.Dead); Set(bird, "deathTimer", 2f);
        check(EngineerModernEffects.BeforeFrame(bird, true) && Get<ValuableDesertBirdCage.State>(bird, "stateCurrent") == ValuableDesertBirdCage.State.Idle && Get<float>(bird, "deathTimer") == 0, "Bird returns to safe idle");
        var energy = Add<ValuableDesertEnergyCrystalChunk>(); Set(energy, "currentState", ValuableDesertEnergyCrystalChunk.States.Active);
        EngineerModernEffects.BeforeFrame(energy, true); EngineerModernEffects.BeforeFrame(energy, true);
        check(energy.StateChanges == 1 && Get<ValuableDesertEnergyCrystalChunk.States>(energy, "currentState") == ValuableDesertEnergyCrystalChunk.States.Idle, "Energy broadcasts idle once; native expiry remains active");
        var tower = Add<ValuableDesertExplosiveTower>(); Set(tower, "stateCurrent", ValuableDesertExplosiveTower.State.Trigger);
        var particles = tower.Owner!.Add(new ParticleSystem());
        EngineerModernEffects.BeforeFrame(tower, true); EngineerModernEffects.BeforeFrame(tower, true);
        check(tower.StateChanges == 1 && Get<ValuableDesertExplosiveTower.State>(tower, "stateCurrent") == ValuableDesertExplosiveTower.State.Reset && !particles.isPlaying, "Armed tower enters native reset once");
        Set(tower, "stateCurrent", ValuableDesertExplosiveTower.State.Destroy);
        EngineerModernEffects.BeforeFrame(tower, true);
        check(tower.StateChanges == 1, "Destroyed tower is not resurrected");
        var lighter = Add<LighterValuable>(); Set(lighter, "openProgress", 1f); Set(lighter, "grabbed", true);
        check(!EngineerModernEffects.BeforeFrame(lighter, true) && !lighter.candleLight!.activeSelf && !Get<bool>(lighter, "grabbed"), "Lighter stops light and auto alignment");
        check(EngineerModernEffects.BeforeFrame(lighter, false), "Lighter native frame resumes");
        var popcorn = Add<ValuableDesertPopcornStand>(); Set(popcorn, "revValue", 1f);
        check(EngineerModernEffects.BeforeFrame(popcorn, true) && Get<float>(popcorn, "revValue") == 0, "Popcorn leaves native fade-out and lamps running");
        var zero = Add<ValuableDesertZeroGravCrystal>();
        check(EngineerModernEffects.BeforeFrame(zero, true), "Zero gravity keeps native effect cleanup running");
        var cart = Add<ValuableDesertGolfcart>();
        EngineerModernEffects.BeforeFrame(cart, true);
        PlayerAvatar? driver = new();
        check(cart.currentState == ValuableDesertGolfcart.State.Idle && !cart.hurtCollider!.gameObject.activeSelf && !EngineerGolfcartDriverPatch.Prefix(cart, ref driver) && driver == null, "Cart idles, removes hitbox, and rejects driving while held");
        var cube = Add<ValuableCubeOfKnowledge>();
        check(EngineerModernEffects.BeforeFrame(cube, true) && cube.ActivatedSides == 7 && cube.Value == 12345 && Get<List<PhysGrabObject>>(cube, "rejectedItems").Count == 0 && cube.TalkCleanups == 1 && cube.ZapCleanups == 1, "Cube cleanup preserves accumulated progress/value and clears queued forces");

        var dynamite = Add<ValuableDesertExplosiveTowerDynamite>(); var fuse = dynamite.Owner!.Add(new EnemyBangFuse());
        float progress = Get<float>(fuse, "standaloneFuseLerp");
        check(!EngineerModernEffects.BeforeFrame(fuse, true) && dynamite.SentSpeeds.Single() == float.MaxValue, "Fuse countdown pauses on native clients");
        EngineerModernEffects.BeforeFrame(fuse, true);
        check(dynamite.SentSpeeds.Count == 1 && Get<float>(fuse, "standaloneFuseLerp") == progress, "Pause is idempotent and never resets elapsed fuse");
        check(!fuse.soundFuseLoop!.Playing && !fuse.particleFire!.isPlaying && !fuse.tipObject!.activeSelf && !Get<bool>(fuse, "active"), "Local fuse visuals end and can rearm later");
        check(EngineerModernEffects.BeforeFrame(fuse, false) && dynamite.SentSpeeds.Last() == 12 && Get<float>(fuse, "standaloneFuseLerp") == progress, "Release restores original fuse speed and remaining time");
        EngineerModernEffects.BeforeFrame(fuse, true); EngineerModernEffects.Clear();
        check(dynamite.SentSpeeds.Count == 4 && dynamite.SentSpeeds.Last() == 12, "Stage end restores paused fuse");
        var enemyFuse = Add<EnemyBangFuse>(); enemyFuse.standalone = false;
        check(EngineerModernEffects.BeforeFrame(enemyFuse, true), "Enemy fuse is never suppressed");

        var instrument = Add<ValuableMusicInstrument>();
        check(EngineerModernFramePatch.Prefix(instrument, out bool scope) && scope && !EngineerInstrumentNoisePatch.Prefix(), "Native instrument detachment runs in quiet scope");
        try { throw new Exception("simulate native exception"); }
        catch { EngineerModernFramePatch.Finalizer(scope); }
        check(EngineerInstrumentNoisePatch.Prefix(), "Finalizer restores noise after native exception");
        EngineerModernFramePatch.Prefix(instrument, out bool outer);
        EngineerModernFramePatch.Prefix(instrument, out bool inner);
        EngineerModernFramePatch.Finalizer(inner);
        check(!EngineerInstrumentNoisePatch.Prefix(), "Nested quiet scope stays quiet");
        EngineerModernFramePatch.Finalizer(outer);
        check(EngineerModernEffects.QuietInstrumentDepth == 0, "Nested scopes leave no global mute");
        StageRolesPlugin.Instance = null;
        check(EngineerModernActionPatch.Prefix(instrument), "No controller means no suppression");

        Visibility(check);
    }

    private static void Visibility(Action<bool, string> check)
    {
        var visibility = new EngineerGrabVisibility();
        var obj = new PhysGrabObject();
        var a = new PhysGrabber { photonView = new() { ViewID = 101 } };
        var b = new PhysGrabber { photonView = new() { ViewID = 202 } };
        obj.playerGrabbing.Add(a); obj.playerGrabbing.Add(b);
        visibility.Hide(obj); visibility.Hide(obj);
        check(obj.playerGrabbing.Count == 2 && obj.photonView!.Calls.Count == 2 && obj.photonView.Calls.All(c => c.Method == "GrabPlayerRemoveRPC" && c.Target == RpcTarget.Others), "Co-holders hidden once on guests; host physics holders preserved");
        var c = new PhysGrabber { photonView = new() { ViewID = 303 } }; obj.playerGrabbing.Add(c); visibility.Hide(obj);
        check(obj.photonView!.Calls.Count == 3 && obj.photonView.Calls.Last().Id == 303, "Later co-holder also hidden");
        obj.playerGrabbing.Remove(a);
        visibility.Tick(_ => true);
        check(obj.photonView.Calls.Count == 3, "Release grace keeps others suppressed");
        visibility.Tick(_ => false);
        var adds = obj.photonView.Calls.Where(c => c.Method == "GrabPlayerAddRPC").ToArray();
        check(adds.Length == 2 && adds.Select(x => x.Id).Order().SequenceEqual(new[] { 202, 303 }), "Role loss restores only remaining holders");
        visibility.Clear(); check(obj.photonView.Calls.Count == 5, "Repeated cleanup never duplicates restored holders");
        visibility.Hide(obj); visibility.NativeGrabWillBeVisible(obj, 202); visibility.Clear();
        check(obj.photonView.Calls.Count(c => c.Method == "GrabPlayerAddRPC" && c.Id == 202) == 1, "Fresh native grab is not added twice on restore");
        var separate = new PhysGrabObject(); separate.playerGrabbing.Add(a);
        int previous = separate.photonView!.Calls.Count;
        visibility.Hide(obj); visibility.Tick(_ => false);
        check(separate.photonView.Calls.Count == previous, "Other valuable never changed");
        SemiFunc.Multiplayer = false; int count = obj.photonView.Calls.Count;
        visibility.Hide(obj); visibility.Clear(); check(obj.photonView.Calls.Count == count, "Solo does not send network holder changes");
        SemiFunc.Multiplayer = true; SemiFunc.Authority = false;
        visibility.Hide(obj); check(obj.photonView.Calls.Count == count, "Guest never sends authoritative holder changes");
        SemiFunc.Authority = true;
    }
}
