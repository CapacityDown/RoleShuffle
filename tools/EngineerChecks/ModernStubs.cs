using UnityEngine;
#pragma warning disable CS0169, CS0414, CS0649
public class PlayerAvatar : Component { }
public class PhysGrabber : Component { public Photon.Pun.PhotonView? photonView = new(); }
public static class SemiFunc
{
    public static bool Authority = true, Multiplayer = true;
    public static bool IsMasterClientOrSingleplayer() => Authority;
    public static bool IsMultiplayer() => Multiplayer;
}
namespace Photon.Pun
{
    public enum RpcTarget { Others, All }
    public class PhotonView : Component
    {
        public int ViewID;
        public readonly List<(string Method, RpcTarget Target, int Id)> Calls = new();
        public void RPC(string name, RpcTarget target, params object[] args) => Calls.Add((name, target, (int)args[0]));
    }
}
public class EnemyDirector { public void SetInvestigate() { } }
public class LighterValuable : Component
{
    public GameObject? candleLight = new(); public Transform? lid = new();
    private float openProgress, grabbedTimer; private bool grabbed;
    public void Update() { } public void FixedUpdate() { }
}
public class SnakeInACanValuable : Component
{
    private bool snakeOut;
    public bool Hitbox = true, Retracted;
    private void SnakeIn() { snakeOut = false; Hitbox = false; Retracted = true; }
    public void Update() { }
    public void SnakeOut() { }
}
public class ValuableDesertBirdCage : Component
{
    public enum State { Idle, Dead }
    private State stateCurrent;
    private float chirpTimer, deathTimer;
    public int RestCalls;
    private void StateSet(State state) => stateCurrent = state;
    private void PoseReset() => RestCalls++;
    public void Update() { }
    public void StateIdle() { }
    public void StateDead() { }
    public void ChirpSet() { }
}
public class ValuableDesertEnergyCrystalChunk : Component
{
    public enum States { Idle, Active }
    private States currentState;
    public int StateChanges;
    private void SetState(States state) { currentState = state; StateChanges++; }
    public void Update() { }
    public void StateIdle() { }
    public void EnergizeLogic() { }
    public void BreakImpulseChargeCloud() { }
    public void ChargeItemsInRange() { }
}
public class ValuableDesertExplosiveTower : Component
{
    public enum State { Idle, Trigger, Reset, Explode, Destroy }
    private State stateCurrent;
    private float grabberTriggerTimer;
    public int StateChanges;
    private void StateSet(State state) { stateCurrent = state; StateChanges++; }
    public void Update() { }
    public void StateTrigger() { }
    public void StateExplode() { }
    public bool GrabberTriggerCheck() => true;
    public bool CollisionTriggerCheck() => true;
}
public class ValuableDesertExplosiveTowerDynamite : Component
{
    public readonly List<float> SentSpeeds = new();
    public void StandaloneSetup(float speed)
    {
        SentSpeeds.Add(speed);
        var fuse = GetComponent<EnemyBangFuse>()!;
        typeof(EnemyBangFuse).GetField("standaloneFuseSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.SetValue(fuse, speed);
    }
    public void Explode() { }
}
public class EnemyBangFuse : Component
{
    public bool standalone = true;
    private float standaloneFuseSpeed = 12, standaloneFuseLerp = .4f;
    private bool active = true;
    public Sound? soundFuseLoop = new(); public ParticleSystem? particleFire = new(), particleSpark = new(); public GameObject? tipObject = new();
    public void Update() { }
}
public class ValuableDesertGolfcart : Component
{
    public enum State { Idle, Active }
    public State currentState = State.Active;
    public HurtCollider? hurtCollider = new();
    public int StateChanges;
    private void SetState(State state) { currentState = state; StateChanges++; }
    private PlayerAvatar? Driver() => new();
    public void Update() { } public void FixedUpdate() { }
    public void FixedUpdateActive() { }
    public void NeighOnBreak() { }
}
public class ValuableDesertPopcornStand : Component
{
    private float revValue, revSmooth, investigateTimer;
    public void Update() { }
    public void RevLogic() { }
    public void InvestigateLogic() { }
}
public class ValuableDesertZeroGravCrystal : Component { public void Update() { }     public void AffectSpawnLogic() { }
    public void AffectRenewLogic() { }
    public void PlayerAffectStart() { }
    public void CrystalAffectStart() { }
    public void GrabberBeamsLogic() { }
}
public class ValuableMusicInstrument : Component { public void Update() { } public void FixedUpdate() { }     public void EnemySetInvestigate() { }
    public bool IsGrabbed() => true;
}
public class ValuableWizardCrystal : Component { public void Update() { }     public void Destroyed() { }
}
public class ValuableCubeOfKnowledge : Component
{
    private readonly List<PhysGrabObject> rejectedItems = new() { new() };
    public int TalkCleanups, ZapCleanups;
    public int ActivatedSides = 7, Value = 12345;
    private void TalkCleanup() => TalkCleanups++;
    private void ItemZapCleanup() => ZapCleanups++;
    public void Update() { } public void FixedUpdate() { }
    public void TalkInvestigateLogic() { }
    public void TalkPhysics() { }
    public void TalkAudioLogic() { }
    public void SoundLoopLogic() { }
    public void ItemRejectPhysics() { }
    public void ItemRejectExisting() { }
    public void ConnectionImpactApply() { }
    public void PlatformAlignmentFixedUpdate() { }
    public void AwakenedFloatFixedUpdate() { }
    public void ActivationHoldLogic() { }
    public void FinalBuildShakeLogic() { }
    public bool SideActivate() => true;
    public bool CanConnectGet() => true;
    public bool ItemReject() => true;
    public bool ItemRejectActive() => true;
}
