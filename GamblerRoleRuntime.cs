using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Photon.Pun;
using UnityEngine;

namespace REPOJP.StageRoles;

internal sealed class GamblerRoleRuntime
{
    internal sealed class GambitEffectContext
    {
        internal GambitEffectContext(
            RoleAssignment assignment,
            PlayerAvatar target,
            EnemySpinny.Colors color)
        {
            Assignment = assignment;
            Target = target;
            Color = color;
        }

        internal RoleAssignment Assignment { get; }
        internal PlayerAvatar Target { get; }
        internal EnemySpinny.Colors Color { get; }
    }

    private static readonly FieldInfo? DollarValueCurrentField =
        AccessTools.Field(typeof(ValuableObject), "dollarValueCurrent");
    private static readonly FieldInfo? DollarValueSetField =
        AccessTools.Field(typeof(ValuableObject), "dollarValueSet");
    private static readonly FieldInfo? ExtractionPointsCompletedField =
        AccessTools.Field(typeof(RoundDirector), "extractionPointsCompleted");
    private readonly StageRolesConfig _config;
    private readonly HashSet<string> _wageredValuables = new(StringComparer.Ordinal);
    private readonly HashSet<int> _processedGambitEffects = new();
    private readonly Dictionary<int, PlayerAvatar> _gambitTargets = new();
    private bool _active;
    private int _lastDirectorId;
    private int _lastCompletionCount = -1;

    internal GamblerRoleRuntime(StageRolesConfig config)
    {
        _config = config;
    }

    internal void Begin()
    {
        _wageredValuables.Clear();
        _processedGambitEffects.Clear();
        _gambitTargets.Clear();
        _lastDirectorId = 0;
        _lastCompletionCount = -1;
        _active = true;
    }

    internal void Stop()
    {
        _active = false;
        _wageredValuables.Clear();
        _processedGambitEffects.Clear();
        _gambitTargets.Clear();
        _lastDirectorId = 0;
        _lastCompletionCount = -1;
    }

    internal void Tick(RoleAssignment assignment, RoleNotifier notifier)
    {
        if (!_active || !PlayerState.IsLiving(assignment.Player) ||
            assignment.GamblerWagersUsed > 0 || ValuableDirector.instance == null)
        {
            return;
        }

        foreach (ValuableObject valuable in ValuableDirector.instance.valuableList)
        {
            if (valuable == null || !ValueIsReady(valuable) ||
                !IsGrabbedBy(valuable, assignment))
            {
                continue;
            }

            string valuableId = ValuableId(valuable);
            if (_wageredValuables.Contains(valuableId))
            {
                continue;
            }

            if (TryApplyWager(valuable, assignment, valuableId, notifier))
            {
                return;
            }
        }
    }

    internal void ExtractionCompleted(
        RoundDirector? director,
        IReadOnlyList<RoleAssignment> assignments,
        RoleNotifier notifier)
    {
        if (!_active || director == null)
        {
            return;
        }

        int directorId = director.GetInstanceID();
        int completionCount = ReadExtractionCompletionCount(director);
        if (directorId == _lastDirectorId && completionCount == _lastCompletionCount)
        {
            return;
        }
        _lastDirectorId = directorId;
        _lastCompletionCount = completionCount;

        int restoredPlayers = 0;
        foreach (RoleAssignment assignment in assignments)
        {
            if (!RoleCatalog.HasCapability(
                    assignment.Role,
                    StageRole.Gambler) ||
                assignment.GamblerWagersUsed <= 0)
            {
                continue;
            }

            assignment.GamblerWagersUsed = 0;
            notifier.Notify(assignment.Player, "GamblerReady");
            restoredPlayers++;
        }

        StageRolesPlugin.ModLogger.LogInfo(
            $"Extraction #{completionCount} restored the Gambler effect for " +
            $"{restoredPlayers} player(s).");
    }

    internal void ObserveGambitState(EnemySpinny gambit)
    {
        if (!_active || gambit == null || gambit.RouletteGoingOn())
        {
            return;
        }
        int gambitId = gambit.GetInstanceID();
        _processedGambitEffects.Remove(gambitId);
        _gambitTargets.Remove(gambitId);
    }

    internal void CaptureGambitTarget(EnemySpinny gambit)
    {
        if (!_active || gambit == null || gambit.playerTarget == null)
        {
            return;
        }
        _gambitTargets[gambit.GetInstanceID()] = gambit.playerTarget;
    }

