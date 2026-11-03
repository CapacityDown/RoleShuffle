using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace REPOJP.StageRoles;

internal static class EngineerModernEffects
{
    internal static int QuietInstrumentDepth;
    private static readonly Dictionary<EnemyBangFuse, float> PausedFuses = new();
    private static readonly FieldInfo HorseState = Field<ValuableHorseOnWheels>("currentState");
    private static readonly MethodInfo HorseSetState = Method<ValuableHorseOnWheels>("UpdateState");
    private static readonly MethodInfo HorseIdle = Method<ValuableHorseOnWheels>("StateIdle");
    private static readonly FieldInfo SnakeOut = Field<SnakeInACanValuable>("snakeOut");
    private static readonly MethodInfo SnakeIn = Method<SnakeInACanValuable>("SnakeIn");
    private static readonly FieldInfo BirdState = Field<ValuableDesertBirdCage>("stateCurrent");
    private static readonly FieldInfo BirdChirp = Field<ValuableDesertBirdCage>("chirpTimer");
    private static readonly FieldInfo BirdDeath = Field<ValuableDesertBirdCage>("deathTimer");
    private static readonly MethodInfo BirdSetState = Method<ValuableDesertBirdCage>("StateSet");
    private static readonly MethodInfo BirdRest = Method<ValuableDesertBirdCage>("PoseReset");
    private static readonly FieldInfo EnergyState = Field<ValuableDesertEnergyCrystalChunk>("currentState");
    private static readonly MethodInfo EnergySetState = Method<ValuableDesertEnergyCrystalChunk>("SetState");
    private static readonly FieldInfo TowerState = Field<ValuableDesertExplosiveTower>("stateCurrent");
    private static readonly MethodInfo TowerSetState = Method<ValuableDesertExplosiveTower>("StateSet");
    private static readonly FieldInfo TowerGrabTimer = Field<ValuableDesertExplosiveTower>("grabberTriggerTimer");
    private static readonly FieldInfo FuseActive = Field<EnemyBangFuse>("active");
    private static readonly FieldInfo FuseSpeed = Field<EnemyBangFuse>("standaloneFuseSpeed");
    private static readonly FieldInfo LighterOpen = Field<LighterValuable>("openProgress");
    private static readonly FieldInfo LighterTimer = Field<LighterValuable>("grabbedTimer");
    private static readonly FieldInfo LighterGrabbed = Field<LighterValuable>("grabbed");
    private static readonly FieldInfo PopcornRev = Field<ValuableDesertPopcornStand>("revValue");
    private static readonly FieldInfo PopcornSmooth = Field<ValuableDesertPopcornStand>("revSmooth");
    private static readonly FieldInfo PopcornInvestigate = Field<ValuableDesertPopcornStand>("investigateTimer");
    private static readonly MethodInfo CartSetState = Method<ValuableDesertGolfcart>("SetState");
    private static readonly MethodInfo CubeTalkCleanup = Method<ValuableCubeOfKnowledge>("TalkCleanup");
    private static readonly MethodInfo CubeZapCleanup = Method<ValuableCubeOfKnowledge>("ItemZapCleanup");
    private static readonly FieldInfo CubeRejectedItems = Field<ValuableCubeOfKnowledge>("rejectedItems");

    internal static bool Suppressed(Component component) =>
        StageRolesPlugin.Instance?.Controller?.ShouldSuppressEngineerEffect(component) == true;

