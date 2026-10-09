using System;

namespace REPOJP.StageRoles;

internal sealed class BrawlerCombo
{
    private int _target, _hits, _frame = -1;
    private float _lastHit;

    internal float Multiplier(int target, float now, int frame, float baseline, float step, float maximum, float timeout)
    {
        int hits = target == _target && now >= _lastHit && now - _lastHit < timeout ? _hits : 0;
        // Multiple colliders reported in one frame belong to the same combo step.
        if (frame == _frame && hits > 0) hits--;
        baseline = Math.Clamp(baseline, 0f, 10f);
        return Math.Min(Math.Max(baseline, Math.Clamp(maximum, 0f, 10f)),
            baseline + hits * Math.Clamp(step, 0f, 5f));
    }

    internal void Confirm(int target, float now, int frame, float timeout)
    {
        if (target == _target && frame == _frame) return;
        _hits = target == _target && now >= _lastHit && now - _lastHit < timeout ? Math.Min(int.MaxValue - 1, _hits) + 1 : 1;
        _target = target;
        _lastHit = now;
        _frame = frame;
    }

    internal void Reset() { _target = _hits = 0; _frame = -1; _lastHit = 0; }
}

internal static class CombatRoleRules
{
    internal static float AvengerMultiplier(float now, float deathUntil, float hitUntil, float deathMultiplier, float hitMultiplier) =>
        Math.Max(now < deathUntil ? Math.Clamp(deathMultiplier, 1f, 10f) : 1f,
            now < hitUntil ? Math.Clamp(hitMultiplier, 1f, 10f) : 1f);

    internal static int HunterExtraOrbs(int normal, int kills, int interval, bool jackpot, int jackpotCount, bool doubleDrop)
    {
        int guaranteed = interval > 0 && kills > 0 && kills % interval == 0 ? 1 : 0;
        int randomExtra = jackpot ? Math.Max(0, jackpotCount - normal) : doubleDrop ? normal : 0;
        // The guarantee is a minimum reward, not another jackpot multiplier.
        return Math.Max(guaranteed, randomExtra);
    }
}