    internal GambitEffectContext? BeginGambitEffect(
        EnemySpinny gambit,
        IReadOnlyList<RoleAssignment> assignments)
    {
        if (!_active || gambit == null ||
            gambit.currentState != EnemySpinny.State.RouletteEffect)
        {
            return null;
        }

        int gambitId = gambit.GetInstanceID();
        PlayerAvatar? target = _gambitTargets.TryGetValue(
            gambitId,
            out PlayerAvatar capturedTarget)
                ? capturedTarget
                : gambit.playerTarget;
        if (target == null)
        {
            return null;
        }
        if (!_processedGambitEffects.Add(gambitId))
        {
            return null;
        }

        RoleAssignment? assignment = FindAssignment(target, assignments);
        if (assignment == null ||
            !RoleCatalog.HasCapability(
                assignment.Role,
                StageRole.Gambler))
        {
            return null;
        }

        EnemySpinny.Colors color =
            gambit.GetCurrentColorbyAngle(gambit.targetAngleDegrees);
        if (color != EnemySpinny.Colors.Green &&
            color != EnemySpinny.Colors.Red &&
            color != EnemySpinny.Colors.Black &&
            color != EnemySpinny.Colors.White)
        {
            return null;
        }
        return new GambitEffectContext(
            assignment,
            target,
            color);
    }

    internal void CompleteGambitEffect(GambitEffectContext context)
    {
        if (!_active || context == null)
        {
            return;
        }

        RoleAssignment assignment = context.Assignment;
        EnemySpinny.Colors color = context.Color;
        PlayerAvatar target = context.Target;
        try
        {
            switch (color)
            {
                case EnemySpinny.Colors.Green:
                    int extraHeal = Math.Max(
                        0,
                        _config.GamblerGambitGreenHealAmount.Value - 25);
                    if (extraHeal > 0)
                    {
                        target.playerHealth?.HealOther(extraHeal, true);
                    }
                    break;
                case EnemySpinny.Colors.Red:
                    int extraDamage = Math.Max(
                        0,
                        _config.GamblerGambitRedDamage.Value - 50);
                    if (extraDamage > 0)
                    {
                        target.playerHealth?.HurtOther(
                            extraDamage,
                            Vector3.zero,
                            false);
                    }
                    break;
                case EnemySpinny.Colors.Black:
                    target.PlayerDeath(-1);
                    break;
                case EnemySpinny.Colors.White:
                    UpgradeService.AddLevels(
                        assignment.SteamId,
                        "Health",
                        Math.Max(
                            0,
                            _config.GamblerGambitWhiteHealthUpgradeLevels.Value));
                    break;
            }

            StageRolesPlugin.ModLogger.LogInfo(
                $"Gambit {color} effect adjusted for Gambler {assignment.SteamId}.");
        }
        catch (Exception exception)
        {
            StageRolesPlugin.ModLogger.LogWarning(
                $"Could not adjust Gambit {color} for Gambler " +
                $"{assignment.SteamId}: {exception.Message}");
        }
    }

    private bool TryApplyWager(
        ValuableObject valuable,
        RoleAssignment assignment,
        string valuableId,
        RoleNotifier notifier)
    {
        float currentValue = ReadFloat(DollarValueCurrentField, valuable);
        if (currentValue < 0f || float.IsNaN(currentValue) || float.IsInfinity(currentValue))
        {
            return false;
        }

        bool won = UnityEngine.Random.Range(0, 100) <
            Mathf.Clamp(_config.GamblerWinChancePercent.Value, 0, 100);
        float newValue = won
            ? Mathf.Max(
                0f,
                currentValue * Mathf.Clamp(
                    _config.GamblerWinValueMultiplier.Value,
                    0f,
                    10f))
            : 0f;

        try
        {
            if (won)
            {
                if (!TrySetValue(valuable, newValue))
                {
                    return false;
                }
            }
            else if (!TryDestroyValuable(valuable))
            {
                return false;
            }

            _wageredValuables.Add(valuableId);
            assignment.GamblerWagersUsed = 1;
            notifier.Notify(assignment.Player, won ? "Jackpot!" : "Bust!");
            StageRolesPlugin.ModLogger.LogInfo(
                $"Gambler effect used by player {assignment.SteamId}: " +
                (won
                    ? $"{currentValue:0} -> {newValue:0} (doubled)."
                    : $"{currentValue:0} -> destroyed."));
            return true;
        }
        catch (Exception exception)
        {
            StageRolesPlugin.ModLogger.LogWarning(
                $"Gambler effect failed for player {assignment.SteamId}: " +
                exception.Message);
            return false;
        }
    }

