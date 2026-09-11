using REPOJP.StageRoles;
using UnityEngine;

int checks = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new Exception(name);
    checks++;
}
Check(RoleUpgradeScaling.MaximumUpgradeLevel == 200, "Upgrade setting limit is 200");
foreach (var definition in RoleUpgradeScaling.Definitions)
{
    int maximum = definition.Name == "MapPlayerCount" ? 1 : 200;
    Check(definition.MaximumLevel == maximum, definition.Name + " maximum");
    Check(RoleUpgradeScaling.TryParse("1:" + maximum, 30, maximum, out var exact, out _) &&
        exact.Count == 1 && exact[0].Level == maximum, definition.Name + " accepts boundary");
    Check(RoleUpgradeScaling.TryParse("1:9999,999999:9999", 1, 999999, maximum, true,
        out var clamped, out _) && clamped.Count == 2 && clamped.All(rule => rule.Level == maximum) &&
        clamped[1].Condition == 999999, definition.Name + " base clamps without changing run limit");
    Check(RoleUpgradeScaling.TryParse("0:-1,1000000:201", 1, 999999, maximum, true,
        out var bounds, out _) && bounds[0].Condition == 1 && bounds[0].Level == 0 &&
        bounds[1].Condition == 999999 && bounds[1].Level == maximum,
        definition.Name + " base lower and upper boundaries");
}
var budget = new PendingDamageBudget();
Check(budget.Available("guard", 30) == 29, "Leave one HP");
var first = budget.Add("guard", 30, 20);
Check(budget.Available("guard", 30) == 9, "Pending first transfer is reserved");
var second = budget.Add("guard", 30, 9);
Check(budget.Available("guard", 30) == 0, "Concurrent transfers cannot exceed 29 HP");
Check(!budget.Observe("guard", 30, 25), "Unrelated damage does not acknowledge a transfer");
Check(budget.Reserved("guard") == 29, "Unconfirmed reservations are retained");
Check(budget.Observe("guard", 30, 10), "First acknowledgement");
Check(budget.Available("guard", 10) == 0, "Intermediate HP still accounts for second transfer");
Check(budget.Observe("guard", 10, 1), "Second acknowledgement");
Check(budget.Reserved("guard") == 0 && budget.Available("guard", 1) == 0, "No transfer at HP one");
budget.Add("guard", 100, 25);
budget.Add("other", 50, 10);
budget.Clear("guard");
Check(budget.Reserved("guard") == 0 && budget.Reserved("other") == 10, "Player cleanup");
budget.Clear();
Check(budget.Reserved("other") == 0, "Stage cleanup");
budget.Remove(first);
budget.Remove(second);
Check(budget.Reserved("guard") == 0, "Duplicate acknowledgements are harmless");

var player = new PlayerAvatar();
player.photonView.IsMine = true;
int restored = -1;
Check(RoleHealingRuntime.TryHeal(player, 5, value => restored = value), "Local request");
Check(restored == 5 && player.playerHealth.Health == 55, "Actual local healing");
player.playerHealth.Health = 98;
Check(RoleHealingRuntime.TryHeal(player, 10, value => restored = value), "Clamped request");
Check(restored == 2, "Only missing HP is charged");
Check(!RoleHealingRuntime.TryHeal(player, 1, _ => throw new Exception()), "Full HP skips request");
player.playerHealth.Health = 50;
player.playerHealth.OnHeal = _ => { };
Check(RoleHealingRuntime.TryHeal(player, 10, value => restored = value), "Rejected local heal is observed");
Check(restored == 0, "Rejected local heal consumes zero");
player.playerHealth.OnHeal = _ => { player.playerHealth.Health += 3; throw new Exception("partial result"); };
try { RoleHealingRuntime.TryHeal(player, 10, value => restored = value); }
catch (Exception) { }
Check(restored == 3, "Partial result survives an exception");