    internal static bool BeforeFrame(Component component, bool suppress)
    {
        if (component is EnemyBangFuse fuse)
        {
            if (!fuse.standalone) return true; // Never change the enemy's own fuse.
            if (!suppress) { RestoreFuse(fuse); return true; }
            var dynamite = fuse.GetComponentInParent<ValuableDesertExplosiveTowerDynamite>();
            if (dynamite == null) return true;
            if (!PausedFuses.ContainsKey(fuse))
            {
                PausedFuses.Add(fuse, (float)FuseSpeed.GetValue(fuse)!);
                // This native RPC pauses the same countdown on vanilla guests.
                // Do not reset elapsed fuse progress or grant a new fuse on release.
                dynamite.StandaloneSetup(float.MaxValue);
            }
            FuseActive.SetValue(fuse, false);
            fuse.soundFuseLoop?.PlayLoop(false, 5f, 5f);
            if (fuse.particleFire != null) fuse.particleFire.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            if (fuse.particleSpark != null) fuse.particleSpark.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            if (fuse.tipObject != null) fuse.tipObject.SetActive(false);
            return false;
        }
        if (!suppress) return true;
        switch (component)
        {
            case SnakeInACanValuable snake:
                if ((bool)SnakeOut.GetValue(snake)!) SnakeIn.Invoke(snake, null);
                break;
            case ValuableDesertBirdCage bird:
                if ((ValuableDesertBirdCage.State)BirdState.GetValue(bird)! != ValuableDesertBirdCage.State.Idle)
                    BirdSetState.Invoke(bird, new object[] { ValuableDesertBirdCage.State.Idle });
                BirdChirp.SetValue(bird, -1f);
                BirdDeath.SetValue(bird, 0f);
                BirdRest.Invoke(bird, null);
                break;
            case ValuableDesertEnergyCrystalChunk energy:
                if ((ValuableDesertEnergyCrystalChunk.States)EnergyState.GetValue(energy)! != ValuableDesertEnergyCrystalChunk.States.Idle)
                    EnergySetState.Invoke(energy, new object[] { ValuableDesertEnergyCrystalChunk.States.Idle });
                // Cloud particles and short-lived player overrides expire normally;
                // charge/energize callbacks cannot extend them during suppression.
                break;
            case ValuableDesertExplosiveTower tower:
                var state = (ValuableDesertExplosiveTower.State)TowerState.GetValue(tower)!;
                if (state is ValuableDesertExplosiveTower.State.Trigger or ValuableDesertExplosiveTower.State.Explode)
                    TowerSetState.Invoke(tower, new object[] { ValuableDesertExplosiveTower.State.Reset });
                TowerGrabTimer.SetValue(tower, 0f);
                foreach (ParticleSystem particles in tower.GetComponentsInChildren<ParticleSystem>(true))
                    if (particles != null && particles.isPlaying) particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                break;
            case LighterValuable lighter:
                LighterOpen.SetValue(lighter, 0f);
                LighterTimer.SetValue(lighter, 0f);
                LighterGrabbed.SetValue(lighter, false);
                if (lighter.candleLight != null) lighter.candleLight.SetActive(false);
                if (lighter.lid != null) lighter.lid.localRotation = Quaternion.identity;
                return false;
            case ValuableDesertPopcornStand popcorn:
                PopcornRev.SetValue(popcorn, 0f);
                PopcornSmooth.SetValue(popcorn, 0f);
                PopcornInvestigate.SetValue(popcorn, 0f);
                break;
            case ValuableDesertGolfcart cart:
                if (cart.currentState != ValuableDesertGolfcart.State.Idle)
                    CartSetState.Invoke(cart, new object[] { ValuableDesertGolfcart.State.Idle });
                if (cart.hurtCollider != null) cart.hurtCollider.gameObject.SetActive(false);
                break;
            case ValuableCubeOfKnowledge cube:
                CubeTalkCleanup.Invoke(cube, null);
                CubeZapCleanup.Invoke(cube, null);
                ((IList)CubeRejectedItems.GetValue(cube)!).Clear();
                // Keep scale/value, activated sides and growth clocks intact.
                break;
        }
        return true;
    }

    internal static void RestHorse(ValuableHorseOnWheels horse)
    {
        if ((ValuableHorseOnWheels.State)HorseState.GetValue(horse)! == ValuableHorseOnWheels.State.MoveForward)
        {
            HorseSetState.Invoke(horse, new object[] { ValuableHorseOnWheels.State.Idle });
            HorseIdle.Invoke(horse, new object[] { false });
        }
        horse.soundMove?.PlayLoop(false, 0.8f, 0.8f);
    }

    private static void RestoreFuse(EnemyBangFuse fuse)
    {
        if (!PausedFuses.TryGetValue(fuse, out float speed)) return;
        if (fuse != null && SemiFunc.IsMasterClientOrSingleplayer())
            fuse.GetComponentInParent<ValuableDesertExplosiveTowerDynamite>()?.StandaloneSetup(speed);
        PausedFuses.Remove(fuse!);
    }

    internal static void Clear()
    {
        foreach (EnemyBangFuse fuse in new List<EnemyBangFuse>(PausedFuses.Keys)) RestoreFuse(fuse);
        PausedFuses.Clear();
        // Do not reset an executing finalizer's noise scope here.
    }

    private static FieldInfo Field<T>(string name) => AccessTools.Field(typeof(T), name) ?? throw new MissingFieldException(typeof(T).Name, name);
    private static MethodInfo Method<T>(string name) => AccessTools.Method(typeof(T), name) ?? throw new MissingMethodException(typeof(T).Name, name);
}