    private static bool TrySetValue(ValuableObject valuable, float newValue)
    {
        if (SemiFunc.IsMultiplayer())
        {
            PhotonView? view = valuable.GetComponent<PhotonView>() ??
                               valuable.GetComponentInParent<PhotonView>();
            if (view == null || view.ViewID == 0)
            {
                return false;
            }
            view.RPC(
                nameof(ValuableObject.DollarValueSetRPC),
                RpcTarget.All,
                new object[] { newValue });
            return true;
        }

        valuable.DollarValueSetRPC(newValue);
        return true;
    }

    private static bool TryDestroyValuable(ValuableObject valuable)
    {
        PhysGrabObjectImpactDetector? detector =
            valuable.GetComponent<PhysGrabObjectImpactDetector>() ??
            valuable.GetComponentInParent<PhysGrabObjectImpactDetector>() ??
            valuable.GetComponentInChildren<PhysGrabObjectImpactDetector>(true);
        if (detector == null)
        {
            return false;
        }

        if (SemiFunc.IsMultiplayer())
        {
            PhotonView? view = detector.GetComponent<PhotonView>() ??
                               detector.GetComponentInParent<PhotonView>();
            if (view == null || view.ViewID == 0)
            {
                return false;
            }
            view.RPC(
                nameof(PhysGrabObjectImpactDetector.DestroyObjectRPC),
                RpcTarget.AllViaServer,
                new object[] { true });
            return true;
        }

        detector.DestroyObject(effects: true);
        return true;
    }

    private static bool IsGrabbedBy(
        ValuableObject valuable,
        RoleAssignment assignment)
    {
        PhysGrabObject? grabObject = valuable.GetComponent<PhysGrabObject>() ??
                                     valuable.GetComponentInParent<PhysGrabObject>();
        if (grabObject?.playerGrabbing == null)
        {
            return false;
        }
        foreach (PhysGrabber grabber in grabObject.playerGrabbing)
        {
            PlayerAvatar? player = grabber?.playerAvatar;
            if (player == null)
            {
                continue;
            }
            if (ReferenceEquals(player, assignment.Player) ||
                string.Equals(
                    PlayerIdentity.SteamId(player),
                    assignment.SteamId,
                    StringComparison.Ordinal))
            {
                return true;
            }
        }
        return false;
    }

    private static RoleAssignment? FindAssignment(
        PlayerAvatar player,
        IReadOnlyList<RoleAssignment> assignments)
    {
        string steamId = PlayerIdentity.SteamId(player);
        foreach (RoleAssignment assignment in assignments)
        {
            if (ReferenceEquals(assignment.Player, player) ||
                (!string.IsNullOrEmpty(steamId) &&
                 string.Equals(
                     assignment.SteamId,
                     steamId,
                     StringComparison.Ordinal)))
            {
                return assignment;
            }
        }
        return null;
    }

    private static string ValuableId(ValuableObject valuable)
    {
        PhotonView? view = valuable.GetComponent<PhotonView>() ??
                           valuable.GetComponentInParent<PhotonView>();
        return view != null && view.ViewID != 0
            ? $"view:{view.ViewID}"
            : $"object:{valuable.GetInstanceID()}";
    }

    private static bool ValueIsReady(ValuableObject valuable) =>
        DollarValueSetField?.GetValue(valuable) is bool value && value;

    private static float ReadFloat(FieldInfo? field, object instance) =>
        field?.GetValue(instance) is float value ? value : -1f;

    private int ReadExtractionCompletionCount(RoundDirector director)
    {
        try
        {
            if (ExtractionPointsCompletedField?.GetValue(director) is int count)
            {
                return count;
            }
        }
        catch (Exception exception)
        {
            StageRolesPlugin.ModLogger.LogDebug(
                $"Gambler could not read the extraction count: {exception.Message}");
        }
        return _lastCompletionCount + 1;
    }

}