SemiFunc.Multiplayer = true;
player.photonView.IsMine = false;
player.playerHealth.OnHeal = _ => { };
player.playerHealth.Health = 50;
Time.unscaledTime = 0;
restored = -1;
Check(RoleHealingRuntime.TryHeal(player, 10, value => restored = value), "Remote request");
Check(!RoleHealingRuntime.TryHeal(player, 5, _ => throw new Exception()), "Shared recipient serialization");
Check(restored == -1, "Send is not immediate confirmation");
RoleHealingRuntime.Observe(player, 50, 45);
Check(restored == -1, "Damage does not confirm healing");
RoleHealingRuntime.Observe(player, 95, 100);
Check(restored == 10, "Ambiguous partial remote increase cannot refund pending healing and exceed cap");
restored = -1;
Check(RoleHealingRuntime.TryHeal(player, 5, value => restored = value), "Next request after acknowledgement");
Time.unscaledTime = 6;
Check(RoleHealingRuntime.TryHeal(player, 5, _ => { }), "Timeout does not deadlock");
Check(restored == -1, "Timeout does not refund a possibly delivered request");
RoleHealingRuntime.Clear();
Check(RoleHealingRuntime.TryHeal(player, 5, value => restored = value), "New stage request");
RoleHealingRuntime.Clear();
RoleHealingRuntime.Observe(player, 50, 55);
Check(restored == -1, "Previous-stage callback cannot change new budget");
Check(RoleHealingRuntime.TryHeal(player, 5, value => restored = value), "Request before death");
RoleHealingRuntime.Forget(player);
RoleHealingRuntime.Observe(player, 0, 25);
Check(restored == -1, "Revival is not attributed to pending heal");
var notices = new AbilityExhaustionState();
Check(!notices.TryQueue(1, false), "No exhaustion notice before depletion");
Check(notices.TryQueue(1, true), "First depletion queues notice");
Check(!notices.TryQueue(1, true), "No duplicates while queued");
notices.Finished(1);
Check(notices.TryQueue(1, true), "Cancelled or failed notice can retry");
notices.Sent(1);
notices.Finished(1);
Check(!notices.TryQueue(1, true), "Delivered depletion only announced once");
Check(notices.TryQueue(2, true), "Other ability has independent notification state");
notices.Finished(2);
Check(!notices.TryQueue(1, false), "Restored allowance re-arms silently");
Check(notices.TryQueue(1, true), "Next depletion is announced again");
notices.Sent(1);
notices.Finished(1);
notices.Rearm(1);
Check(notices.TryQueue(1, true), "Extraction re-arms even if consumed before next tick");
notices.Sent(1);
notices.Finished(1);
notices.Rearm();
Check(notices.TryQueue(1, true), "Role reassignment re-arms notices");
Check(MechanicRepairBudget.RepairValue(5700f, 1000f, 0.001f, 0.001f) == 1f, "Final sub-dollar tail repairs one dollar");
Check(MechanicRepairBudget.RepairValue(5700f, 1000f, 0.001f, 25f) == 0f, "Ordinary sub-dollar tick still accumulates");
Check(MechanicRepairBudget.RepairValue(100f, 50f, 1f, 1f) == 1f, "Exact final dollar is unchanged");
Check(MechanicRepairBudget.RepairValue(100f, 50f, 1.25f, 1.25f) == 2f, "Final whole amount includes fractional tail");
Check(MechanicRepairBudget.RepairValue(100f, 50f, 1.25f, 25f) == 1f, "Ordinary ticks do not round up");
Check(MechanicRepairBudget.RepairValue(5700f, 5700f, 1f, 1f) == 0f, "Full item receives no repair");
Check(MechanicRepairBudget.RepairValue(5700f, 1000f, 0f, 0f) == 0f, "Spent budget receives no extra repair");
Check(MechanicRepairBudget.RepairValue(0f, 0f, 1f, 1f) == 0f, "Zero-value item receives no repair");
Check(MechanicRepairBudget.RepairValue(100f, 99f, 50f, 50f) == 1f, "Missing value limits final repair");
Check(MechanicRepairBudget.RepairValue(100f, 50f, 50f, 1.25f) == 2f, "Pending accumulation cannot exceed remaining budget");
Check(MechanicRepairBudget.RepairValue(float.NaN, 50f, 1f, 1f) == 0f, "Invalid value rejected");
Check(TruckDrawWeight.Resolve(10, 0, 1, 0.25f) == 10f, "Uncapped map count keeps full weight");
Check(TruckDrawWeight.Resolve(10, 1, 1, 0.25f) == 2.5f, "Capped map count uses quarter weight");
Check(TruckDrawWeight.Resolve(1, 9999, 9999, 0.25f) == 0.25f, "Small weights retain fractional probability");
Check(TruckDrawWeight.Resolve(10, 100, 50, 0.25f) == 2.5f, "Configured level above draw cap also reduced");
Check(TruckDrawWeight.Resolve(10, 0, 0, 0.25f) == 2.5f, "Zero cap is supported");
Check(TruckDrawWeight.Resolve(10, 1, 1, 0f) == 0f, "Zero multiplier excludes capped candidate");
Check(TruckDrawWeight.Resolve(10, 0, 1, 0f) == 10f, "Zero multiplier keeps uncapped candidates");
Check(TruckDrawWeight.Resolve(10, 1, 1, 1f) == 10f, "One restores original weight");
Check(TruckDrawWeight.Resolve(0, 1, 1, 0.25f) == 0f, "Disabled upgrade stays disabled");
Check(TruckDrawWeight.Resolve(10, 1, 1, float.NaN) == 2.5f, "Nonfinite setting uses safe default");
Check(TruckDrawWeight.Resolve(10, 1, 1, float.PositiveInfinity) == 2.5f, "Infinity uses safe default");
Check(TruckDrawWeight.Resolve(10, 1, 1, -1f) == 0f, "Negative multiplier clamped");
Check(TruckDrawWeight.Resolve(10, 1, 1, 2f) == 10f, "Excessive multiplier clamped");
foreach (var pair in new[] { ("Health", 2), ("Stamina", 4), ("ExtraJump", 1), ("Speed", 3), ("Strength", 2), ("Range", 3), ("Launch", 1), ("TumbleClimb", 1), ("TumbleWings", 1), ("CrouchRest", 3), ("MapPlayerCount", 1), ("DeathHeadBattery", 1) })
{
    Check(TruckDrawWeight.VanillaShopMaximum(pair.Item1) == pair.Item2, pair.Item1 + " vanilla shop count");
    Check(TruckDrawWeight.DefaultWeight(pair.Item1) == 10 * pair.Item2, pair.Item1 + " vanilla-based default weight");
    Check(TruckDrawWeight.Resolve(7, 0, 1, 0.25f) == 7f, pair.Item1 + " configured weight is used directly");
    Check(TruckDrawWeight.Resolve(7, 1, 1, 0.25f) == 1.75f, pair.Item1 + " only cap correction applies");
}
foreach (int exponent in Enumerable.Range(1, 6))
{
    float previous = 1f;
    for (int level = 0; level <= 210; level++)
    {
        float actual = TruckDrawWeight.Resolve(1, level, 200, 0.25f, exponent);
        double expected = 1d - 0.75d * Math.Pow(Math.Min(1d, level / 200d), exponent);
        Check(Math.Abs(actual - expected) < 0.000001, "Curve matches proposed formula");
        Check(actual <= previous && actual >= 0.25f, "Curve monotonic and bounded");
        previous = actual;
    }
}
Check(TruckDrawWeight.Resolve(1, 100, 200, 0.25f) == 0.8125f, "Default curve is 2");
Check(TruckDrawWeight.Resolve(1, -1, 200, 0.25f) == 1f, "Negative level clamps to zero");
foreach (float invalid in new[] { float.NaN, float.NegativeInfinity, float.PositiveInfinity })
    Check(TruckDrawWeight.Resolve(1, 100, 200, 0.25f, invalid) == 0.8125f, "Invalid curve uses default");
Check(TruckDrawWeight.Resolve(1, 100, 200, 0.25f, 0f) ==
    TruckDrawWeight.Resolve(1, 100, 200, 0.25f, 0.1f), "Curve lower bound");
Check(TruckDrawWeight.Resolve(1, 100, 200, 0.25f, 20f) ==
    TruckDrawWeight.Resolve(1, 100, 200, 0.25f, 10f), "Curve upper bound");
Check(TruckDrawWeight.Resolve(1, 199, 200, 0f) > 0f, "Zero minimum preserves pre-cap eligibility");
Check(TruckDrawWeight.Resolve(1, 100, 200, 1f) == 1f, "Minimum one disables falloff");
Console.WriteLine($"PASS: {checks} budget, exhaustion-notification, and truck-weight regression checks.");
Console.WriteLine("Production budget/recovery sources with deterministic stand-ins; no Unity or live transport simulation.");
