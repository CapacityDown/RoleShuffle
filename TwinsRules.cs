using System;
using System.Collections.Generic;

namespace REPOJP.StageRoles;

// Engine-independent state. A pair owns one instance for the entire stage.
internal sealed class TwinsCooperation
{
    private readonly HashSet<int> _paid = new();
    private readonly HashSet<int> _qualified = new();
    private int _carriedId;
    private float _carryStarted = -1f;
    private float _restStarted = -1f;
    private readonly bool[] _reuniting = new bool[2];

    internal int DeliveryBonusUsed { get; private set; }
    internal float RestReadyAt { get; private set; }

    internal void ObserveCarry(int id, bool together, bool inDelivery, float now, float requiredSeconds)
    {
        if (!together || inDelivery || id == 0)
        {
            _carriedId = 0;
            _carryStarted = -1f;
            return;
        }
        if (_carriedId != id || _carryStarted < 0f)
        {
            _carriedId = id;
            _carryStarted = now;
        }
        if (now - _carryStarted >= requiredSeconds) _qualified.Add(id);
    }

    internal int Deliver(int id, bool inDelivery, bool bothNearby, float currentValue, float percent, int stageLimit)
    {
        if (!inDelivery || !bothNearby || !_qualified.Contains(id) || _paid.Contains(id) ||
            currentValue <= 0 || percent <= 0 || DeliveryBonusUsed >= stageLimit) return 0;
        // Mark before dispatching any native RPC. Leaving and reentering cannot pay twice.
        _paid.Add(id);
        int bonus = (int)Math.Min(stageLimit - (long)DeliveryBonusUsed,
            Math.Floor(currentValue * (double)percent / 100d));
        DeliveryBonusUsed += Math.Max(0, bonus);
        return Math.Max(0, bonus);
    }

    internal bool Rest(bool conditionsMet, bool damaged, float now, float holdSeconds, float cooldown)
    {
        if (!conditionsMet || damaged || now < RestReadyAt)
        {
            _restStarted = -1f;
            return false;
        }
        if (_restStarted < 0f) _restStarted = now;
        if (now - _restStarted < holdSeconds) return false;
        _restStarted = -1f;
        RestReadyAt = now + cooldown;
        return true;
    }

    internal void InterruptRest() => _restStarted = -1f;

    internal bool Rendezvous(int member, float distance, bool runningTowardPartner, float startRange, float endRange)
    {
        if (distance >= Math.Max(startRange, endRange)) _reuniting[member] = true;
        if (distance <= Math.Min(startRange, endRange)) _reuniting[member] = false;
        return _reuniting[member] && runningTowardPartner;
    }
}
