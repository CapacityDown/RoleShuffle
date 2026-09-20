using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Photon.Pun;
using UnityEngine;

namespace REPOJP.StageRoles;

internal sealed partial class StageRoleController : MonoBehaviour
{
    private static readonly FieldInfo? EnemyHealthEnemyField =
        AccessTools.Field(typeof(EnemyHealth), "enemy");
    private static readonly FieldInfo? EnemyParentField =
        AccessTools.Field(typeof(Enemy), "EnemyParent");
    private static readonly FieldInfo? EnemyDifficultyField =
        AccessTools.Field(typeof(EnemyParent), "difficulty");
    private static readonly FieldInfo? EnemySpawnValuableCurrentField =
        AccessTools.Field(typeof(EnemyHealth), "spawnValuableCurrent");
    private const float PlayerPresenceCheckIntervalSeconds = 0.5f;
    private const int PlayerMissingChecksBeforeRemoval = 10;
    private const float TunaMovementThresholdSquared = 0.000025f;
    private const float TunaStageStartGraceSeconds = 5f;
    private const float RoleQueryCooldownSeconds = 2f;
    private const float HunterKillCreditLifetimeSeconds = 10f;
    private const int RecentRoleHistoryLimit = 5;
    private static readonly StageRole[] SoloExcludedRoles =
    {
        StageRole.Tracker,
        StageRole.Ghost,
        StageRole.Medic,
        StageRole.Jobless,
        StageRole.Rescuer,
        StageRole.Influencer,
        StageRole.Werewolf,
        StageRole.Bodyguard,
        StageRole.Imitator,
        StageRole.Avenger,
        StageRole.Influenza,
        StageRole.Disaster
    };
    private readonly List<RoleAssignment> _assignments = new();
    private readonly Dictionary<string, RoleAssignment> _assignmentsBySteamId =
        new(StringComparer.Ordinal);
    private readonly Dictionary<PlayerAvatar, RoleAssignment> _assignmentsByPlayer = new();
    private readonly Dictionary<string, float> _rescueDeadSince = new(StringComparer.Ordinal);
    private readonly HashSet<string> _rescueRevivePending = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _rescueReviverByTarget =
        new(StringComparer.Ordinal);
    private readonly Dictionary<string, float> _roleQueryAllowedAt = new(StringComparer.Ordinal);
    private readonly Dictionary<string, float> _expressionInputAllowedAt = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _consecutiveMissingPlayerChecks =
        new(StringComparer.Ordinal);
    private readonly Dictionary<string, RoleAssignment> _departedAssignments =
        new(StringComparer.Ordinal);
    private readonly Dictionary<string, StageRole> _previousRoles =
        new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<StageRole>> _recentRolesByPlayer =
        new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _lastHardshipStages =
        new(StringComparer.Ordinal);
    private readonly Dictionary<int, EnemyAttackCredit> _enemyLastAttacker = new();
    private readonly Dictionary<int, HashSet<int>> _grabbersByObjectId = new();
    private readonly HashSet<int> _engineerSuppressedTrapIds = new();
    private readonly Dictionary<int, float> _engineerSuppressionGraceUntil = new();
    private StageRolesConfig _config = null!;
    private VanillaRolePrefabResolver _resolver = null!;
    private BomberRoleRuntime _bomber = null!;
    private MedicRoleRuntime _medic = null!;
    private MageRoleRuntime _mage = null!;
    private GamblerRoleRuntime _gambler = null!;
    private StinkerRoleRuntime _stinker = null!;
    private TricksterRoleRuntime _trickster = null!;
    private UtilityRoleRuntime _utilityRoles = null!;
    private EventRoleRuntime _eventRoles = null!;
    private DiverRoleRuntime _diver = null!;
    private RoleOverhaulRuntime _overhaul = null!;
    private RoleNotifier _notifier = null!;
    private bool _stageReady;
    private bool _assignmentsInitialized;
    private int _stageGeneration;
    private int _levelGeneratorInstanceId;
    private float _nextPlayerPresenceCheckAt;
    private float _phoenixFailureGraceUntil;
    private float _tunaStageGraceUntil;
    private int _completedAssignmentStages;
    private int _currentAssignmentStage;
    private string _previousCrownedSteamId = string.Empty;
    private bool _kingCrownApplied;
    private float _nextExhaustionCheckAt;
    private static readonly (StageRole Ability, string Message)[] ExhaustionMessages =
    {
        (StageRole.Medic, "MedicEmpty"),
        (StageRole.Mage, "MageHealEmpty"),
        (StageRole.Mechanic, "RepairEmpty"),
        (StageRole.Electrician, "ChargeEmpty")
    };

    private readonly struct EnemyAttackCredit
    {
        internal EnemyAttackCredit(string steamId, float recordedAt)
        {
            SteamId = steamId;
            RecordedAt = recordedAt;
        }

        internal string SteamId { get; }
        internal float RecordedAt { get; }
    }

    internal void Initialize(StageRolesConfig config)
    {
        _config = config;
        _resolver = new VanillaRolePrefabResolver();
        _bomber = new BomberRoleRuntime(this, config, _resolver);
        _medic = new MedicRoleRuntime(config);
        _mage = new MageRoleRuntime(this, config, _resolver);
        _gambler = new GamblerRoleRuntime(config);
        _stinker = new StinkerRoleRuntime(this, config, _resolver);
        _trickster = new TricksterRoleRuntime(this, config, _resolver);
        _utilityRoles = new UtilityRoleRuntime(config, gameObject);
        _eventRoles = new EventRoleRuntime(this, config);
        _diver = new DiverRoleRuntime(config);
        _overhaul = new RoleOverhaulRuntime(config);
        _notifier = new RoleNotifier(this, config);
        gameObject.SetActive(false);
    }

    internal void StageReady(LevelGenerator generator)
    {
        if (!_config.Enabled.Value || !IsAuthority() || !IsPlayableStageContext())
        {
            StageEndingInternal(
                restoreBaseUpgrades: true,
                deactivate: true);
            return;
        }

        int instanceId = generator != null ? generator.GetInstanceID() : 0;
        if (_stageReady && _levelGeneratorInstanceId == instanceId)
        {
            return;
        }

        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }
        StageEndingInternal(restoreBaseUpgrades: true, deactivate: false);
        _stageReady = true;
        _assignmentsInitialized = false;
        _tunaStageGraceUntil = Time.time + TunaStageStartGraceSeconds;
        _levelGeneratorInstanceId = instanceId;
        int generation = ++_stageGeneration;
        StartCoroutine(PrepareStage(generation));
    }

    internal void StageEnding()
    {
        StageEndingInternal(restoreBaseUpgrades: true, deactivate: true);
    }

    internal void ExtractionCompleted(RoundDirector? director)
    {
        if (!_stageReady || !IsAuthority())
        {
            return;
        }
        _gambler.ExtractionCompleted(director, _assignments, _notifier);
    }

    internal void ObserveGambitState(EnemySpinny gambit)
    {
        if (_stageReady && IsAuthority())
        {
            _gambler.ObserveGambitState(gambit);
        }
    }

    internal void CaptureGambitTarget(EnemySpinny gambit)
    {
        if (_stageReady && IsAuthority())
        {
            _gambler.CaptureGambitTarget(gambit);
        }
    }

    internal void DiverTumbleStarted(PlayerTumble tumble)
    {
        if (_stageReady && _assignmentsInitialized && IsAuthority())
        {
            _diver.PlayerTumbleStarted(tumble);
        }
    }

    internal bool ShouldKeepDiverTumbling(PlayerTumble tumble) =>
        _stageReady && _assignmentsInitialized && IsAuthority() &&
        _diver.ShouldKeepTumbling(tumble);

    internal GamblerRoleRuntime.GambitEffectContext? BeginGambitEffect(
        EnemySpinny gambit) =>
        _stageReady && IsAuthority()
            ? _gambler.BeginGambitEffect(gambit, _assignments)
            : null;

    internal void CompleteGambitEffect(
        GamblerRoleRuntime.GambitEffectContext context)
    {
        if (_stageReady && IsAuthority())
        {
            _gambler.CompleteGambitEffect(context);
        }
    }

    internal bool AllowBomberGrenadeGrabStart(
        PhysGrabObject grenade,
        int playerViewId,
        PhotonMessageInfo messageInfo) =>
        !_stageReady || !IsAuthority() ||
        _bomber.AllowGrabStart(
            grenade,
            playerViewId,
            messageInfo,
            IsAssignedBomber(playerViewId));

    internal bool AllowBomberGrenadeGrabEnd(
        PhysGrabObject grenade,
        int playerViewId,
        PhotonMessageInfo messageInfo) =>
        !_stageReady || !IsAuthority() ||
        _bomber.AllowGrabEnd(grenade, playerViewId, messageInfo);

    internal bool IsTricksterDecoy(PhysGrabObject physObject) =>
        _stageReady && IsAuthority() && _trickster.IsDecoy(physObject);

    internal bool IsTricksterDecoy(Component component)
    {
        if (!_stageReady || !IsAuthority() || component == null)
        {
            return false;
        }
        PhysGrabObject? physObject =
            component.GetComponent<PhysGrabObject>() ??
            component.GetComponentInParent<PhysGrabObject>() ??
            component.GetComponentInChildren<PhysGrabObject>(true);
        return physObject != null && _trickster.IsDecoy(physObject);
    }

    internal bool MaintainTricksterDecoyActiveState(ScreamDollValuable screamDoll) =>
        _stageReady && IsAuthority() && _trickster.MaintainActiveState(screamDoll);

    internal bool ShouldSuppressEngineerTrap(Trap trap) =>
        ShouldSuppressEngineerEffect(trap);

    internal bool ShouldSuppressEngineerEffect(Component effect)
    {
        if (!_stageReady || !IsAuthority() || effect == null)
        {
            return false;
        }

        try
        {
            PhysGrabObject? valuable =
                effect.GetComponent<PhysGrabObject>() ??
                effect.GetComponentInParent<PhysGrabObject>() ??
                effect.GetComponentInChildren<PhysGrabObject>(true);
            if (valuable == null)
            {
                return false;
            }
            if (!EngineerEffectCatalog.IsEffectValuable(valuable))
            {
                return false;
            }
            int valuableId = valuable.GetInstanceID();
            if (_engineerSuppressionGraceUntil.TryGetValue(
                    valuableId,
                    out float graceUntil))
            {
                if (Time.time < graceUntil)
                {
                    ResetEngineerEffectState(effect);
                    return true;
                }
                _engineerSuppressionGraceUntil.Remove(valuableId);
            }
            foreach (PhysGrabber grabber in valuable.playerGrabbing)
            {
                if (grabber?.playerAvatar != null &&
                    PlayerHasRole(grabber.playerAvatar, StageRole.Engineer))
                {
                    ResetEngineerEffectState(effect);
                    LogEngineerSuppression(valuable);
                    return true;
                }
            }
            if (_grabbersByObjectId.TryGetValue(
                    valuable.GetInstanceID(),
                    out HashSet<int> grabberViewIds))
            {
                foreach (int grabberViewId in grabberViewIds)
                {
                    PhotonView? grabberView = PhotonView.Find(grabberViewId);
                    PlayerAvatar? player = grabberView != null
                        ? grabberView.GetComponent<PlayerAvatar>() ??
                          grabberView.GetComponent<PhysGrabber>()?.playerAvatar
                        : null;
                    if (PlayerHasRole(player, StageRole.Engineer))
                    {
                        ResetEngineerEffectState(effect);
                        LogEngineerSuppression(valuable);
                        return true;
                    }
                }
            }
            _engineerSuppressedTrapIds.Remove(valuable.GetInstanceID());
        }
        catch (Exception exception)
        {
            StageRolesPlugin.ModLogger.LogDebug(
                $"Engineer trap ownership could not be resolved: {exception.Message}");
        }
        return false;
    }

    internal bool TryRegisterEngineerHostOnlyGrab(
        PhysGrabObject grabbedObject,
        int playerViewId,
        PhotonMessageInfo messageInfo)
    {
        if (!_stageReady || !IsAuthority() || grabbedObject == null ||
            playerViewId <= 0 ||
            !EngineerEffectCatalog.IsEffectValuable(grabbedObject))
        {
            return false;
        }

        try
        {
            PhotonView? playerView = PhotonView.Find(playerViewId);
            PhysGrabber? grabber = playerView != null
                ? playerView.GetComponent<PhysGrabber>()
                : null;
            PhotonView? grabberPhotonView = grabber != null
                ? grabber.GetComponent<PhotonView>()
                : null;
            if (grabber?.playerAvatar == null ||
                grabberPhotonView == null ||
                !PlayerHasRole(grabber.playerAvatar, StageRole.Engineer) ||
                !SemiFunc.OwnerOnlyRPC(messageInfo, grabberPhotonView))
            {
                return false;
            }

            if (!grabbedObject.playerGrabbing.Contains(grabber))
            {
                grabbedObject.playerGrabbing.Add(grabber);
            }
            RecordGrabStarted(grabbedObject, playerViewId);
            _engineerSuppressionGraceUntil.Remove(
                grabbedObject.GetInstanceID());
            StageRolesPlugin.ModLogger.LogDebug(
                "Engineer effect valuable grab registered on the host without broadcasting the vanilla activation condition.");
            return true;
        }
        catch (Exception exception)
        {
            StageRolesPlugin.ModLogger.LogDebug(
                $"Engineer host-only grab registration fell back to vanilla: {exception.Message}");
            return false;
        }
    }

    internal void RecordGrabStarted(
        PhysGrabObject grabbedObject,
        int playerViewId)
    {
        if (!_stageReady || !IsAuthority() || grabbedObject == null ||
            playerViewId <= 0)
        {
            return;
        }

        int objectId = grabbedObject.GetInstanceID();
        if (!_grabbersByObjectId.TryGetValue(
                objectId,
                out HashSet<int> grabbers))
        {
            grabbers = new HashSet<int>();
            _grabbersByObjectId[objectId] = grabbers;
        }
        grabbers.Add(playerViewId);
    }

    internal void RecordGrabEnded(
        PhysGrabObject grabbedObject,
        int playerViewId)
    {
        if (grabbedObject == null)
        {
            return;
        }

        int objectId = grabbedObject.GetInstanceID();
        if (_stageReady && IsAuthority())
            foreach (RoleAssignment assignment in _assignments)
                if (assignment.Player != null && assignment.Player.photonView != null &&
                    assignment.Player.photonView.ViewID == playerViewId && assignment.Overhaul.CargoId == objectId)
                {
                    _overhaul.CargoReleased(assignment, grabbedObject, _notifier);
                    assignment.Overhaul.ResetCargo();
                }
        if (_engineerSuppressedTrapIds.Contains(objectId) ||
            (EngineerEffectCatalog.IsEffectValuable(grabbedObject) &&
             IsAssignedRole(playerViewId, StageRole.Engineer)))
        {
            _engineerSuppressionGraceUntil[objectId] = Time.time + 0.25f;
        }
        if (_grabbersByObjectId.TryGetValue(objectId, out HashSet<int> grabbers))
        {
            grabbers.Remove(playerViewId);
            if (grabbers.Count == 0)
            {
                _grabbersByObjectId.Remove(objectId);
                _engineerSuppressedTrapIds.Remove(objectId);
            }
        }
    }

    private static void ResetEngineerEffectState(Component effect)
    {
        if (effect is not Trap trap)
        {
            return;
        }

        trap.trapStart = false;
        trap.trapActive = false;
        trap.trapTriggered = false;
        trap.enemyInvestigate = false;
    }

    private void LogEngineerSuppression(PhysGrabObject valuable)
    {
        if (_engineerSuppressedTrapIds.Add(valuable.GetInstanceID()))
        {
            StageRolesPlugin.ModLogger.LogDebug(
                "Engineer prevented a held valuable effect from updating or activating.");
        }
    }

    private bool IsAssignedBomber(int playerViewId) =>
        IsAssignedRole(playerViewId, StageRole.Bomber);

    private bool IsAssignedRole(int playerViewId, StageRole role)
    {
        try
        {
            PhotonView? playerView = PhotonView.Find(playerViewId);
            PlayerAvatar? player = playerView != null
                ? playerView.GetComponent<PlayerAvatar>() ??
                  playerView.GetComponent<PhysGrabber>()?.playerAvatar
                : null;
            if (player == null)
            {
                return false;
            }
            string steamId = PlayerIdentity.SteamId(player);
            foreach (RoleAssignment assignment in _assignments)
            {
                if (RoleCatalog.HasCapability(assignment.Role, role) &&
                    (ReferenceEquals(assignment.Player, player) ||
                     (!string.IsNullOrEmpty(steamId) &&
                      string.Equals(
                          assignment.SteamId,
                          steamId,
                          StringComparison.Ordinal))))
                {
                    return true;
                }
            }
        }
        catch (Exception exception)
        {
            StageRolesPlugin.ModLogger.LogDebug(
                $"Grabber role could not be resolved: {exception.Message}");
        }
        return false;
    }

    internal bool TryPreventPhoenixRetake()
    {
        if (!_stageReady || !IsAuthority())
        {
            return false;
        }

        bool pending = false;
        foreach (RoleAssignment assignment in _assignments)
        {
            if (!RoleCatalog.HasCapability(
                    assignment.Role,
                    StageRole.Phoenix) ||
                assignment.PhoenixUsed ||
                assignment.Player == null)
            {
                continue;
            }

            bool alive = PlayerState.IsLiving(assignment.Player);
            if (alive)
            {
                CompletePhoenixRevival(assignment);
                assignment.WasAlive = true;
                assignment.PhoenixReadyAt = 0f;
                continue;
            }
            if (assignment.PhoenixRevivePending)
            {
                pending = true;
                continue;
            }
            if (!assignment.WasAlive && assignment.PhoenixReadyAt <= 0f)
            {
                continue;
            }

            pending = true;
            SchedulePhoenix(assignment);
            TryRevivePhoenix(assignment);
        }

        if (!pending)
        {
            _phoenixFailureGraceUntil = 0f;
            return false;
        }
        if (_phoenixFailureGraceUntil <= 0f)
        {
            _phoenixFailureGraceUntil = Time.time +
                Mathf.Clamp(_config.PhoenixFailureGraceSeconds.Value, 1f, 15f);
        }

        bool revived = false;
        foreach (RoleAssignment assignment in _assignments)
        {
            if (RoleCatalog.HasCapability(
                    assignment.Role,
                    StageRole.Phoenix) &&
                assignment.PhoenixUsed &&
                PlayerState.IsLiving(assignment.Player))
            {
                revived = true;
            }
        }
        if (revived)
        {
            _phoenixFailureGraceUntil = 0f;
            return true;
        }
        return Time.time < _phoenixFailureGraceUntil;
    }

    internal void Shutdown()
    {
        StageEndingInternal(
            restoreBaseUpgrades: true,
            deactivate: true);
    }

    internal bool TrySetRole(
        string targetIdentifier,
        StageRole role,
        out string response)
    {
        if (!CanChangeRoles(out response))
        {
            return false;
        }

        RoleAssignment? assignment = ResolveAssignment(targetIdentifier);
        if (assignment == null)
        {
            response = string.IsNullOrWhiteSpace(targetIdentifier)
                ? "The local player has no stage role."
                : $"Player not found: {targetIdentifier}";
            return false;
        }

        StageRole previousRole = assignment.AssignedRole;
        Dictionary<string, StageRole> changes = new(StringComparer.Ordinal)
        {
            [assignment.SteamId] = role
        };
        ApplyRoleChanges(changes);

        string playerName = PlayerIdentity.Name(assignment.Player);
        if (string.IsNullOrEmpty(playerName))
        {
            playerName = assignment.SteamId;
        }
        response = $"{playerName}: {RoleCatalog.AssignmentName(previousRole)} -> " +
                   RoleCatalog.AssignmentName(role);
        StageRolesPlugin.ModLogger.LogInfo($"Test role changed: {response}.");
        return true;
    }

    internal bool TrySetRoleForAll(StageRole role, out string response)
    {
        if (!CanChangeRoles(out response))
        {
            return false;
        }
        if (_assignments.Count == 0)
        {
            response = "No players currently have a stage role.";
            return false;
        }

        Dictionary<string, StageRole> changes = new(StringComparer.Ordinal);
        foreach (RoleAssignment assignment in _assignments)
        {
            changes[assignment.SteamId] = role;
        }
        ApplyRoleChanges(changes);
        response = $"Assigned {RoleCatalog.AssignmentName(role)} to all " +
                   $"{_assignments.Count} players.";
        StageRolesPlugin.ModLogger.LogInfo($"Test roles changed: {response}");
        return true;
    }

    internal bool TryRandomizeRole(
        string targetIdentifier,
        out string response)
    {
        if (!CanChangeRoles(out response))
        {
            return false;
        }

        RoleAssignment? assignment = ResolveAssignment(targetIdentifier);
        if (assignment == null)
        {
            response = string.IsNullOrWhiteSpace(targetIdentifier)
                ? "The local player has no stage role."
                : $"Player not found: {targetIdentifier}";
            return false;
        }

        List<StageRole> enabledRoles = EnabledRolesForCurrentParty();
        if (enabledRoles.Count == 0)
        {
            response = "No enabled roles with a positive weight are available.";
            return false;
        }

        BuildRerollHistory(
            out Dictionary<string, StageRole> previousRoles,
            out Dictionary<string, List<StageRole>> recentRolesByPlayer,
            out Dictionary<string, int> lastHardshipStages,
            out int rerollStage);
        List<StageRole> reservedRoles = new();
        foreach (RoleAssignment current in _assignments)
        {
            if (!ReferenceEquals(current, assignment))
            {
            reservedRoles.Add(current.AssignedRole);
            }
        }

        RoleAssignmentPlanner planner = new(
            _config,
            previousRoles,
            recentRolesByPlayer,
            lastHardshipStages,
            rerollStage);
        StageRole? plannedRole = planner.PlanJoinedAssignment(
            assignment.SteamId,
            enabledRoles,
            reservedRoles,
            _assignments.Count);
        if (plannedRole == null)
        {
            response = "No role could be selected with the current settings.";
            return false;
        }

        StageRole previousRole = assignment.AssignedRole;
        Dictionary<string, StageRole> changes = new(StringComparer.Ordinal)
        {
            [assignment.SteamId] = plannedRole.Value
        };
        ApplyRoleChanges(changes);

        string playerName = PlayerIdentity.Name(assignment.Player);
        if (string.IsNullOrEmpty(playerName))
        {
            playerName = assignment.SteamId;
        }
        response = $"{playerName}: {RoleCatalog.AssignmentName(previousRole)} -> " +
                   RoleCatalog.AssignmentName(plannedRole.Value);
        StageRolesPlugin.ModLogger.LogInfo($"Test role randomized: {response}.");
        return true;
    }

    internal bool TryRandomizeAllRoles(out string response)
    {
        if (!CanChangeRoles(out response))
        {
            return false;
        }
        if (_assignments.Count == 0)
        {
            response = "No players currently have a stage role.";
            return false;
        }

        List<StageRole> enabledRoles = EnabledRolesForCurrentParty();
        if (enabledRoles.Count == 0)
        {
            response = "No enabled roles with a positive weight are available.";
            return false;
        }

        BuildRerollHistory(
            out Dictionary<string, StageRole> previousRoles,
            out Dictionary<string, List<StageRole>> recentRolesByPlayer,
            out Dictionary<string, int> lastHardshipStages,
            out int rerollStage);
        List<PlayerAvatar> players = new(_assignments.Count);
        foreach (RoleAssignment assignment in _assignments)
        {
            players.Add(assignment.Player);
        }

        RoleAssignmentPlanner planner = new(
            _config,
            previousRoles,
            recentRolesByPlayer,
            lastHardshipStages,
            rerollStage);
        Dictionary<string, StageRole> plannedRoles =
            planner.PlanInitialAssignments(players, enabledRoles);
        if (plannedRoles.Count != _assignments.Count)
        {
            response = "Roles could not be selected for every player with the current settings.";
            return false;
        }

        ApplyRoleChanges(plannedRoles);
        response = $"Randomized roles for all {_assignments.Count} players.";
        StageRolesPlugin.ModLogger.LogInfo($"Test roles randomized: {response}");
        return true;
    }

    private bool CanChangeRoles(out string response)
    {
        response = string.Empty;
        if (!_config.SetRoleCommandEnabled.Value)
        {
            response = "The set-role command is disabled.";
            return false;
        }
        if (!_stageReady || !IsAuthority())
        {
            response = "Roles can only be changed by the host during a stage.";
            return false;
        }
        return true;
    }

    private List<StageRole> EnabledRolesForCurrentParty()
    {
        List<StageRole> enabledRoles = EnabledRoles();
        if (!SemiFunc.IsMultiplayer() || _assignments.Count <= 1)
        {
            RemoveSoloIncompatibleRoles(enabledRoles);
        }
        RemoveUnavailableContextRoles(enabledRoles);
        RemoveOrphanedSuperbot(enabledRoles);
        return enabledRoles;
    }

    private void BuildRerollHistory(
        out Dictionary<string, StageRole> previousRoles,
        out Dictionary<string, List<StageRole>> recentRolesByPlayer,
        out Dictionary<string, int> lastHardshipStages,
        out int rerollStage)
    {
        previousRoles = new Dictionary<string, StageRole>(
            _previousRoles,
            StringComparer.Ordinal);
        recentRolesByPlayer = CopyRecentRoleHistory();
        lastHardshipStages = new Dictionary<string, int>(
            _lastHardshipStages,
            StringComparer.Ordinal);
        int currentStage = Math.Max(
            1,
            _currentAssignmentStage > 0
                ? _currentAssignmentStage
                : _completedAssignmentStages + 1);
        rerollStage = currentStage + 1;
        foreach (RoleAssignment assignment in _assignments)
        {
            previousRoles[assignment.SteamId] = assignment.AssignedRole;
            RecordRecentRole(
                recentRolesByPlayer,
                assignment.SteamId,
                assignment.AssignedRole);
            if (assignment.AssignedRole == StageRole.Jobless ||
                assignment.AssignedRole == StageRole.Tuna ||
                assignment.AssignedRole == StageRole.Influenza ||
                assignment.AssignedRole == StageRole.Disaster)
            {
                lastHardshipStages[assignment.SteamId] = currentStage;
            }
        }
    }

    private void ApplyRoleChanges(
        IReadOnlyDictionary<string, StageRole> changes)
    {
        _notifier.ResetPending();
        RoleHealingRuntime.Clear();
        RestoreKingCrown();
        _bomber.Stop();
        _mage.Stop();
        _stinker.Stop();
        _trickster.Stop();
        _utilityRoles.Stop();
        _eventRoles.Stop();
        _diver.Stop();
        HunterBatteryRuntime.Clear();

        HashSet<string> changedSteamIds = new(StringComparer.Ordinal);
        foreach (RoleAssignment assignment in _assignments)
        {
            if (!changes.TryGetValue(assignment.SteamId, out StageRole role))
            {
                continue;
            }

            StageRole previousRole = assignment.AssignedRole;
            EndInfluenza(assignment);
            assignment.AssignedRole = role;
            assignment.Role = role;
            ResetAssignmentForRoleChange(assignment);
            changedSteamIds.Add(assignment.SteamId);
            StageRolesPlugin.ModLogger.LogInfo(
                $"Test role changed for {assignment.SteamId}: " +
                $"{previousRole} -> {role}.");
        }

        foreach (RoleAssignment assignment in _assignments)
        {
            if (changedSteamIds.Contains(assignment.SteamId))
            {
                UpgradeService.SetLevels(
                    assignment.SteamId,
                    RoleCatalog.TargetUpgrades(assignment.Role, _config));
            }
        }

        foreach (RoleAssignment assignment in _assignments)
        {
            if (RoleCatalog.HasCapability(assignment.Role, StageRole.Bomber) ||
                RoleCatalog.HasCapability(assignment.Role, StageRole.Stinker))
            {
                Vector3 position = assignment.Player.transform.position;
                assignment.PreviousPosition = position;
                assignment.StinkerPreviousPosition = position;
                assignment.TunaPreviousPosition = position;
                assignment.StinkerTravelDistance = 0f;
                assignment.StinkerTrailAnchor = position;
                assignment.TravelDistance = 0f;
            }
        }

        _bomber.Begin();
        _medic.Begin(_assignments);
        _mage.Begin(_assignments);
        _gambler.Begin();
        _stinker.Begin(_assignments);
        _trickster.Begin();
        _utilityRoles.Begin(_assignments);
        _eventRoles.Begin(_assignments);
        _diver.Begin(_assignments);
        ApplyKingCrown();
        RoleAssignmentSync.Publish(_assignments);
    }

    private void ResetAssignmentForRoleChange(RoleAssignment assignment)
    {
        assignment.ExhaustionNotifications.Rearm();
        assignment.Overhaul.ResetCargo();
        Vector3 position = assignment.Player.transform.position;
        assignment.PreviousPosition = position;
        assignment.StinkerPreviousPosition = position;
        assignment.TunaPreviousPosition = position;
        assignment.StinkerTravelDistance = 0f;
        assignment.StinkerTrailAnchor = position;
        assignment.TravelDistance = 0f;
        assignment.JoblessDamageTimer = 0f;
        assignment.TunaStationaryTimer = 0f;
        assignment.TunaDamageTimer = 0f;
        assignment.MageNextCastAt = 0f;
        assignment.WasAlive = PlayerState.IsLiving(assignment.Player);
        assignment.PhoenixUsed = false;
        assignment.PhoenixRevivePending = false;
        assignment.PhoenixReadyAt = 0f;
        assignment.RescuerRevivesUsed = 0;
        assignment.GamblerWagersUsed = 0;
        assignment.MechanicRepairPercentUsed = 0f;
        assignment.ElectricianChargePercentUsed = 0f;
        assignment.InfluencerNextTtsAt = 0f;
        assignment.AvengerEmpoweredUntil = 0f;
        _rescueDeadSince.Remove(assignment.SteamId);
        _rescueRevivePending.Remove(assignment.SteamId);
        _rescueReviverByTarget.Remove(assignment.SteamId);
    }

    internal bool IsRoleQueryRequest(string message) =>
        _config.Enabled.Value && IsAuthority() &&
        string.Equals(message?.Trim(), "/roles", StringComparison.OrdinalIgnoreCase);

    internal void TryHandleMageCast(PlayerAvatar requester, string message)
    {
        if (requester == null || !_stageReady || !_config.Enabled.Value ||
            !IsAuthority())
        {
            return;
        }

        string spell = message?.Trim().ToLowerInvariant() ?? string.Empty;
        if (spell != "star" && spell != "roll" && spell != "gravity" &&
            spell != "void" && spell != "laser")
        {
            return;
        }

        string requesterId = PlayerIdentity.SteamId(requester);
        foreach (RoleAssignment assignment in _assignments)
        {
            if (!RoleCatalog.HasCapability(
                    assignment.Role,
                    StageRole.Mage) ||
                (!ReferenceEquals(assignment.Player, requester) &&
                 (string.IsNullOrEmpty(requesterId) ||
                  !string.Equals(
                      assignment.SteamId,
                      requesterId,
                      StringComparison.Ordinal))))
            {
                continue;
            }
            _mage.TryCast(assignment, spell);
            return;
        }
    }

    internal void TryHandleTricksterDecoy(PlayerAvatar requester, string message)
    {
        if (requester == null || !_stageReady || !_config.Enabled.Value ||
            !IsAuthority() ||
            !string.Equals(message?.Trim(), "decoy", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        string requesterId = PlayerIdentity.SteamId(requester);
        foreach (RoleAssignment assignment in _assignments)
        {
            if (!RoleCatalog.HasCapability(
                    assignment.Role,
                    StageRole.Trickster) ||
                (!ReferenceEquals(assignment.Player, requester) &&
                 (string.IsNullOrEmpty(requesterId) ||
                  !string.Equals(
                      assignment.SteamId,
                      requesterId,
                      StringComparison.Ordinal))))
            {
                continue;
            }
            _trickster.TryPlace(assignment);
            return;
        }
    }

    internal void TryHandleRoleExpression(
        PlayerAvatar requester,
        int expressionIndex)
    {
        if (requester == null || !_stageReady || !_config.Enabled.Value ||
            !IsAuthority())
        {
            return;
        }

        string actualName = ExpressionNameForIndex(expressionIndex);
        if (actualName.Length == 0)
        {
            return;
        }

        string requesterId = PlayerIdentity.SteamId(requester);
        string inputKey = $"{requesterId}:{expressionIndex}";
        if (_expressionInputAllowedAt.TryGetValue(inputKey, out float allowedAt) &&
            Time.time < allowedAt)
        {
            return;
        }

        foreach (RoleAssignment assignment in _assignments)
        {
            if (!ReferenceEquals(assignment.Player, requester) &&
                (string.IsNullOrEmpty(requesterId) ||
                 !string.Equals(
                     assignment.SteamId,
                     requesterId,
                     StringComparison.Ordinal)))
            {
                continue;
            }

            if (RoleCatalog.HasCapability(assignment.Role, StageRole.Mage))
            {
                string spell = MageSpellForExpression(actualName);
                if (!string.IsNullOrEmpty(spell))
                {
                    _expressionInputAllowedAt[inputKey] = Time.time + 0.25f;
                    _mage.TryCast(assignment, spell);
                    return;
                }
            }

            if (RoleCatalog.HasCapability(assignment.Role, StageRole.Trickster) &&
                ExpressionNamesMatch(actualName, _config.TricksterExpression.Value))
            {
                _expressionInputAllowedAt[inputKey] = Time.time + 0.25f;
                _trickster.TryPlace(assignment);
            }
            return;
        }
    }

    private static string ExpressionNameForIndex(int expressionIndex) =>
        expressionIndex switch
        {
            1 => "Angry",
            2 => "Sad",
            3 => "Suspicious",
            4 => "EyesClosed",
            5 => "Scared",
            6 => "Happy",
            _ => string.Empty
        };

    private string MageSpellForExpression(string actualName)
    {
        if (ExpressionNamesMatch(actualName, _config.MageStarExpression.Value))
        {
            return "star";
        }
        if (ExpressionNamesMatch(actualName, _config.MageRollExpression.Value))
        {
            return "roll";
        }
        if (ExpressionNamesMatch(actualName, _config.MageGravityExpression.Value))
        {
            return "gravity";
        }
        if (ExpressionNamesMatch(actualName, _config.MageVoidExpression.Value))
        {
            return "void";
        }
        return ExpressionNamesMatch(actualName, _config.MageLaserExpression.Value)
            ? "laser"
            : string.Empty;
    }

    private static bool ExpressionNamesMatch(
        string actualName,
        string configuredName)
    {
        string actual = NormalizeExpressionName(actualName);
        string configured = NormalizeExpressionName(configuredName);
        return configured.Length > 0 &&
               string.Equals(actual, configured, StringComparison.Ordinal);
    }

    private static string NormalizeExpressionName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        string normalized = value.Trim().ToLowerInvariant();
        if (normalized.StartsWith("expression", StringComparison.Ordinal))
        {
            normalized = normalized.Substring("expression".Length);
        }

        char[] buffer = new char[normalized.Length];
        int length = 0;
        foreach (char character in normalized)
        {
            if (char.IsLetterOrDigit(character))
            {
                buffer[length++] = character;
            }
        }
        return new string(buffer, 0, length);
    }

    internal void RespondToRoleQuery(PlayerAvatar requester)
    {
        if (requester == null || !_config.Enabled.Value || !IsAuthority())
        {
            return;
        }

        string requesterId = PlayerIdentity.SteamId(requester);
        if (string.IsNullOrEmpty(requesterId))
        {
            requesterId = requester.photonView != null
                ? $"view:{requester.photonView.ViewID}"
                : $"object:{requester.GetInstanceID()}";
        }
        if (_roleQueryAllowedAt.TryGetValue(requesterId, out float allowedAt) &&
            Time.time < allowedAt)
        {
            return;
        }
        _roleQueryAllowedAt[requesterId] = Time.time + RoleQueryCooldownSeconds;

        RoleAssignment? requesterAssignment = null;
        if (_stageReady)
        {
            foreach (RoleAssignment assignment in _assignments)
            {
                if (ReferenceEquals(assignment.Player, requester) ||
                    string.Equals(
                        assignment.SteamId,
                        requesterId,
                        StringComparison.Ordinal))
                {
                    requesterAssignment = assignment;
                    break;
                }
            }
        }

        string response = requesterAssignment != null
            ? $"YourRole:{RoleCatalog.AssignmentName(requesterAssignment.AssignedRole, requesterAssignment.Role)}"
            : "YourRole:Unavailable";
        _notifier.NotifyResponse(requester, response);
        StageRolesPlugin.ModLogger.LogInfo(
            $"Queued a role query response for player {requesterId}.");
    }

    private IEnumerator PrepareStage(int generation)
    {
        yield return new WaitForSeconds(0.75f);
        float deadline = Time.time + 10f;
        while (generation == _stageGeneration &&
               (!UpgradeService.Ready || GameDirector.instance == null) &&
               Time.time < deadline)
        {
            yield return new WaitForSeconds(0.25f);
        }

        if (generation != _stageGeneration || !_stageReady || !IsAuthority() ||
            !IsPlayableStageContext())
        {
            yield break;
        }

        List<PlayerAvatar> players = CollectPlayers();
        while (generation == _stageGeneration &&
               (players.Count == 0 || !AllPlayerIdentitiesReady(players)) &&
               Time.time < deadline)
        {
            yield return new WaitForSeconds(0.25f);
            players = CollectPlayers();
        }
        if (players.Count == 0)
        {
            StageRolesPlugin.ModLogger.LogWarning(
                "No players were available for stage role assignment.");
            CompleteStagePreparation();
            yield break;
        }

        IReadOnlyList<UpgradeGrant> baseUpgrades = RoleCatalog.BaseUpgrades(_config);
        List<StageRole> enabledRoles = EnabledRoles();
        if (!SemiFunc.IsMultiplayer() || players.Count <= 1)
        {
            RemoveSoloIncompatibleRoles(enabledRoles);
            StageRolesPlugin.ModLogger.LogDebug(
                "Tracker, Ghost, Medic, Jobless, Rescuer, Influencer, Werewolf, " +
                "Bodyguard, Imitator, and Avenger were excluded " +
                "from the one-player assignment pool.");
        }
        RemoveUnavailableContextRoles(enabledRoles);
        RemoveOrphanedSuperbot(enabledRoles);
        if (enabledRoles.Count == 0)
        {
            StageRolesPlugin.ModLogger.LogWarning(
                "No roles are enabled with a positive assignment weight.");
            foreach (PlayerAvatar player in players)
            {
                string steamId = PlayerIdentity.SteamId(player);
                if (!string.IsNullOrEmpty(steamId))
                {
                    UpgradeService.SetLevels(steamId, baseUpgrades);
                }
            }
            CompleteStagePreparation();
            yield break;
        }

        _currentAssignmentStage = _completedAssignmentStages + 1;
        RoleAssignmentPlanner planner = new(
            _config,
            _previousRoles,
            _recentRolesByPlayer,
            _lastHardshipStages,
            _currentAssignmentStage);
        Dictionary<string, StageRole> plannedRoles =
            planner.PlanInitialAssignments(players, enabledRoles);
        ClearAssignments();
        foreach (PlayerAvatar player in players)
        {
            string steamId = PlayerIdentity.SteamId(player);
            if (string.IsNullOrEmpty(steamId))
            {
                continue;
            }
            if (!plannedRoles.TryGetValue(steamId, out StageRole role))
            {
                StageRolesPlugin.ModLogger.LogWarning(
                    $"No role could be planned for player {steamId}.");
                UpgradeService.SetLevels(steamId, baseUpgrades);
                continue;
            }
            RoleAssignment assignment = new(steamId, player, role);
            RegisterAssignment(assignment);
        }

        foreach (RoleAssignment assignment in _assignments)
        {
            UpgradeService.SetLevels(
                assignment.SteamId,
                RoleCatalog.TargetUpgrades(assignment.Role, _config));
            StageRolesPlugin.ModLogger.LogInfo(
                $"Assigned {RoleCatalog.AssignmentName(assignment.AssignedRole, assignment.Role)} " +
                $"to player {assignment.SteamId}.");
        }

        _bomber.Begin();
        _medic.Begin(_assignments);
        _mage.Begin(_assignments);
        _gambler.Begin();
        _stinker.Begin(_assignments);
        _trickster.Begin();
        _utilityRoles.Begin(_assignments);
        _eventRoles.Begin(_assignments);
        _diver.Begin(_assignments);
        ApplyKingCrown();
        RoleAssignmentSync.Publish(_assignments);
        RoleGuideSync.Publish(_config);
        _notifier.Begin(_assignments);
        CompleteStagePreparation();
    }

    private void CompleteStagePreparation()
    {
        foreach (RoleAssignment assignment in _assignments) StartInfluenza(assignment);
        _assignmentsInitialized = true;
        _nextPlayerPresenceCheckAt =
            Time.time + PlayerPresenceCheckIntervalSeconds;
    }

    private void Update()
    {
        if (!_stageReady || !_assignmentsInitialized || !IsAuthority())
        {
            return;
        }
        if (Time.time >= _nextPlayerPresenceCheckAt)
        {
            CheckPlayerPresence();
            _nextPlayerPresenceCheckAt =
                Time.time + PlayerPresenceCheckIntervalSeconds;
        }

        TickInfluenza();
        _eventRoles.Tick(_assignments);
        _overhaul.Tick(_assignments, _notifier);
        _mage.MaintainSpawnedObjects();
        RefreshRescueDeaths();
        foreach (RoleAssignment assignment in _assignments)
        {
            if (RoleCatalog.HasCapability(assignment.Role, StageRole.Bomber))
            {
                _bomber.Tick(assignment);
            }
            if (RoleCatalog.HasCapability(assignment.Role, StageRole.Jobless))
            {
                TickJobless(assignment);
            }
            if (RoleCatalog.HasCapability(assignment.Role, StageRole.Phoenix))
            {
                TickPhoenix(assignment);
            }
            if (RoleCatalog.HasCapability(assignment.Role, StageRole.Rescuer))
            {
                TickRescuer(assignment);
            }
            if (RoleCatalog.HasCapability(assignment.Role, StageRole.Tuna))
            {
                TickTuna(assignment);
            }
            if (RoleCatalog.HasCapability(assignment.Role, StageRole.Mage))
            {
                _mage.Tick(assignment);
            }
            if (RoleCatalog.HasCapability(assignment.Role, StageRole.Gambler))
            {
                _gambler.Tick(assignment, _notifier);
            }
            if (RoleCatalog.HasCapability(assignment.Role, StageRole.Stinker))
            {
                _stinker.Tick(assignment);
            }
            if (RoleCatalog.HasCapability(assignment.Role, StageRole.Avenger))
            {
                TickAvenger(assignment);
            }
        }
        _medic.Tick(_assignments);
        _trickster.Tick(_assignments, _notifier);
        _utilityRoles.Tick(_assignments);
        _diver.Tick(_assignments, _notifier);
        NotifyExhaustedAbilities();
        PublishAbilityStatus();
    }

    private void NotifyExhaustedAbilities()
    {
        if (Time.unscaledTime < _nextExhaustionCheckAt) return;
        _nextExhaustionCheckAt = Time.unscaledTime + 0.5f;
        foreach (RoleAssignment assignment in _assignments)
        {
            foreach ((StageRole ability, string message) in ExhaustionMessages)
            {
                bool exhausted = IsAbilityExhausted(assignment, ability);
                AbilityExhaustionState state = assignment.ExhaustionNotifications;
                if (exhausted && (!_config.AnnouncementsEnabled.Value ||
                    !_notifier.CanQueueNotification || !PlayerState.IsLiving(assignment.Player)))
                    continue;
                int key = (int)ability;
                if (!state.TryQueue(key, exhausted)) continue;
                if (!_notifier.NotifyExhaustion(assignment.Player, message,
                    () => _stageReady && ReferenceEquals(FindAssignment(assignment.Player), assignment) &&
                        PlayerState.IsLiving(assignment.Player) && IsAbilityExhausted(assignment, ability),
                    () => state.Sent(key),
                    () => state.Finished(key)))
                {
                    state.Finished(key);
                }
            }
        }
    }

    private bool IsAbilityExhausted(RoleAssignment assignment, StageRole ability)
    {
        if (!RoleCatalog.HasCapability(assignment.Role, ability)) return false;
        return ability switch
        {
            StageRole.Medic => _medic.IsExhausted(assignment.SteamId),
            StageRole.Mage => _config.MageAutoRecoveryEnabled.Value &&
                assignment.MageAutoRecoveryUsed >= Mathf.Clamp(_config.MageAutoRecoveryTotalHealingLimit.Value, 1, 10000),
            StageRole.Mechanic => assignment.MechanicRepairPercentUsed > 0f &&
                assignment.MechanicRepairPercentUsed + 0.0001f >= Mathf.Clamp(_config.MechanicMaximumRepairPercentPerStage.Value, 0f, 100f),
            StageRole.Electrician => assignment.ElectricianChargePercentUsed > 0f &&
                assignment.ElectricianChargePercentUsed + 0.0001f >= Mathf.Clamp(_config.ElectricianMaximumChargePercentPerStage.Value, 0f, 1000f),
            _ => false
        };
    }

    private void FixedUpdate()
    {
        if (_stageReady && _assignmentsInitialized && IsAuthority())
        {
            _diver.FixedTick();
        }
    }

    private void LateUpdate()
    {
        if (_stageReady && _assignmentsInitialized && IsAuthority())
        {
            _eventRoles.LateTick(_assignments);
        }
    }

    private void TickJobless(RoleAssignment assignment)
    {
        PlayerAvatar player = assignment.Player;
        if (!PlayerState.IsLiving(player) || PlayerState.IsInTruck(player))
        {
            assignment.JoblessDamageTimer = 0f;
            return;
        }

        assignment.Overhaul.Start(Time.time, _config.JoblessInitialGrace.Value);
        if (Time.time < assignment.Overhaul.PaidUntil)
        {
            assignment.JoblessDamageTimer = 0f;
            return;
        }

        assignment.JoblessDamageTimer += Time.deltaTime;
        int ticks = 0;
        float interval = _config.ClampedJoblessInterval;
        while (assignment.JoblessDamageTimer >= interval && ticks < 20 &&
               PlayerState.IsLiving(player) && !PlayerState.IsInTruck(player))
        {
            assignment.JoblessDamageTimer -= interval;
            ticks++;
            try
            {
                player.playerHealth?.HurtOther(
                    Mathf.Clamp(_config.JoblessDamage.Value, 1, 100),
                    Vector3.zero,
                    false);
            }
            catch (Exception exception)
            {
                StageRolesPlugin.ModLogger.LogDebug(
                    $"Jobless damage was skipped: {exception.Message}");
                break;
            }
        }
    }

    private void TickTuna(RoleAssignment assignment)
    {
        PlayerAvatar player = assignment.Player;
        Vector3 currentPosition = player.transform.position;
        Vector3 movement = currentPosition - assignment.TunaPreviousPosition;
        assignment.TunaPreviousPosition = currentPosition;

        if (!PlayerState.IsLiving(player))
        {
            assignment.TunaStationaryTimer = 0f;
            assignment.TunaDamageTimer = 0f;
            return;
        }
        if (Time.time < _tunaStageGraceUntil)
        {
            assignment.TunaStationaryTimer = 0f;
            assignment.TunaDamageTimer = 0f;
            return;
        }
        if (movement.sqrMagnitude > TunaMovementThresholdSquared)
        {
            assignment.TunaStationaryTimer = 0f;
            assignment.TunaDamageTimer = 0f;
            return;
        }

        assignment.TunaStationaryTimer += Time.deltaTime;
        float delay = Mathf.Clamp(
            _config.TunaStationaryDelaySeconds.Value,
            0.1f,
            30f);
        if (assignment.TunaStationaryTimer < delay)
        {
            assignment.TunaDamageTimer = 0f;
            return;
        }

        assignment.TunaDamageTimer += Time.deltaTime;
        int ticks = 0;
        float interval = _config.ClampedTunaDamageInterval;
        while (assignment.TunaDamageTimer >= interval && ticks < 20 &&
               PlayerState.IsLiving(player))
        {
            assignment.TunaDamageTimer -= interval;
            ticks++;
            try
            {
                player.playerHealth?.HurtOther(
                    Mathf.Clamp(_config.TunaDamage.Value, 1, 100),
                    Vector3.zero,
                    false);
            }
            catch (Exception exception)
            {
                StageRolesPlugin.ModLogger.LogDebug(
                    $"Tuna damage was skipped: {exception.Message}");
                break;
            }
        }
    }

    private void TickPhoenix(RoleAssignment assignment)
    {
        bool alive = PlayerState.IsLiving(assignment.Player);
        if (assignment.PhoenixUsed)
        {
            assignment.WasAlive = alive;
            return;
        }
        if (alive)
        {
            CompletePhoenixRevival(assignment);
            assignment.WasAlive = true;
            assignment.PhoenixReadyAt = 0f;
            return;
        }
        if (assignment.PhoenixRevivePending)
        {
            return;
        }
        if (StageFluxCompatibility.WillHandleSecondChance(assignment.Player))
        {
            assignment.WasAlive = true;
            assignment.PhoenixReadyAt = 0f;
            return;
        }
        if (assignment.WasAlive)
        {
            SchedulePhoenix(assignment);
        }
        TryRevivePhoenix(assignment);
    }

    private void TickRescuer(RoleAssignment rescuer)
    {
        int maximumRevives = Mathf.Clamp(
            _config.RescuerMaximumRevives.Value,
            1,
            10);
        if (rescuer.RescuerRevivesUsed >= maximumRevives ||
            !PlayerState.IsLiving(rescuer.Player))
        {
            return;
        }

        float delay = Mathf.Clamp(_config.RescuerReviveDelaySeconds.Value, 0f, 10f);
        float radius = Mathf.Clamp(_config.RescuerRadius.Value, 1f, 50f);
        float nearestDistanceSquared = radius * radius;
        RoleAssignment? nearestTarget = null;
        foreach (RoleAssignment target in _assignments)
        {
            if (target.SteamId == rescuer.SteamId ||
                _rescueRevivePending.Contains(target.SteamId) ||
                (RoleCatalog.HasCapability(
                     target.Role,
                     StageRole.Phoenix) && !target.PhoenixUsed) ||
                _eventRoles.HasCorrectiveRevivalPending(target.Player) ||
                StageFluxCompatibility.WillHandleSecondChance(target.Player) ||
                !_rescueDeadSince.TryGetValue(target.SteamId, out float deadSince) ||
                Time.time < deadSince + delay || PlayerState.IsLiving(target.Player) ||
                !PlayerState.HasDeathHead(target.Player))
            {
                continue;
            }

            if (!PlayerState.TryGetDeathHeadPosition(
                    target.Player,
                    out Vector3 deathHeadPosition))
            {
                continue;
            }
            float distanceSquared =
                (deathHeadPosition - rescuer.Player.transform.position).sqrMagnitude;
            if (distanceSquared > nearestDistanceSquared)
            {
                continue;
            }
            nearestDistanceSquared = distanceSquared;
            nearestTarget = target;
        }

        if (nearestTarget == null)
        {
            return;
        }

        try
        {
            if (!PlayerState.TryRequestDeathHeadRevival(nearestTarget.Player))
            {
                return;
            }
            rescuer.RescuerRevivesUsed++;
            _rescueDeadSince.Remove(nearestTarget.SteamId);
            _rescueRevivePending.Add(nearestTarget.SteamId);
            _rescueReviverByTarget[nearestTarget.SteamId] = rescuer.SteamId;
            StageRolesPlugin.ModLogger.LogInfo(
                $"Rescuer {rescuer.SteamId} requested one revival for nearest player " +
                $"{nearestTarget.SteamId} " +
                $"({rescuer.RescuerRevivesUsed}/{maximumRevives}).");
        }
        catch (Exception exception)
        {
            StageRolesPlugin.ModLogger.LogWarning(
                $"Rescuer revival failed: {exception.Message}");
        }
    }

    private void RefreshRescueDeaths()
    {
        // Finish already requested revivals even when the last charge was used.
        foreach (RoleAssignment assignment in _assignments)
        {
            if (PlayerState.IsLiving(assignment.Player) &&
                _rescueRevivePending.Remove(assignment.SteamId))
            {
                _rescueReviverByTarget.Remove(assignment.SteamId);
                SetRevivalHealth(assignment, "Rescuer", _config.RescuerRevivalHealth.Value);
            }
        }
        bool hasAvailableRescuer = false;
        foreach (RoleAssignment assignment in _assignments)
        {
            if (RoleCatalog.HasCapability(
                    assignment.Role,
                    StageRole.Rescuer) &&
                assignment.RescuerRevivesUsed < Mathf.Clamp(
                    _config.RescuerMaximumRevives.Value,
                    1,
                    10))
            {
                hasAvailableRescuer = true;
                break;
            }
        }
        if (!hasAvailableRescuer)
        {
            _rescueDeadSince.Clear();
            return;
        }

        foreach (RoleAssignment assignment in _assignments)
        {
            if (PlayerState.IsLiving(assignment.Player))
            {
                _rescueDeadSince.Remove(assignment.SteamId);
            }
            else if (_rescueRevivePending.Contains(assignment.SteamId))
            {
                continue;
            }
            else if (!_rescueDeadSince.ContainsKey(assignment.SteamId))
            {
                _rescueDeadSince[assignment.SteamId] = Time.time;
            }
        }
    }

    internal bool PlayerHasRole(PlayerAvatar? player, StageRole role)
    {
        if (!_stageReady || player == null)
        {
            return false;
        }
        RoleAssignment? assignment = FindAssignment(player);
        return assignment != null && RoleCatalog.HasCapability(assignment.Role, role);
    }

    internal float InfluencerNoiseMultiplier(PlayerAvatar? player) =>
        PlayerHasExactRole(player, StageRole.Influencer)
            ? Mathf.Clamp(
                _config.InfluencerNoiseRadiusMultiplier.Value,
                1f,
                10f)
            : 1f;

    private bool PlayerHasExactRole(PlayerAvatar? player, StageRole role)
    {
        if (!_stageReady || player == null)
        {
            return false;
        }
        return FindAssignment(player)?.Role == role;
    }

    internal bool ShouldDelayNinjaVision(
        EnemyVision vision,
        int playerId,
        PlayerAvatar player,
        bool playerNear) =>
        _stageReady && IsAuthority() &&
        _utilityRoles.ShouldDelayNinjaVision(
            vision,
            playerId,
            player,
            playerNear,
            _assignments);

    internal void RecordEnemyAttacker(Enemy? enemy, PlayerAvatar? attacker)
    {
        if (!_stageReady || !IsAuthority() || enemy == null)
        {
            return;
        }

        EnemyHealth? enemyHealth = enemy.GetComponent<EnemyHealth>();
        if (enemyHealth == null)
        {
            return;
        }

        int enemyId = enemyHealth.GetInstanceID();
        string steamId = PlayerIdentity.SteamId(attacker);
        if (string.IsNullOrEmpty(steamId))
        {
            _enemyLastAttacker.Remove(enemyId);
            return;
        }
        _enemyLastAttacker[enemyId] = new EnemyAttackCredit(steamId, Time.time);
    }

    internal void RegisterNonPlayerEnemyDamage(EnemyHealth enemyHealth)
    {
        if (!_stageReady || !IsAuthority() || enemyHealth == null)
        {
            return;
        }
        _enemyLastAttacker.Remove(enemyHealth.GetInstanceID());
    }

    internal void ApplyEnemyHitRoleOverrides(
        HurtCollider collider,
        Enemy enemy,
        PlayerAvatar? attacker)
    {
        if (!_stageReady || !IsAuthority() || collider == null || enemy == null)
        {
            return;
        }

        _eventRoles.ApplyEnemyHitOverride(collider, attacker);
        if (attacker == null)
        {
            return;
        }

        PlayerTumble? tumble = collider.GetComponentInParent<PlayerTumble>();
        if (tumble != null &&
            ReferenceEquals(tumble.playerAvatar, attacker) &&
            ReferenceEquals(tumble.hurtCollider, collider) &&
            PlayerHasRole(attacker, StageRole.Rammer))
        {
            collider.enemyDamage = Mathf.Clamp(
                _config.RammerTumbleDamage.Value,
                0,
                100000);
        }

        if (collider.enemyStun && PlayerHasRole(attacker, StageRole.Warden))
        {
            collider.enemyStunTime += Mathf.Clamp(
                _config.WardenAdditionalStunSeconds.Value,
                0f,
                30f);
        }

        if (collider.enemyDamage > 0 && enemy.IsStunned() &&
            PlayerHasRole(attacker, StageRole.Executioner))
        {
            collider.enemyDamage = Mathf.Clamp(
                Mathf.RoundToInt(
                    collider.enemyDamage * Mathf.Clamp(
                        _config.ExecutionerStunnedDamageMultiplier.Value,
                        1f,
                        10f)),
                0,
                100000);
        }

        ApplyBrawlerEnemyDamage(collider, attacker);
        ApplySniperDamage(collider, enemy, attacker);
        ApplyAvengerDamage(collider, attacker);
    }

    internal bool ShouldSuppressRammerVanillaSelfDamage(PlayerTumble tumble)
    {
        if (!_stageReady || !IsAuthority() || tumble == null ||
            tumble.playerAvatar == null)
        {
            return false;
        }

        RoleAssignment? assignment = FindAssignment(tumble.playerAvatar);
        return assignment?.Role == StageRole.Rammer;
    }

    internal void ApplyRammerSelfDamageAfterEnemyHit(
        HurtCollider collider,
        PlayerAvatar? attacker)
    {
        if (!_stageReady || !IsAuthority() || collider == null ||
            attacker == null)
        {
            return;
        }

        PlayerTumble? tumble = collider.GetComponentInParent<PlayerTumble>();
        if (tumble == null ||
            !ReferenceEquals(tumble.playerAvatar, attacker) ||
            !ReferenceEquals(tumble.hurtCollider, collider))
        {
            return;
        }

        RoleAssignment? assignment = FindAssignment(attacker);
        if (assignment?.Role != StageRole.Rammer)
        {
            return;
        }

        int damage = Mathf.Clamp(
            _config.RammerSelfDamage.Value,
            0,
            100000);
        if (damage <= 0)
        {
            return;
        }

        try
        {
            tumble.playerAvatar.playerHealth?.HurtOther(
                damage,
                Vector3.zero,
                savingGrace: true);
        }
        catch (Exception exception)
        {
            StageRolesPlugin.ModLogger.LogWarning(
                "Rammer self-damage failed: " +
                exception.Message);
        }
    }

    private void ApplyBrawlerEnemyDamage(
        HurtCollider collider,
        PlayerAvatar attacker)
    {
        if (collider.enemyDamage <= 0 ||
            !PlayerHasRole(attacker, StageRole.Brawler) ||
            !TryGetBrawlerMultiplier(collider, out float multiplier))
        {
            return;
        }
        collider.enemyDamage = ScaleWeaponDamage(
            collider.enemyDamage,
            multiplier);
    }

    internal void ApplyPlayerHitRoleOverrides(
        HurtCollider collider,
        PlayerAvatar? attacker)
    {
        if (!_stageReady || !IsAuthority() || collider == null ||
            attacker == null || collider.playerDamage <= 0 ||
            !PlayerHasRole(attacker, StageRole.Brawler) ||
            !TryGetBrawlerMultiplier(collider, out float multiplier))
        {
            return;
        }
        collider.playerDamage = ScaleWeaponDamage(
            collider.playerDamage,
            multiplier);
    }

    private bool TryGetBrawlerMultiplier(
        HurtCollider collider,
        out float multiplier)
    {
        if (collider.GetComponentInParent<ItemMelee>() != null)
        {
            multiplier = Mathf.Clamp(
                _config.BrawlerMeleeDamageMultiplier.Value,
                0f,
                10f);
            return true;
        }
        if (collider.GetComponentInParent<ItemGunBullet>() != null ||
            collider.GetComponentInParent<ItemGun>() != null ||
            collider.GetComponentInParent<SlowProjectile>() != null ||
            collider.GetComponentInParent<SemiLaser>() != null)
        {
            multiplier = Mathf.Clamp(
                _config.BrawlerRangedDamageMultiplier.Value,
                0f,
                10f);
            return true;
        }
        multiplier = 1f;
        return false;
    }

    private static int ScaleWeaponDamage(int damage, float multiplier)
    {
        if (multiplier <= 0f)
        {
            return 0;
        }
        return Mathf.Clamp(
            Mathf.Max(1, Mathf.RoundToInt(damage * multiplier)),
            1,
            100000);
    }

    private void ApplyAvengerDamage(
        HurtCollider collider,
        PlayerAvatar attacker)
    {
        if (collider.enemyDamage <= 0)
        {
            return;
        }
        RoleAssignment? assignment = FindAssignment(attacker);
        if (assignment == null ||
            !RoleCatalog.HasCapability(assignment.Role, StageRole.Avenger) ||
            Time.time >= assignment.AvengerEmpoweredUntil)
        {
            return;
        }

        collider.enemyDamage = Mathf.Clamp(
            Mathf.Max(
                1,
                Mathf.RoundToInt(
                    collider.enemyDamage * Mathf.Clamp(
                        _config.AvengerDamageMultiplier.Value,
                        1f,
                        10f))),
            1,
            100000);
    }

    internal void PlayerDied(PlayerAvatar player)
    {
        if (!_stageReady || !IsAuthority() || player == null)
        {
            return;
        }
        string deadSteamId = PlayerIdentity.SteamId(player);
        RoleHealingRuntime.Forget(player);
        float triggerRadius = Mathf.Clamp(
            _config.AvengerTriggerRadius.Value,
            1f,
            100f);
        float triggerRadiusSquared = triggerRadius * triggerRadius;
        float empoweredUntil = Time.time + Mathf.Clamp(
            _config.AvengerDurationSeconds.Value,
            1f,
            120f);
        foreach (RoleAssignment assignment in _assignments)
        {
            if (ReferenceEquals(assignment.Player, player) ||
                (!string.IsNullOrEmpty(deadSteamId) &&
                 string.Equals(
                     assignment.SteamId,
                     deadSteamId,
                     StringComparison.Ordinal)) ||
                !PlayerState.IsLiving(assignment.Player) ||
                !RoleCatalog.HasCapability(
                    assignment.Role,
                    StageRole.Avenger))
            {
                continue;
            }

            Vector3 offset = assignment.Player.transform.position -
                player.transform.position;
            if (offset.sqrMagnitude > triggerRadiusSquared)
            {
                continue;
            }

            bool wasEmpowered = Time.time < assignment.AvengerEmpoweredUntil;
            assignment.AvengerEmpoweredUntil = empoweredUntil;
            _notifier.NotifyConditional(
                assignment.Player,
                wasEmpowered ? "AvengerUpdated" : "AvengerActive",
                () => RoleCatalog.HasCapability(
                          assignment.Role,
                          StageRole.Avenger) &&
                      Time.time < assignment.AvengerEmpoweredUntil);
        }
    }

    private void TickAvenger(RoleAssignment assignment)
    {
        if (assignment.AvengerEmpoweredUntil <= 0f ||
            Time.time < assignment.AvengerEmpoweredUntil)
        {
            return;
        }

        assignment.AvengerEmpoweredUntil = 0f;
        _notifier.NotifyConditional(
            assignment.Player,
            "AvengerEnded",
            () => RoleCatalog.HasCapability(
                      assignment.Role,
                      StageRole.Avenger) &&
                  assignment.AvengerEmpoweredUntil <= 0f);
    }

    private void ApplySniperDamage(
        HurtCollider collider,
        Enemy enemy,
        PlayerAvatar attacker)
    {
        if (collider.enemyDamage <= 0 ||
            !PlayerHasRole(attacker, StageRole.Sniper) ||
            collider.GetComponentInParent<PlayerTumble>() != null ||
            collider.GetComponentInParent<ItemVehicle>() != null)
        {
            return;
        }

        float distance = Vector3.Distance(
            attacker.transform.position,
            enemy.transform.position);
        if (float.IsNaN(distance) || float.IsInfinity(distance))
        {
            return;
        }

        float referenceDistance = Mathf.Clamp(
            _config.SniperReferenceDistance.Value,
            1f,
            50f);
        float minimumMultiplier = Mathf.Clamp(
            _config.SniperMinimumDamageMultiplier.Value,
            0f,
            1f);
        float maximumMultiplier = Mathf.Clamp(
            _config.SniperMaximumDamageMultiplier.Value,
            1f,
            10f);
        float maximumDistance = Mathf.Max(
            referenceDistance + 0.1f,
            Mathf.Clamp(
                _config.SniperMaximumMultiplierDistance.Value,
                1f,
                100f));
        float multiplier = distance <= referenceDistance
            ? Mathf.Lerp(
                minimumMultiplier,
                1f,
                Mathf.Clamp01(distance / referenceDistance))
            : Mathf.Lerp(
                1f,
                maximumMultiplier,
                Mathf.Clamp01(
                    (distance - referenceDistance) /
                    (maximumDistance - referenceDistance)));

        collider.enemyDamage = Mathf.Clamp(
            Mathf.Max(
                1,
                Mathf.RoundToInt(collider.enemyDamage * multiplier)),
            1,
            100000);
    }

    internal void RegisterPlayerHit(
        HurtCollider collider,
        PlayerAvatar target,
        PlayerAvatar? attacker)
    {
        if (_stageReady && IsAuthority())
        {
            _eventRoles.RegisterPlayerHit(collider, target, attacker);
        }
    }

    internal void ObserveHealthUpdate(
        PlayerAvatar player,
        int previousHealth,
        int currentHealth,
        bool healingAcknowledgement = false)
    {
        if (_stageReady && IsAuthority())
        {
            if (healingAcknowledgement)
                RoleHealingRuntime.Observe(player, previousHealth, currentHealth);
            _mage.ObserveHealthUpdate(
                player,
                previousHealth,
                currentHealth);
            _eventRoles.ObserveHealthUpdate(
                player,
                previousHealth,
                currentHealth);
        }
    }

    internal void EndPlayerHit(HurtCollider collider, PlayerAvatar target) =>
        _eventRoles.EndPlayerHit(collider, target);

    internal IReadOnlyList<RoleAssignment> Assignments => _assignments;

    internal bool HasPendingAutomaticNotifications =>
        _notifier?.HasPendingNotifications == true;

    internal bool RoleAssignmentsReady =>
        _stageReady && _assignmentsInitialized && IsAuthority();

    internal string AssignedRoleName(PlayerAvatar player)
    {
        if (!RoleAssignmentsReady || player == null)
        {
            return string.Empty;
        }
        string steamId = PlayerIdentity.SteamId(player);
        foreach (RoleAssignment assignment in _assignments)
        {
            if (ReferenceEquals(assignment.Player, player) ||
                (!string.IsNullOrEmpty(steamId) &&
                 string.Equals(assignment.SteamId, steamId, StringComparison.Ordinal)))
            {
                return RoleCatalog.AssignmentName(assignment.AssignedRole);
            }
        }
        return string.Empty;
    }

    internal int ActiveRoleCount(string roleName)
    {
        if (!RoleAssignmentsReady ||
            !Enum.TryParse(roleName, true, out StageRole role))
        {
            return 0;
        }
        int count = 0;
        foreach (RoleAssignment assignment in _assignments)
        {
            if (assignment.AssignedRole == role)
            {
                count++;
            }
        }
        return count;
    }

    internal bool HasCorrectiveRevivalPending(PlayerAvatar player) =>
        _stageReady && IsAuthority() &&
        _eventRoles.HasCorrectiveRevivalPending(player);

    internal void NotifyExternalRevival(PlayerAvatar player)
    {
        if (!_stageReady || !IsAuthority() || player == null)
        {
            return;
        }

        string steamId = PlayerIdentity.SteamId(player);
        if (string.IsNullOrEmpty(steamId))
        {
            return;
        }

        _eventRoles.ExternalPlayerRevived(player);
        foreach (RoleAssignment assignment in _assignments)
        {
            if (!string.Equals(
                    assignment.SteamId,
                    steamId,
                    StringComparison.Ordinal))
            {
                continue;
            }
            assignment.WasAlive = true;
            assignment.PhoenixRevivePending = false;
            assignment.PhoenixReadyAt = 0f;
            break;
        }

        _rescueDeadSince.Remove(steamId);
        if (_rescueRevivePending.Remove(steamId) &&
            _rescueReviverByTarget.Remove(steamId, out string rescuerSteamId))
        {
            foreach (RoleAssignment assignment in _assignments)
            {
                if (string.Equals(
                        assignment.SteamId,
                        rescuerSteamId,
                        StringComparison.Ordinal))
                {
                    assignment.RescuerRevivesUsed = Math.Max(
                        0,
                        assignment.RescuerRevivesUsed - 1);
                    break;
                }
            }
        }
    }

    internal bool RoleAssignedToPlayer(string steamId, StageRole role) =>
        RoleAssignedTo(steamId, role);

    internal void EnemyDied(EnemyHealth enemyHealth)
    {
        if (!_stageReady || !IsAuthority() || enemyHealth == null)
        {
            return;
        }

        Vector3 deathPosition = enemyHealth.transform.position;
        float radius = Mathf.Clamp(_config.VampireRadius.Value, 1f, 50f);
        float radiusSquared = radius * radius;
        int enemyTier = ResolveEnemyRewardTier(enemyHealth);
        int healAmount = Mathf.Clamp(
            enemyTier switch
            {
                3 => _config.VampireTier3HealAmount.Value,
                2 => _config.VampireTier2HealAmount.Value,
                _ => _config.VampireTier1HealAmount.Value
            },
            1,
            100);
        foreach (RoleAssignment assignment in _assignments)
        {
            if (!RoleCatalog.HasCapability(
                    assignment.Role,
                    StageRole.Vampire) ||
                !PlayerState.IsLiving(assignment.Player) ||
                (assignment.Player.transform.position - deathPosition).sqrMagnitude > radiusSquared)
            {
                continue;
            }
            try
            {
                assignment.Player.playerHealth?.HealOther(healAmount, true);
            }
            catch (Exception exception)
            {
                StageRolesPlugin.ModLogger.LogDebug(
                    $"Vampire healing was skipped: {exception.Message}");
            }
        }
    }

    internal int CaptureEnemyOrbSpawnCount(EnemyParent enemyParent)
    {
        if (!_stageReady || !IsAuthority() || enemyParent == null)
        {
            return -1;
        }
        EnemyHealth? enemyHealth =
            enemyParent.GetComponentInChildren<EnemyHealth>(true);
        return ReadEnemyOrbSpawnCount(enemyHealth);
    }

    internal void EnemyDespawned(EnemyParent enemyParent, int previousSpawnCount)
    {
        if (!_stageReady || !IsAuthority() || enemyParent == null ||
            previousSpawnCount < 0)
        {
            return;
        }

        EnemyHealth? enemyHealth =
            enemyParent.GetComponentInChildren<EnemyHealth>(true);
        int currentSpawnCount = ReadEnemyOrbSpawnCount(enemyHealth);
        int normalDropCount = currentSpawnCount - previousSpawnCount;
        if (enemyHealth == null || normalDropCount <= 0)
        {
            return;
        }

        int enemyId = enemyHealth.GetInstanceID();
        if (!_enemyLastAttacker.Remove(enemyId, out EnemyAttackCredit credit) ||
            Time.time - credit.RecordedAt > HunterKillCreditLifetimeSeconds ||
            !RoleAssignedTo(credit.SteamId, StageRole.Hunter))
        {
            return;
        }

        float jackpotChance = Mathf.Clamp(
            _config.HunterJackpotOrbChancePercent.Value,
            0f,
            100f) / 100f;
        float doubleChance = Mathf.Clamp(
            _config.HunterDoubleOrbChancePercent.Value,
            0f,
            100f) / 100f;

        int targetDropCount;
        string result;
        if (UnityEngine.Random.value < jackpotChance)
        {
            targetDropCount = Mathf.Clamp(
                _config.HunterJackpotOrbCount.Value,
                1,
                30);
            result = "jackpot";
        }
        else if (UnityEngine.Random.value < doubleChance)
        {
            targetDropCount = Mathf.Clamp(normalDropCount * 2, 1, 100);
            result = "double";
        }
        else
        {
            return;
        }

        int extraCount = Mathf.Max(0, targetDropCount - normalDropCount);
        int spawnedExtraCount = SpawnHunterBonusOrbs(
            enemyHealth,
            extraCount);
        int actualDropCount = normalDropCount + spawnedExtraCount;
        StageRolesPlugin.ModLogger.LogInfo(
            $"Hunter orb roll {result}: {normalDropCount} -> " +
            $"{actualDropCount} for player {credit.SteamId}.");
    }

    private static int ReadEnemyOrbSpawnCount(EnemyHealth? enemyHealth)
    {
        try
        {
            return enemyHealth != null &&
                   EnemySpawnValuableCurrentField?.GetValue(enemyHealth) is int count
                ? count
                : -1;
        }
        catch (Exception exception)
        {
            StageRolesPlugin.ModLogger.LogDebug(
                $"Hunter orb count could not be read: {exception.Message}");
            return -1;
        }
    }

    private int SpawnHunterBonusOrbs(
        EnemyHealth enemyHealth,
        int extraCount)
    {
        if (extraCount <= 0 || AssetManager.instance == null)
        {
            return 0;
        }

        int tier = ResolveEnemyRewardTier(enemyHealth);
        GameObject prefab = tier switch
        {
            3 => AssetManager.instance.enemyValuableBig,
            2 => AssetManager.instance.enemyValuableMedium,
            _ => AssetManager.instance.enemyValuableSmall
        };
        if (prefab == null)
        {
            return 0;
        }

        int spawned = 0;
        Vector3 position = ResolveEnemyOrbSpawnPosition(enemyHealth);
        for (int index = 0; index < extraCount; index++)
        {
            try
            {
                if (SemiFunc.IsMultiplayer())
                {
                    PhotonNetwork.InstantiateRoomObject(
                        $"Valuables/{prefab.name}",
                        position,
                        Quaternion.identity,
                        0);
                }
                else
                {
                    UnityEngine.Object.Instantiate(
                        prefab,
                        position,
                        Quaternion.identity);
                }
                spawned++;
            }
            catch (Exception exception)
            {
                StageRolesPlugin.ModLogger.LogWarning(
                    $"Hunter bonus orb spawning stopped after {spawned}/{extraCount}: {exception.Message}");
                break;
            }
        }
        return spawned;
    }

    private static Vector3 ResolveEnemyOrbSpawnPosition(EnemyHealth enemyHealth)
    {
        try
        {
            if (EnemyHealthEnemyField?.GetValue(enemyHealth) is Enemy enemy)
            {
                if (enemy.CustomValuableSpawnTransform != null)
                {
                    return enemy.CustomValuableSpawnTransform.position;
                }
                if (enemy.CenterTransform != null)
                {
                    return enemy.CenterTransform.position;
                }
            }
        }
        catch (Exception exception)
        {
            StageRolesPlugin.ModLogger.LogDebug(
                $"Hunter orb spawn position fell back to enemy health: {exception.Message}");
        }
        return enemyHealth.transform.position;
    }

    private bool RoleAssignedTo(string steamId, StageRole role)
    {
        foreach (RoleAssignment assignment in _assignments)
        {
            if (RoleCatalog.HasCapability(assignment.Role, role) &&
                string.Equals(assignment.SteamId, steamId, StringComparison.Ordinal))
            {
                return true;
            }
        }
        return false;
    }

    internal void InstrumentNotePlayed(int grabberViewId)
    {
        if (!_stageReady || !IsAuthority())
        {
            return;
        }

        PlayerAvatar? musician = ResolveInstrumentPlayer(grabberViewId);
        if (musician == null)
        {
            return;
        }
        string musicianSteamId = PlayerIdentity.SteamId(musician);
        bool assignedMusician = false;
        foreach (RoleAssignment assignment in _assignments)
        {
            if (RoleCatalog.HasCapability(
                    assignment.Role,
                    StageRole.Musician) &&
                (ReferenceEquals(assignment.Player, musician) ||
                 (!string.IsNullOrEmpty(musicianSteamId) &&
                  string.Equals(
                      assignment.SteamId,
                      musicianSteamId,
                      StringComparison.Ordinal))))
            {
                assignedMusician = true;
                musician = assignment.Player;
                break;
            }
        }
        if (!assignedMusician || !PlayerState.IsLiving(musician))
        {
            return;
        }

        float radius = Mathf.Clamp(_config.MusicianHealRadius.Value, 1f, 50f);
        float radiusSquared = radius * radius;
        int healAmount = Mathf.Clamp(_config.MusicianHealAmount.Value, 1, 100);
        int healedPlayers = 0;
        if (GameDirector.instance == null)
        {
            return;
        }
        foreach (PlayerAvatar player in GameDirector.instance.PlayerList)
        {
            if (!PlayerState.IsLiving(player) ||
                (player.transform.position - musician.transform.position).sqrMagnitude >
                radiusSquared)
            {
                continue;
            }
            try
            {
                player.playerHealth?.HealOther(healAmount, true);
                healedPlayers++;
            }
            catch (Exception exception)
            {
                StageRolesPlugin.ModLogger.LogDebug(
                    $"Musician healing was skipped: {exception.Message}");
            }
        }
        StageRolesPlugin.ModLogger.LogDebug(
            $"Musician note healed {healedPlayers} player(s) for {healAmount} HP.");
    }

    private static PlayerAvatar? ResolveInstrumentPlayer(int grabberViewId)
    {
        try
        {
            PhysGrabber? grabber = null;
            if (SemiFunc.IsMultiplayer() && grabberViewId >= 0)
            {
                PhotonView? view = PhotonView.Find(grabberViewId);
                grabber = view != null ? view.GetComponent<PhysGrabber>() : null;
            }
            else
            {
                grabber = PhysGrabber.instance;
            }
            return grabber?.playerAvatar;
        }
        catch (Exception exception)
        {
            StageRolesPlugin.ModLogger.LogDebug(
                $"Musician instrument player could not be resolved: {exception.Message}");
            return null;
        }
    }

    internal static int ResolveEnemyTier(EnemyHealth enemyHealth)
    {
        try
        {
            if (EnemyHealthEnemyField?.GetValue(enemyHealth) is Enemy enemy &&
                EnemyParentField?.GetValue(enemy) is EnemyParent enemyParent)
            {
                object? difficulty = EnemyDifficultyField?.GetValue(enemyParent);
                if (difficulty != null)
                {
                    int tier = Convert.ToInt32(difficulty) + 1;
                    if (tier >= 1 && tier <= 3)
                    {
                        return tier;
                    }
                }
            }
        }
        catch (Exception exception)
        {
            StageRolesPlugin.ModLogger.LogDebug(
                $"Vampire enemy tier resolution failed: {exception.Message}");
        }
        return 1;
    }

    private int ResolveEnemyRewardTier(EnemyHealth enemyHealth)
    {
        int vanillaTier = ResolveEnemyTier(enemyHealth);
        if (!_config.EnhancedEnemyRewardsEnabled.Value)
        {
            return vanillaTier;
        }

        return Mathf.Clamp(
            vanillaTier +
            EliteEnemyVariantsCompatibility.GetRewardTierBonus(enemyHealth),
            1,
            3);
    }

    private void SchedulePhoenix(RoleAssignment assignment)
    {
        if (assignment.PhoenixReadyAt <= 0f)
        {
            assignment.PhoenixReadyAt = Time.time +
                Mathf.Clamp(_config.PhoenixReviveDelaySeconds.Value, 2f, 10f);
            StageRolesPlugin.ModLogger.LogDebug(
                $"Phoenix revival scheduled for player {assignment.SteamId}.");
        }
        assignment.WasAlive = true;
    }

    private void TryRevivePhoenix(RoleAssignment assignment)
    {
        if (assignment.PhoenixUsed || assignment.PhoenixRevivePending ||
            assignment.PhoenixReadyAt <= 0f ||
            Time.time < assignment.PhoenixReadyAt)
        {
            return;
        }

        try
        {
            assignment.PhoenixRevivePending = true;
            if (!PlayerState.TryRequestDeathHeadRevival(assignment.Player))
            {
                assignment.PhoenixRevivePending = false;
                return;
            }
            assignment.PhoenixReadyAt = 0f;
            StageRolesPlugin.ModLogger.LogInfo(
                $"Phoenix requested one revival for player {assignment.SteamId}.");
            if (PlayerState.IsLiving(assignment.Player))
            {
                CompletePhoenixRevival(assignment);
            }
        }
        catch (Exception exception)
        {
            assignment.PhoenixRevivePending = false;
            StageRolesPlugin.ModLogger.LogWarning(
                $"Phoenix revival failed: {exception.Message}");
        }
    }

    private void CompletePhoenixRevival(RoleAssignment assignment)
    {
        if (!assignment.PhoenixRevivePending)
        {
            return;
        }
        assignment.PhoenixRevivePending = false;
        assignment.PhoenixUsed = true;
        assignment.PhoenixReadyAt = 0f;
        assignment.InfluencerNextTtsAt = 0f;
        assignment.WasAlive = true;
        SetRevivalHealth(
            assignment,
            "Phoenix",
            _config.PhoenixRevivalHealth.Value);
        StageRolesPlugin.ModLogger.LogInfo(
            $"Phoenix revival synchronized for player {assignment.SteamId}.");
    }

    private static void SetRevivalHealth(
        RoleAssignment assignment,
        string source,
        int configuredHealth)
    {
        int revivalHealth = Mathf.Clamp(configuredHealth, 1, 1000);
        try
        {
            if (!PlayerState.SetHealthSynchronized(assignment.Player, revivalHealth))
            {
                StageRolesPlugin.ModLogger.LogWarning(
                    $"{source} could not set revival health for player {assignment.SteamId}.");
                return;
            }
            StageRolesPlugin.ModLogger.LogInfo(
                $"{source} set revival health to {revivalHealth} for player {assignment.SteamId}.");
        }
        catch (Exception exception)
        {
            StageRolesPlugin.ModLogger.LogWarning(
                $"{source} revival health update failed for player {assignment.SteamId}: " +
                exception.Message);
        }
    }

    private void CheckPlayerPresence()
    {
        List<PlayerAvatar> players = CollectPlayers();
        Dictionary<string, PlayerAvatar> playersBySteamId =
            new(StringComparer.Ordinal);
        foreach (PlayerAvatar player in players)
        {
            string steamId = PlayerIdentity.SteamId(player);
            if (!string.IsNullOrEmpty(steamId))
            {
                playersBySteamId[steamId] = player;
            }
        }

        HashSet<string> connectedSteamIds = ConnectedSteamIds(playersBySteamId);
        if (connectedSteamIds.Count == 0)
        {
            return;
        }

        List<RoleAssignment> departed = new();
        bool assignmentsChanged = false;
        for (int index = _assignments.Count - 1; index >= 0; index--)
        {
            RoleAssignment assignment = _assignments[index];
            if (connectedSteamIds.Contains(assignment.SteamId) ||
                PlayerState.HasDeathHead(assignment.Player))
            {
                if (_consecutiveMissingPlayerChecks.Remove(assignment.SteamId))
                {
                    StageRolesPlugin.ModLogger.LogInfo(
                        $"Player presence reconfirmed for {assignment.SteamId}; the role assignment was retained.");
                }
            }
            else
            {
                _consecutiveMissingPlayerChecks.TryGetValue(
                    assignment.SteamId,
                    out int missingChecks);
                missingChecks++;
                if (missingChecks >= PlayerMissingChecksBeforeRemoval)
                {
                    departed.Add(assignment);
                    UnindexAssignment(assignment);
                    _assignments.RemoveAt(index);
                    _consecutiveMissingPlayerChecks.Remove(assignment.SteamId);
                    _departedAssignments[assignment.SteamId] = assignment;
                    assignmentsChanged = true;
                    continue;
                }
                _consecutiveMissingPlayerChecks[assignment.SteamId] = missingChecks;
                StageRolesPlugin.ModLogger.LogDebug(
                    $"Player {assignment.SteamId} was not found during presence check " +
                    $"{missingChecks}/{PlayerMissingChecksBeforeRemoval}.");
                continue;
            }
            if (playersBySteamId.TryGetValue(
                    assignment.SteamId,
                    out PlayerAvatar currentPlayer))
            {
                if (!ReferenceEquals(assignment.Player, currentPlayer))
                {
                    UnindexAssignment(assignment);
                    assignment.Player = currentPlayer;
                    IndexAssignment(assignment);
                }
                int actorNumber = PlayerIdentity.ActorNumber(currentPlayer);
                if (actorNumber != 0)
                {
                    assignment.ActorNumber = actorNumber;
                }
            }
        }

        foreach (RoleAssignment assignment in departed)
        {
            _bomber.RemovePlayer(assignment.SteamId);
            _medic.RemovePlayer(
                assignment.SteamId,
                preserveHealingState: true);
            _stinker.RemovePlayer(assignment.SteamId);
            _trickster.RemovePlayer(assignment.SteamId);
            _rescueDeadSince.Remove(assignment.SteamId);
            _rescueRevivePending.Remove(assignment.SteamId);
            _rescueReviverByTarget.Remove(assignment.SteamId);
            _roleQueryAllowedAt.Remove(assignment.SteamId);
            if (RoleCatalog.HasCapability(assignment.Role, StageRole.King))
            {
                RestoreKingCrown();
            }
            StageRolesPlugin.ModLogger.LogInfo(
                $"Removed departed player {assignment.SteamId} ({assignment.Role}) from the active role list.");
        }

        foreach (KeyValuePair<string, PlayerAvatar> pair in playersBySteamId)
        {
            string steamId = pair.Key;
            if (!connectedSteamIds.Contains(steamId) ||
                HasActiveAssignment(steamId))
            {
                continue;
            }

            if (_departedAssignments.Remove(
                    steamId,
                    out RoleAssignment restoredAssignment))
            {
                restoredAssignment.Player = pair.Value;
                restoredAssignment.ActorNumber = PlayerIdentity.ActorNumber(pair.Value);
                IReadOnlyList<UpgradeGrant> restoredTargets =
                    RoleCatalog.TargetUpgrades(
                        restoredAssignment.Role,
                        _config);
                UpgradeService.EnsureAtLeastLevels(
                    steamId,
                    restoredTargets);
                if (restoredAssignment.Role == StageRole.Rammer)
                {
                    UpgradeService.SetLevels(
                        steamId,
                        RoleCatalog.RammerLockedUpgrades);
                }
                RegisterAssignment(restoredAssignment);
                _consecutiveMissingPlayerChecks.Remove(steamId);
                ActivateJoinedAssignment(restoredAssignment);
                _notifier.Notify(
                    pair.Value,
                    RoleCatalog.AssignmentName(
                        restoredAssignment.AssignedRole,
                        restoredAssignment.Role));
                assignmentsChanged = true;
                StageRolesPlugin.ModLogger.LogInfo(
                    $"Restored {restoredAssignment.Role} for returning player {steamId}.");
                continue;
            }

            RoleAssignment? newAssignment = AssignRoleToNewPlayer(
                steamId,
                pair.Value,
                connectedSteamIds.Count);
            if (newAssignment == null)
            {
                continue;
            }
            RegisterAssignment(newAssignment);
            ActivateJoinedAssignment(newAssignment);
            _notifier.Notify(
                pair.Value,
                RoleCatalog.AssignmentName(
                    newAssignment.AssignedRole,
                    newAssignment.Role));
            assignmentsChanged = true;
            StageRolesPlugin.ModLogger.LogInfo(
                $"Assigned {newAssignment.Role} to newly detected player {steamId}.");
        }

        if (assignmentsChanged)
        {
            RoleAssignmentSync.Publish(_assignments);
        }
    }

    private bool HasActiveAssignment(string steamId)
    {
        return _assignmentsBySteamId.ContainsKey(steamId);
    }

    private RoleAssignment? AssignRoleToNewPlayer(
        string steamId,
        PlayerAvatar player,
        int connectedPlayerCount)
    {
        List<StageRole> enabledRoles = EnabledRoles();
        if (!SemiFunc.IsMultiplayer() || connectedPlayerCount <= 1)
        {
            RemoveSoloIncompatibleRoles(enabledRoles);
        }
        RemoveUnavailableContextRoles(enabledRoles);
        if (!HasActiveImitatorCopyTarget())
        {
            enabledRoles.Remove(StageRole.Imitator);
        }
        RemoveOrphanedSuperbot(enabledRoles);
        if (enabledRoles.Count == 0)
        {
            UpgradeService.SetLevels(
                steamId,
                RoleCatalog.BaseUpgrades(_config));
            StageRolesPlugin.ModLogger.LogWarning(
                $"No role was available for newly detected player {steamId}.");
            return null;
        }

        List<StageRole> reservedRoles = new();
        foreach (RoleAssignment reservedAssignment in _assignments)
        {
            reservedRoles.Add(reservedAssignment.AssignedRole);
        }
        foreach (RoleAssignment reservedAssignment in _departedAssignments.Values)
        {
            reservedRoles.Add(reservedAssignment.AssignedRole);
        }

        RoleAssignmentPlanner planner = new(
            _config,
            _previousRoles,
            _recentRolesByPlayer,
            _lastHardshipStages,
            Math.Max(1, _currentAssignmentStage));
        StageRole? plannedRole = planner.PlanJoinedAssignment(
            steamId,
            enabledRoles,
            reservedRoles,
            connectedPlayerCount);
        if (plannedRole == null)
        {
            UpgradeService.SetLevels(
                steamId,
                RoleCatalog.BaseUpgrades(_config));
            StageRolesPlugin.ModLogger.LogWarning(
                $"No repeatable role was available for newly detected player {steamId}.");
            return null;
        }

        StageRole role = plannedRole.Value;
        RoleAssignment assignment = new(steamId, player, role);
        UpgradeService.SetLevels(
            steamId,
            RoleCatalog.TargetUpgrades(assignment.Role, _config));
        return assignment;
    }

    private static void RemoveSoloIncompatibleRoles(List<StageRole> roles)
    {
        foreach (StageRole role in SoloExcludedRoles)
        {
            roles.Remove(role);
        }
    }

    internal void TryCopyImitatorRole(
        StaticGrabObject grabbedObject,
        int grabberViewId)
    {
        if (!_stageReady || !_assignmentsInitialized || !IsAuthority() ||
            grabbedObject == null)
        {
            return;
        }

        PlayerHealthGrab? healthGrab =
            grabbedObject.GetComponent<PlayerHealthGrab>() ??
            grabbedObject.GetComponentInParent<PlayerHealthGrab>() ??
            grabbedObject.GetComponentInChildren<PlayerHealthGrab>(true);
        PlayerAvatar? targetPlayer = healthGrab?.playerAvatar;
        PhotonView? grabberView = PhotonView.Find(grabberViewId);
        PlayerAvatar? mimicPlayer = grabberView != null
            ? grabberView.GetComponent<PhysGrabber>()?.playerAvatar ??
              grabberView.GetComponent<PlayerAvatar>()
            : null;
        if (targetPlayer == null || mimicPlayer == null ||
            ReferenceEquals(targetPlayer, mimicPlayer))
        {
            return;
        }

        RoleAssignment? mimic = FindAssignment(mimicPlayer);
        RoleAssignment? target = FindAssignment(targetPlayer);
        if (mimic == null || target == null ||
            mimic.AssignedRole != StageRole.Imitator ||
            mimic.Role != StageRole.Imitator ||
            !RoleCatalog.CanBeCopiedByImitator(target.AssignedRole) ||
            target.Role == StageRole.Imitator)
        {
            return;
        }

        mimic.Role = target.Role;
        ResetAssignmentForRoleChange(mimic);
        UpgradeService.SetLevels(
            mimic.SteamId,
            RoleCatalog.TargetUpgrades(mimic.Role, _config));
        _medic.AddPlayer(mimic);
        _mage.AddPlayer(mimic);
        _eventRoles.AddPlayer(mimic);
        RoleAssignmentSync.Publish(_assignments);
        _notifier.Notify(
            mimic.Player,
            RoleCatalog.AssignmentName(mimic.Role));
        StageRolesPlugin.ModLogger.LogInfo(
            $"Imitator {mimic.SteamId} copied {target.AssignedRole} " +
            $"from player {target.SteamId}.");
    }

    private static void RemoveOrphanedSuperbot(List<StageRole> roles)
    {
        foreach (StageRole role in roles)
        {
            if (!RoleCatalog.IsSecretRole(role))
            {
                return;
            }
        }
        roles.RemoveAll(RoleCatalog.IsSecretRole);
    }

    private void RemoveUnavailableContextRoles(List<StageRole> roles)
    {
        if (!_config.ExcludeUnavailableContextRoles.Value)
        {
            return;
        }

        try
        {
            if (UnityEngine.Object.FindObjectsOfType<MusicalValuableLogic>(true).Length == 0)
            {
                roles.Remove(StageRole.Musician);
            }

            if (UnityEngine.Object.FindObjectsOfType<ItemVehicle>(true).Length == 0)
            {
                roles.Remove(StageRole.Rider);
            }

            if (roles.Contains(StageRole.Brawler) || roles.Contains(StageRole.Sniper))
            {
                bool hasMeleeWeapon = HasNonValuableWeaponContext<ItemMelee>();
                if (!hasMeleeWeapon)
                {
                    roles.Remove(StageRole.Brawler);
                }
                if (roles.Contains(StageRole.Sniper) &&
                    !HasSupportedWeaponContext(hasMeleeWeapon))
                {
                    roles.Remove(StageRole.Sniper);
                }
            }

            bool engineerTargetFound = false;
            foreach (PhysGrabObject valuable in
                     UnityEngine.Object.FindObjectsOfType<PhysGrabObject>(true))
            {
                if (valuable != null &&
                    EngineerEffectCatalog.IsEffectValuable(valuable))
                {
                    engineerTargetFound = true;
                    break;
                }
            }
            if (!engineerTargetFound)
            {
                roles.Remove(StageRole.Engineer);
            }

            bool rechargeableFound = false;
            foreach (ItemBattery battery in
                     UnityEngine.Object.FindObjectsOfType<ItemBattery>(true))
            {
                if (battery != null && !battery.isUnchargable)
                {
                    rechargeableFound = true;
                    break;
                }
            }
            if (!rechargeableFound)
            {
                roles.Remove(StageRole.Electrician);
            }
        }
        catch (Exception exception)
        {
            StageRolesPlugin.ModLogger.LogDebug(
                $"Context-dependent role availability check was skipped: {exception.Message}");
        }
    }

    private bool HasActiveImitatorCopyTarget()
    {
        foreach (RoleAssignment assignment in _assignments)
        {
            if (RoleCatalog.CanBeCopiedByImitator(
                    assignment.AssignedRole))
            {
                return true;
            }
        }
        return false;
    }

    private static bool HasSupportedWeaponContext(bool hasMeleeWeapon) =>
        hasMeleeWeapon ||
        HasNonValuableWeaponContext<ItemGun>();

    private static bool HasNonValuableWeaponContext<T>() where T : Component
    {
        foreach (T weapon in UnityEngine.Object.FindObjectsOfType<T>(true))
        {
            if (weapon == null ||
                weapon.GetComponentInParent<ValuableObject>() != null ||
                weapon.GetComponentInChildren<ValuableObject>(true) != null)
            {
                continue;
            }
            PhysGrabObject? physicalObject = weapon.GetComponentInParent<PhysGrabObject>();
            if (physicalObject != null &&
                physicalObject.GetComponentInChildren<ValuableObject>(true) != null)
            {
                continue;
            }
            return true;
        }
        return false;
    }

    private void PrepareAssignmentForActivePlayer(RoleAssignment assignment)
    {
        Vector3 position = assignment.Player.transform.position;
        assignment.PreviousPosition = position;
        assignment.StinkerPreviousPosition = position;
        assignment.TunaPreviousPosition = position;
        assignment.StinkerTravelDistance = 0f;
        assignment.StinkerTrailAnchor = position;
        assignment.TravelDistance = 0f;
        assignment.JoblessDamageTimer = 0f;
        assignment.TunaStationaryTimer = 0f;
        assignment.TunaDamageTimer = 0f;
        assignment.WasAlive = PlayerState.IsLiving(assignment.Player);
        assignment.PhoenixRevivePending = false;
        assignment.PhoenixReadyAt = 0f;
    }

    private void ActivateJoinedAssignment(RoleAssignment assignment)
    {
        StartInfluenza(assignment);
        PrepareAssignmentForActivePlayer(assignment);
        _medic.AddPlayer(assignment);
        _stinker.AddPlayer(assignment);
        _eventRoles.AddPlayer(assignment);
        if (RoleCatalog.HasCapability(assignment.Role, StageRole.King))
        {
            ApplyKingCrown(assignment.SteamId);
        }
    }

    private HashSet<string> ConnectedSteamIds(
        IReadOnlyDictionary<string, PlayerAvatar> playersBySteamId)
    {
        HashSet<string> result = new(StringComparer.Ordinal);
        if (SemiFunc.IsMultiplayer() && PhotonNetwork.InRoom)
        {
            HashSet<int> connectedActorNumbers = new();
            foreach (var networkPlayer in PhotonNetwork.PlayerList)
            {
                if (networkPlayer != null)
                {
                    connectedActorNumbers.Add(networkPlayer.ActorNumber);
                }
                if (!string.IsNullOrEmpty(networkPlayer?.UserId))
                {
                    result.Add(networkPlayer.UserId);
                }
            }

            foreach (KeyValuePair<string, PlayerAvatar> pair in playersBySteamId)
            {
                try
                {
                    int actorNumber = pair.Value.photonView?.Owner?.ActorNumber ?? 0;
                    if (actorNumber != 0 && connectedActorNumbers.Contains(actorNumber))
                    {
                        result.Add(pair.Key);
                    }
                }
                catch
                {
                    // A transient Photon view teardown is handled by two presence checks.
                }
            }
            foreach (RoleAssignment assignment in _assignments)
            {
                if (assignment.ActorNumber != 0 &&
                    connectedActorNumbers.Contains(assignment.ActorNumber))
                {
                    result.Add(assignment.SteamId);
                }
            }
            if (result.Count > 0)
            {
                return result;
            }
        }

        foreach (string steamId in playersBySteamId.Keys)
        {
            result.Add(steamId);
        }
        return result;
    }

    private void StageEndingInternal(
        bool restoreBaseUpgrades,
        bool deactivate)
    {
        CaptureRoleHistory();
        _stageReady = false;
        _assignmentsInitialized = false;
        _stageGeneration++;
        StopAllCoroutines();
        StopInfluenza();
        _notifier?.End();
        RoleHealingRuntime.Clear();
        _bomber?.Stop();
        _medic?.Stop();
        _mage?.Stop();
        _gambler?.Stop();
        _stinker?.Stop();
        _trickster?.Stop();
        _utilityRoles?.Stop();
        _eventRoles?.Stop();
        _diver?.Stop();
        _overhaul?.Stop();
        HunterBatteryRuntime.Clear();
        RestoreKingCrown();
        RoleAssignmentSync.ClearPreview();
        if (IsAuthority())
        {
            RoleAssignmentSync.Clear();
        }
        RoleAbilitySync.Clear();
        _nextAbilityPublishAt = 0;
        if (restoreBaseUpgrades && IsAuthority())
        {
            HashSet<string> steamIds = new(StringComparer.Ordinal);
            foreach (RoleAssignment assignment in _assignments)
            {
                steamIds.Add(assignment.SteamId);
            }
            foreach (RoleAssignment assignment in _departedAssignments.Values)
            {
                steamIds.Add(assignment.SteamId);
            }
            IReadOnlyList<UpgradeGrant> baseUpgrades = RoleCatalog.BaseUpgrades(_config);
            foreach (string steamId in steamIds)
            {
                UpgradeService.SetLevels(steamId, baseUpgrades);
            }
        }
        ClearAssignments();
        _rescueDeadSince.Clear();
        _rescueRevivePending.Clear();
        _rescueReviverByTarget.Clear();
        _roleQueryAllowedAt.Clear();
        _expressionInputAllowedAt.Clear();
        _consecutiveMissingPlayerChecks.Clear();
        _departedAssignments.Clear();
        _enemyLastAttacker.Clear();
        _grabbersByObjectId.Clear();
        _engineerSuppressedTrapIds.Clear();
        _engineerSuppressionGraceUntil.Clear();
        _levelGeneratorInstanceId = 0;
        _nextPlayerPresenceCheckAt = 0f;
        _nextExhaustionCheckAt = 0f;
        _phoenixFailureGraceUntil = 0f;
        _tunaStageGraceUntil = 0f;
        if (deactivate && gameObject.activeSelf)
        {
            gameObject.SetActive(false);
        }
    }

    private void CaptureRoleHistory()
    {
        if (_currentAssignmentStage <= 0 ||
            (_assignments.Count == 0 && _departedAssignments.Count == 0))
        {
            return;
        }

        foreach (RoleAssignment assignment in _assignments)
        {
            CaptureRoleHistory(assignment);
        }
        foreach (RoleAssignment assignment in _departedAssignments.Values)
        {
            CaptureRoleHistory(assignment);
        }
        _completedAssignmentStages = Math.Max(
            _completedAssignmentStages,
            _currentAssignmentStage);
        _currentAssignmentStage = 0;
    }

    private void CaptureRoleHistory(RoleAssignment assignment)
    {
        if (string.IsNullOrEmpty(assignment.SteamId))
        {
            return;
        }
        _previousRoles[assignment.SteamId] = assignment.AssignedRole;
        RecordRecentRole(
            _recentRolesByPlayer,
            assignment.SteamId,
            assignment.AssignedRole);
        if (assignment.AssignedRole == StageRole.Jobless ||
            assignment.AssignedRole == StageRole.Tuna ||
            assignment.AssignedRole == StageRole.Influenza ||
            assignment.AssignedRole == StageRole.Disaster)
        {
            _lastHardshipStages[assignment.SteamId] =
                _currentAssignmentStage;
        }
    }

    private Dictionary<string, List<StageRole>> CopyRecentRoleHistory()
    {
        Dictionary<string, List<StageRole>> copy =
            new(StringComparer.Ordinal);
        foreach (KeyValuePair<string, List<StageRole>> pair in
                 _recentRolesByPlayer)
        {
            copy[pair.Key] = new List<StageRole>(pair.Value);
        }
        return copy;
    }

    private static void RecordRecentRole(
        IDictionary<string, List<StageRole>> history,
        string steamId,
        StageRole role)
    {
        if (string.IsNullOrEmpty(steamId))
        {
            return;
        }
        if (!history.TryGetValue(steamId, out List<StageRole> roles))
        {
            roles = new List<StageRole>(RecentRoleHistoryLimit);
            history[steamId] = roles;
        }
        roles.Add(role);
        while (roles.Count > RecentRoleHistoryLimit)
        {
            roles.RemoveAt(0);
        }
    }

    private void ApplyKingCrown(string preferredSteamId = "")
    {
        if (!string.IsNullOrEmpty(preferredSteamId))
        {
            foreach (RoleAssignment preferred in _assignments)
            {
                if (RoleCatalog.HasCapability(
                        preferred.Role,
                        StageRole.King) &&
                    string.Equals(
                        preferred.SteamId,
                        preferredSteamId,
                        StringComparison.Ordinal))
                {
                    ApplyKingCrownTo(preferred);
                    return;
                }
            }
        }
        foreach (RoleAssignment assignment in _assignments)
        {
            if (!RoleCatalog.HasCapability(
                    assignment.Role,
                    StageRole.King) || PunManager.instance == null ||
                SessionManager.instance == null)
            {
                continue;
            }
            ApplyKingCrownTo(assignment);
            return;
        }
    }

    private void ApplyKingCrownTo(RoleAssignment assignment)
    {
        if (PunManager.instance == null || SessionManager.instance == null)
        {
            return;
        }
        try
        {
            _previousCrownedSteamId =
                SessionManager.instance.crownedPlayerSteamID ?? string.Empty;
            PunManager.instance.CrownPlayerSync(assignment.SteamId);
            _kingCrownApplied = true;
            StageRolesPlugin.ModLogger.LogInfo(
                $"King crown assigned to player {assignment.SteamId}.");
        }
        catch (Exception exception)
        {
            StageRolesPlugin.ModLogger.LogWarning(
                $"King crown assignment failed: {exception.Message}");
        }
    }

    private void RestoreKingCrown()
    {
        if (!_kingCrownApplied)
        {
            return;
        }
        try
        {
            if (PunManager.instance != null)
            {
                PunManager.instance.CrownPlayerSync(_previousCrownedSteamId);
            }
            else if (SessionManager.instance != null)
            {
                SessionManager.instance.crownedPlayerSteamID = _previousCrownedSteamId;
            }
        }
        catch (Exception exception)
        {
            StageRolesPlugin.ModLogger.LogWarning(
                $"King crown restoration failed: {exception.Message}");
        }
        _kingCrownApplied = false;
        _previousCrownedSteamId = string.Empty;
    }

    private static List<PlayerAvatar> CollectPlayers()
    {
        List<PlayerAvatar> result = new();
        if (GameDirector.instance == null)
        {
            return result;
        }
        foreach (PlayerAvatar player in GameDirector.instance.PlayerList)
        {
            if (player != null && player.gameObject.activeInHierarchy)
            {
                result.Add(player);
            }
        }
        return result;
    }

    private RoleAssignment? ResolveAssignment(string targetIdentifier)
    {
        if (string.IsNullOrWhiteSpace(targetIdentifier))
        {
            string localSteamId = PlayerIdentity.SteamId(SemiFunc.PlayerGetLocal());
            foreach (RoleAssignment assignment in _assignments)
            {
                if (string.Equals(
                        assignment.SteamId,
                        localSteamId,
                        StringComparison.Ordinal))
                {
                    return assignment;
                }
            }
            return null;
        }

        string target = targetIdentifier.Trim();
        // Keep full Steam IDs valid; otherwise a numeric target is the current
        // assignment-list number, not a numeric/partially matching player name.
        if (_assignmentsBySteamId.TryGetValue(target, out RoleAssignment bySteamId))
        {
            return bySteamId;
        }
        if (int.TryParse(target, out int playerNumber))
        {
            return playerNumber >= 1 && playerNumber <= _assignments.Count
                ? _assignments[playerNumber - 1]
                : null;
        }
        foreach (RoleAssignment assignment in _assignments)
        {
            if (string.Equals(assignment.SteamId, target, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(
                    PlayerIdentity.Name(assignment.Player),
                    target,
                    StringComparison.OrdinalIgnoreCase))
            {
                return assignment;
            }
        }

        RoleAssignment? partialMatch = null;
        foreach (RoleAssignment assignment in _assignments)
        {
            string playerName = PlayerIdentity.Name(assignment.Player);
            if (playerName.IndexOf(target, StringComparison.OrdinalIgnoreCase) < 0)
            {
                continue;
            }
            if (partialMatch != null)
            {
                return null;
            }
            partialMatch = assignment;
        }
        return partialMatch;
    }

    private RoleAssignment? FindAssignment(PlayerAvatar player)
    {
        if (player == null)
        {
            return null;
        }
        if (_assignmentsByPlayer.TryGetValue(player, out RoleAssignment byPlayer) &&
            ReferenceEquals(byPlayer.Player, player))
        {
            return byPlayer;
        }
        string steamId = PlayerIdentity.SteamId(player);
        return !string.IsNullOrEmpty(steamId) &&
            _assignmentsBySteamId.TryGetValue(steamId, out RoleAssignment byId)
                ? byId : null;
    }

    private void RegisterAssignment(RoleAssignment assignment)
    {
        _assignments.Add(assignment);
        IndexAssignment(assignment);
    }

    private void IndexAssignment(RoleAssignment assignment)
    {
        _assignmentsBySteamId[assignment.SteamId] = assignment;
        if (!ReferenceEquals(assignment.Player, null))
        {
            _assignmentsByPlayer[assignment.Player] = assignment;
        }
    }

    private void UnindexAssignment(RoleAssignment assignment)
    {
        _assignmentsBySteamId.Remove(assignment.SteamId);
        if (!ReferenceEquals(assignment.Player, null))
        {
            _assignmentsByPlayer.Remove(assignment.Player);
        }
    }

    private void ClearAssignments()
    {
        _assignments.Clear();
        _assignmentsBySteamId.Clear();
        _assignmentsByPlayer.Clear();
    }

    private static bool AllPlayerIdentitiesReady(IEnumerable<PlayerAvatar> players)
    {
        foreach (PlayerAvatar player in players)
        {
            if (string.IsNullOrEmpty(PlayerIdentity.SteamId(player)))
            {
                return false;
            }
        }
        return true;
    }

    private List<StageRole> EnabledRoles()
    {
        List<StageRole> roles = new();
        foreach (StageRole role in RoleCatalog.AllRoles)
        {
            if (RoleCatalog.IsSecretRole(role))
            {
                continue;
            }
            if (_config.RoleIsEnabled(role) && _config.RoleWeightValue(role) > 0)
            {
                roles.Add(role);
            }
        }
        if (roles.Count > 0 && _config.RoleIsEnabled(StageRole.Superbot))
        {
            roles.Add(StageRole.Superbot);
        }
        if (roles.Exists(role => !RoleCatalog.IsSecretRole(role)) &&
            _config.RoleIsEnabled(StageRole.Disaster))
        {
            roles.Add(StageRole.Disaster);
        }
        return roles;
    }

    private static bool IsAuthority()
    {
        try
        {
            return GameManager.instance != null &&
                   SemiFunc.IsMasterClientOrSingleplayer();
        }
        catch
        {
            return false;
        }
    }

    private static bool IsPlayableStageContext()
    {
        try
        {
            return SemiFunc.RunIsLevel() && !SemiFunc.RunIsLobby() &&
                   !SemiFunc.RunIsShop() && !SemiFunc.RunIsArena() &&
                   !SemiFunc.RunIsTutorial();
        }
        catch
        {
            return false;
        }
    }
}
