using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace REPOJP.StageRoles;

internal static class BaseUpgradeBonusStore
{
    private const string KeyPrefix = "RoleShuffle.BaseUpgradeBonus.";

    internal static int Get(string dictionaryName)
    {
        if (StatsManager.instance == null ||
            string.IsNullOrEmpty(dictionaryName))
        {
            return 0;
        }
        return StatsManager.instance.runStats.GetValueOrDefault(
            KeyPrefix + dictionaryName,
            0);
    }

    internal static bool ApplyEffectiveDelta(
        string dictionaryName,
        int currentLevel,
        int configuredLevel,
        int delta,
        int maximumLevel)
    {
        if (StatsManager.instance == null ||
            string.IsNullOrEmpty(dictionaryName) ||
            delta == 0)
        {
            return false;
        }

        int desiredLevel = Math.Max(
            0,
            Math.Min(maximumLevel, currentLevel + delta));
        if (desiredLevel == currentLevel)
        {
            return false;
        }
        string key = KeyPrefix + dictionaryName;
        StatsManager.instance.runStats[key] = desiredLevel - configuredLevel;
        return true;
    }
}

internal sealed class BaseUpgradeDrawRuntime : MonoBehaviour
{
    private const string DrawVersionKey = "RS.BUD.Version";
    private const string DrawIdKey = "RS.BUD.Id";
    private const string DrawUpgradeKey = "RS.BUD.Upgrade";
    private const string DrawDeltaKey = "RS.BUD.Delta";
    private const string DrawStartTimestampKey = "RS.BUD.Start";
    private const int DrawStateFormatVersion = 1;
    private const int DrawNetworkLeadMilliseconds = 250;
    private const string PendingDrawKey =
        "RoleShuffle.BaseUpgradeDraw.Pending";
    private const float StartDelaySeconds = 0.75f;
    private const float UpgradeSpinDurationSeconds = 2.2f;
    private const float TotalSpinDurationSeconds = 3.2f;
    private const float SpinStepSeconds = 0.075f;
    private const float SlotReelTravel = 118f;
    private const float ResultDurationSeconds = 4f;
    private const int DefaultMaximumLevel = RoleUpgradeScaling.MaximumUpgradeLevel;
    private const int MaximumConfiguredDelta = RoleUpgradeScaling.MaximumUpgradeLevel;
    private const string DefaultDeltaWeights = "-1:10,0:15,1:60,2:15";

    private static readonly Vector2 PanelSize = new(860f, 208f);
    private static readonly Color RepoOrange =
        new(1f, 0.38f, 0.015f, 1f);
    private static readonly Color RepoOrangeDim =
        new(0.72f, 0.18f, 0.01f, 0.8f);
    private static readonly UpgradeGrant AllUpgrades =
        new("AllUpgrades", string.Empty, 0);
    private static readonly string[] FallbackDisplayNames =
    {
        "HEALTH",
        "STAMINA",
        "EXTRA JUMP",
        "SPEED",
        "STRENGTH",
        "RANGE",
        "LAUNCH",
        "TUMBLE CLIMB",
        "TUMBLE WINGS",
        "CROUCH REST",
        "MAP PLAYER COUNT",
        "DEATH HEAD BATTERY",
        "ALL UPGRADES"
    };
    private readonly List<UpgradeGrant> _allBaseUpgrades = new();
    private readonly List<UpgradeGrant> _individualCandidates = new();
    private readonly List<UpgradeGrant> _displayCandidates = new();
    private readonly List<WeightedDelta> _deltaRules = new();
    private readonly List<UpgradeGrant> _selected = new();
    private StageRolesConfig _config = null!;
    private RoleNotifier _notifier = null!;
    private bool _pendingTruckDraw;
    private int _generation;
    private bool _drawRunning;
    private GameObject? _canvasObject;
    private GameObject? _panelObject;
    private TextMeshProUGUI? _titleText;
    private TextMeshProUGUI? _upgradeSlotText;
    private TextMeshProUGUI? _upgradeSlotNextText;
    private TextMeshProUGUI? _deltaSlotText;
    private TextMeshProUGUI? _deltaSlotNextText;
    private int _selectedDelta;
    private int _remoteGeneration;
    private int _networkDrawSequence;
    private int _lastRemoteDrawId;
    private float _nextRemoteStateCheckAt;
    private bool _remoteRoomActive;

    private readonly struct AppliedUpgrade
    {
        internal AppliedUpgrade(UpgradeGrant upgrade, int delta)
        {
            Upgrade = upgrade;
            Delta = delta;
        }

        internal UpgradeGrant Upgrade { get; }
        internal int Delta { get; }
    }

    private readonly struct WeightedDelta
    {
        internal WeightedDelta(int delta, int weight)
        {
            Delta = delta;
            Weight = weight;
        }

        internal int Delta { get; }
        internal int Weight { get; }
    }

    internal void Initialize(StageRolesConfig config)
    {
        _config = config;
        _notifier = new RoleNotifier(this, config);
    }

    internal void QueueForTruck()
    {
        if (!_config.Enabled.Value ||
            !_config.TruckUpgradeDrawEnabled.Value ||
            !SemiFunc.IsMasterClientOrSingleplayer())
        {
            _pendingTruckDraw = false;
            return;
        }
        _pendingTruckDraw = true;
    }

    internal void PrepareForTruckTransition()
    {
        if (!_config.Enabled.Value ||
            !_config.TruckUpgradeDrawEnabled.Value ||
            !SemiFunc.IsMasterClientOrSingleplayer() ||
            StatsManager.instance == null)
        {
            return;
        }
        StatsManager.instance.runStats[PendingDrawKey] = 1;
    }

    internal void LevelReady()
    {
        if (!IsTruckContext())
        {
            CancelActiveDraw();
            _pendingTruckDraw = false;
            return;
        }
        if (!_pendingTruckDraw && !HasPersistentPendingDraw())
        {
            return;
        }
        _pendingTruckDraw = false;
        if (!_config.Enabled.Value ||
            !_config.TruckUpgradeDrawEnabled.Value ||
            !SemiFunc.IsMasterClientOrSingleplayer() ||
            !IsTruckContext())
        {
            return;
        }

        StartDraw();
    }

    internal bool TryRunTestDraw(out string response)
    {
        if (!_config.Enabled.Value)
        {
            response = "RoleShuffle is disabled.";
            return false;
        }
        if (!_config.TruckUpgradeDrawEnabled.Value)
        {
            response = "The truck Base Upgrade draw is disabled.";
            return false;
        }
        if (!SemiFunc.IsMasterClientOrSingleplayer())
        {
            response = "Only the host can run the Base Upgrade slot.";
            return false;
        }
        if (!IsTruckContext())
        {
            response = "The Base Upgrade slot can only run in the truck.";
            return false;
        }
        if (_drawRunning)
        {
            response = "The Base Upgrade slot is already running.";
            return false;
        }
        if (!ReadyToApply())
        {
            response = "Player upgrade data is not ready.";
            return false;
        }

        PrepareForTruckTransition();
        try
        {
            SemiFunc.SaveFileSave();
        }
        catch (Exception exception)
        {
            StageRolesPlugin.ModLogger.LogWarning(
                "Base Upgrade slot test state could not be saved before " +
                $"starting: {exception.Message}");
        }
        _pendingTruckDraw = false;
        StartDraw();
        response = "Base Upgrade slot started.";
        return true;
    }

    internal bool TryChangeBaseUpgrade(
        string upgradeName,
        string value,
        out string response)
    {
        if (!_config.Enabled.Value)
        {
            response = "RoleShuffle is disabled.";
            return false;
        }
        if (!SemiFunc.IsMasterClientOrSingleplayer())
        {
            response = "Only the host can change Base Upgrades.";
            return false;
        }
        if (!IsTruckContext())
        {
            response = "Base Upgrades can only be changed in the truck.";
            return false;
        }
        if (_drawRunning)
        {
            response = "Wait for the Base Upgrade slot to finish.";
            return false;
        }
        if (!UpgradeService.Ready || GameDirector.instance == null)
        {
            response = "Player upgrade data is not ready.";
            return false;
        }

        bool relative = value.StartsWith("+", StringComparison.Ordinal) ||
                        value.StartsWith("-", StringComparison.Ordinal);
        if (!int.TryParse(value, out int requested))
        {
            response = "The Base Upgrade value must be a number.";
            return false;
        }

        IReadOnlyList<UpgradeGrant> current = RoleCatalog.BaseUpgrades(_config);
        bool all = string.Equals(
            NormalizeUpgradeName(upgradeName),
            "all",
            StringComparison.OrdinalIgnoreCase) ||
            string.Equals(
                NormalizeUpgradeName(upgradeName),
                "allupgrades",
                StringComparison.OrdinalIgnoreCase);
        List<UpgradeGrant> targets = new();
        foreach (UpgradeGrant upgrade in current)
        {
            if (all || string.Equals(
                    NormalizeUpgradeName(upgrade.CommandName),
                    NormalizeUpgradeName(upgradeName),
                    StringComparison.OrdinalIgnoreCase))
            {
                targets.Add(upgrade);
            }
        }
        if (targets.Count == 0)
        {
            response = $"Unknown Base Upgrade: {upgradeName}.";
            return false;
        }

        Dictionary<string, int> configuredLevels = new(StringComparer.Ordinal);
        foreach (UpgradeGrant configured in
                 RoleCatalog.ConfiguredBaseUpgrades(_config))
        {
            configuredLevels[configured.DictionaryName] = configured.Level;
        }

        List<AppliedUpgrade> applied = new(targets.Count);
        foreach (UpgradeGrant upgrade in targets)
        {
            int maximum = AbsoluteMaximumLevel(upgrade.DictionaryName);
            int desired = relative
                ? Math.Clamp(upgrade.Level + requested, 0, maximum)
                : Math.Clamp(requested, 0, maximum);
            int delta = desired - upgrade.Level;
            if (delta == 0)
            {
                continue;
            }
            int configured = configuredLevels.GetValueOrDefault(
                upgrade.DictionaryName,
                0);
            if (BaseUpgradeBonusStore.ApplyEffectiveDelta(
                    upgrade.DictionaryName,
                    upgrade.Level,
                    configured,
                    delta,
                    maximum))
            {
                applied.Add(new AppliedUpgrade(upgrade, delta));
            }
        }

        ApplyUpgradeChangesToPlayers(applied);
        try
        {
            SemiFunc.SaveFileSave();
            BaseUpgradeSync.Publish(_config);
        }
        catch (Exception exception)
        {
            StageRolesPlugin.ModLogger.LogWarning(
                $"Changed Base Upgrades could not be saved: {exception.Message}");
        }

        response = applied.Count == 0
            ? "Base Upgrade: no change."
            : "Base Upgrade changed: " + string.Join(
                ", ",
                applied.ConvertAll(item =>
                    $"{item.Upgrade.CommandName}" +
                    $"{(item.Delta > 0 ? "+" : string.Empty)}{item.Delta}")) +
              ".";
        return true;
    }

    private static string NormalizeUpgradeName(string value) =>
        value.Replace(" ", string.Empty)
            .Replace("_", string.Empty)
            .Replace("-", string.Empty);

    private void StartDraw()
    {
        int generation = ++_generation;
        _drawRunning = true;
        StartCoroutine(Draw(generation));
    }

    internal void Shutdown()
    {
        _pendingTruckDraw = false;
        _drawRunning = false;
        _generation++;
        _remoteGeneration++;
        StopAllCoroutines();
        _notifier?.End();
        if (_canvasObject != null)
        {
            Destroy(_canvasObject);
            _canvasObject = null;
            _panelObject = null;
            _titleText = null;
            _upgradeSlotText = null;
            _upgradeSlotNextText = null;
            _deltaSlotText = null;
            _deltaSlotNextText = null;
        }
    }

    private IEnumerator Draw(int generation)
    {
        yield return new WaitForSecondsRealtime(StartDelaySeconds);
        float readyDeadline = Time.realtimeSinceStartup + 5f;
        while (CanContinue(generation) &&
               !ReadyToApply() &&
               Time.realtimeSinceStartup < readyDeadline)
        {
            yield return new WaitForSecondsRealtime(0.1f);
        }
        if (!CanContinue(generation))
        {
            FinishDraw(generation);
            yield break;
        }
        if (!ReadyToApply())
        {
            StageRolesPlugin.ModLogger.LogWarning(
                "Truck Base Upgrade draw was skipped because player upgrade data was not ready.");
            FinishDraw(generation);
            yield break;
        }

        BuildEligibleList();
        SelectResults();
        int synchronizedStart = PublishDrawStart();
        EnsureUi();
        SetVisible(true);

        while (CanContinue(generation) &&
               synchronizedStart >= 0 &&
               MillisecondsUntil(synchronizedStart) > 0)
        {
            yield return null;
        }
        if (!CanContinue(generation))
        {
            SetVisible(false);
            FinishDraw(generation);
            yield break;
        }

        float startedAt = Time.realtimeSinceStartup;
        float upgradeStopAt = startedAt + UpgradeSpinDurationSeconds;
        float finishAt = startedAt + TotalSpinDurationSeconds;
        int upgradeCycle = -1;
        int deltaCycle = -1;
        while (CanContinue(generation) &&
               Time.realtimeSinceStartup < upgradeStopAt)
        {
            float elapsed = Time.realtimeSinceStartup - startedAt;
            AnimateReel(
                _upgradeSlotText,
                _upgradeSlotNextText,
                elapsed,
                RandomDisplayName,
                ref upgradeCycle);
            AnimateReel(
                _deltaSlotText,
                _deltaSlotNextText,
                elapsed,
                RandomDeltaText,
                ref deltaCycle);
            yield return null;
        }
        if (!CanContinue(generation))
        {
            SetVisible(false);
            FinishDraw(generation);
            yield break;
        }

        SetUpgradeSlot(
            _selected.Count > 0
                ? DisplayName(_selected[0])
                : "NONE");
        while (CanContinue(generation) &&
               Time.realtimeSinceStartup < finishAt)
        {
            AnimateReel(
                _deltaSlotText,
                _deltaSlotNextText,
                Time.realtimeSinceStartup - startedAt,
                RandomDeltaText,
                ref deltaCycle);
            yield return null;
        }
        if (!CanContinue(generation))
        {
            SetVisible(false);
            FinishDraw(generation);
            yield break;
        }

        bool recordDraw = _selected.Count > 0 && UpgradeService.Ready;
        string drawnUpgrade = recordDraw ? _selected[0].CommandName : string.Empty;
        int[]? beforeLevels = null;
        try { if (recordDraw) beforeLevels = BaseUpgradeHistory.Levels(_config); }
        catch (Exception exception) { StageRolesPlugin.ModLogger.LogWarning($"Could not prepare draw history: {exception.Message}"); }
        ApplyResults();
        try { if (beforeLevels != null) BaseUpgradeHistory.Record(drawnUpgrade, _selectedDelta, beforeLevels, _config); }
        catch (Exception exception) { StageRolesPlugin.ModLogger.LogWarning($"Could not record draw history: {exception.Message}"); }
        CompletePersistentDrawAndSave();
        SetUpgradeSlot(
            _selected.Count > 0
                ? DisplayName(_selected[0])
                : "NONE");
        SetDeltaSlot(DeltaText(_selectedDelta));
        AnnounceResult();

        float hideAt = Time.realtimeSinceStartup + ResultDurationSeconds;
        while (generation == _generation &&
               Time.realtimeSinceStartup < hideAt)
        {
            yield return null;
        }
        if (generation == _generation)
        {
            SetVisible(false);
        }
        FinishDraw(generation);
    }

    private void FinishDraw(int generation)
    {
        if (generation == _generation)
        {
            _drawRunning = false;
        }
    }

    private bool CanContinue(int generation) =>
        generation == _generation &&
        _config.Enabled.Value &&
        _config.TruckUpgradeDrawEnabled.Value &&
        SemiFunc.IsMasterClientOrSingleplayer() &&
        IsTruckContext();

    private static bool HasPersistentPendingDraw() =>
        StatsManager.instance != null &&
        StatsManager.instance.runStats.GetValueOrDefault(PendingDrawKey, 0) > 0;

    private static void CompletePersistentDrawAndSave()
    {
        if (!SemiFunc.IsMasterClientOrSingleplayer() ||
            StatsManager.instance == null)
        {
            return;
        }
        StatsManager.instance.runStats.Remove(PendingDrawKey);
        try
        {
            SemiFunc.SaveFileSave();
        }
        catch (Exception exception)
        {
            StageRolesPlugin.ModLogger.LogWarning(
                "Truck Base Upgrade draw result could not be saved " +
                $"immediately: {exception.Message}");
        }
    }

    private static bool ReadyToApply()
    {
        if (!UpgradeService.Ready || GameDirector.instance == null)
        {
            return false;
        }
        try
        {
            PlayerAvatar local = SemiFunc.PlayerGetLocal();
            return local != null &&
                   local.gameObject.activeInHierarchy &&
                   !string.IsNullOrEmpty(PlayerIdentity.SteamId(local));
        }
        catch
        {
            return false;
        }
    }

    private void BuildEligibleList()
    {
        _allBaseUpgrades.Clear();
        _individualCandidates.Clear();
        _displayCandidates.Clear();
        foreach (UpgradeGrant upgrade in RoleCatalog.BaseUpgrades(_config))
        {
            _allBaseUpgrades.Add(upgrade);
            if (EffectiveUpgradeWeight(upgrade) <= 0f)
            {
                continue;
            }
            _individualCandidates.Add(upgrade);
            _displayCandidates.Add(upgrade);
        }
        if (_config.TruckUpgradeDrawWeight(AllUpgrades.CommandName) > 0)
        {
            _displayCandidates.Add(AllUpgrades);
        }
        ParseDeltaRules(_config.TruckUpgradeDrawDeltaWeights.Value);
        if (_deltaRules.Count == 0)
        {
            ParseDeltaRules(DefaultDeltaWeights);
            StageRolesPlugin.ModLogger.LogWarning(
                "Truck Base Upgrade delta weights had no usable entries; defaults were used.");
        }
    }

    private void SelectResults()
    {
        _selected.Clear();
        _selectedDelta = 0;
        List<WeightedDelta> eligibleOutcomes = new();
        int totalWeight = 0;
        foreach (WeightedDelta rule in _deltaRules)
        {
            if (BuildPoolForDelta(rule.Delta).Count == 0)
            {
                continue;
            }
            eligibleOutcomes.Add(rule);
            totalWeight += rule.Weight;
        }
        if (totalWeight <= 0)
        {
            StageRolesPlugin.ModLogger.LogWarning(
                "Truck Base Upgrade draw had no eligible weighted result.");
            return;
        }

        int roll = UnityEngine.Random.Range(0, totalWeight);
        WeightedDelta selectedRule = eligibleOutcomes[eligibleOutcomes.Count - 1];
        foreach (WeightedDelta rule in eligibleOutcomes)
        {
            if (roll < rule.Weight)
            {
                selectedRule = rule;
                break;
            }
            roll -= rule.Weight;
        }

        _selectedDelta = selectedRule.Delta;
        List<UpgradeGrant> pool = BuildPoolForDelta(_selectedDelta);
        if (pool.Count > 0)
        {
            _selected.Add(SelectWeightedUpgrade(pool));
        }
    }

    private List<UpgradeGrant> BuildPoolForDelta(int delta)
    {
        List<UpgradeGrant> pool = new();
        foreach (UpgradeGrant upgrade in _individualCandidates)
        {
            if (EffectiveUpgradeWeight(upgrade) > 0f && CanApplyDelta(upgrade, delta))
            {
                pool.Add(upgrade);
            }
        }
        if (delta > 0 &&
            _config.TruckUpgradeDrawWeight(AllUpgrades.CommandName) > 0)
        {
            foreach (UpgradeGrant upgrade in _allBaseUpgrades)
            {
                if (!CanApplyDelta(upgrade, delta))
                {
                    continue;
                }
                pool.Add(AllUpgrades);
                break;
            }
        }
        return pool;
    }

    private bool CanApplyDelta(UpgradeGrant upgrade, int delta)
    {
        if (delta == 0)
        {
            return true;
        }
        if (delta > 0)
        {
            return upgrade.Level <=
                DrawMaximumLevel(upgrade.DictionaryName) - delta;
        }
        return upgrade.Level >= -delta;
    }

    private UpgradeGrant SelectWeightedUpgrade(
        IReadOnlyList<UpgradeGrant> pool)
    {
        float totalWeight = 0f;
        foreach (UpgradeGrant upgrade in pool)
        {
            totalWeight += EffectiveUpgradeWeight(upgrade);
        }

        float roll = UnityEngine.Random.Range(0f, totalWeight);
        foreach (UpgradeGrant upgrade in pool)
        {
            float weight = EffectiveUpgradeWeight(upgrade);
            if (roll < weight)
            {
                return upgrade;
            }
            roll -= weight;
        }
        return pool[pool.Count - 1];
    }

    private float EffectiveUpgradeWeight(UpgradeGrant upgrade)
    {
        int weight = _config.TruckUpgradeDrawWeight(upgrade.CommandName);
        return IsAllUpgrades(upgrade)
            ? weight
            : TruckDrawWeight.Resolve(
                weight,
                upgrade.Level,
                DrawMaximumLevel(upgrade.DictionaryName),
                _config.TruckUpgradeDrawCappedWeightMultiplier.Value,
                _config.TruckUpgradeDrawWeightFalloffExponent.Value);
    }

    private void ParseDeltaRules(string serialized)
    {
        _deltaRules.Clear();
        Dictionary<int, int> parsed = new();
        foreach (string segment in serialized.Split(
                     new[] { ',', ';' },
                     StringSplitOptions.RemoveEmptyEntries))
        {
            string[] parts = segment.Split(':');
            if (parts.Length != 2 ||
                !int.TryParse(parts[0].Trim(), out int delta) ||
                !int.TryParse(parts[1].Trim(), out int weight))
            {
                continue;
            }
            delta = Math.Clamp(
                delta,
                -MaximumConfiguredDelta,
                MaximumConfiguredDelta);
            weight = Math.Clamp(weight, 0, 1000);
            parsed[delta] = weight;
        }
        foreach (KeyValuePair<int, int> pair in parsed)
        {
            if (pair.Value > 0)
            {
                _deltaRules.Add(new WeightedDelta(pair.Key, pair.Value));
            }
        }
    }

    private void ApplyResults()
    {
        if (_selected.Count == 0 || !UpgradeService.Ready)
        {
            return;
        }
        if (_selectedDelta == 0)
        {
            StageRolesPlugin.ModLogger.LogInfo(
                $"Truck Base Upgrade draw result: " +
                $"{DisplayName(_selected[0])} no change.");
            return;
        }

        Dictionary<string, int> configuredLevels = new(StringComparer.Ordinal);
        foreach (UpgradeGrant configured in
                 RoleCatalog.ConfiguredBaseUpgrades(_config))
        {
            configuredLevels[configured.DictionaryName] = configured.Level;
        }
        bool applyAll = IsAllUpgrades(_selected[0]);
        IReadOnlyList<UpgradeGrant> targets = applyAll
            ? RoleCatalog.BaseUpgrades(_config)
            : _selected;
        List<AppliedUpgrade> applied = new(targets.Count);
        foreach (UpgradeGrant upgrade in targets)
        {
            int absoluteMaximum = AbsoluteMaximumLevel(upgrade.DictionaryName);
            int resultMaximum = _selectedDelta > 0
                ? DrawMaximumLevel(upgrade.DictionaryName)
                : absoluteMaximum;
            int desiredLevel = Math.Max(
                0,
                Math.Min(resultMaximum, upgrade.Level + _selectedDelta));
            int actualDelta = desiredLevel - upgrade.Level;
            if (actualDelta == 0)
            {
                continue;
            }
            int configuredLevel = configuredLevels.GetValueOrDefault(
                upgrade.DictionaryName,
                0);
            if (BaseUpgradeBonusStore.ApplyEffectiveDelta(
                    upgrade.DictionaryName,
                    upgrade.Level,
                    configuredLevel,
                    actualDelta,
                    absoluteMaximum))
            {
                applied.Add(new AppliedUpgrade(upgrade, actualDelta));
            }
        }

        if (applied.Count == 0 || GameDirector.instance == null)
        {
            _selected.Clear();
            return;
        }

        ApplyUpgradeChangesToPlayers(applied);

        StageRolesPlugin.ModLogger.LogInfo(
            $"Truck Base Upgrade draw result: {ResultNames()}.");
    }

    private static void ApplyUpgradeChangesToPlayers(
        IReadOnlyList<AppliedUpgrade> applied)
    {
        if (GameDirector.instance == null)
        {
            return;
        }
        bool hasDecrease = false;
        foreach (AppliedUpgrade appliedUpgrade in applied)
        {
            if (appliedUpgrade.Delta < 0)
            {
                hasDecrease = true;
                break;
            }
        }
        HashSet<string> players = new(StringComparer.Ordinal);
        foreach (PlayerAvatar player in GameDirector.instance.PlayerList)
        {
            if (player == null || !player.gameObject.activeInHierarchy)
            {
                continue;
            }
            string steamId = PlayerIdentity.SteamId(player);
            if (string.IsNullOrEmpty(steamId) || !players.Add(steamId))
            {
                continue;
            }
            Dictionary<string, int>? currentLevels = null;
            if (hasDecrease)
            {
                UpgradeService.TryGetLevels(steamId, out currentLevels);
            }
            foreach (AppliedUpgrade appliedUpgrade in applied)
            {
                UpgradeGrant upgrade = appliedUpgrade.Upgrade;
                if (appliedUpgrade.Delta < 0 &&
                    (currentLevels == null ||
                     currentLevels.GetValueOrDefault(
                         upgrade.DictionaryName,
                         0) < 1))
                {
                    continue;
                }
                UpgradeService.AddLevelsHostAuthoritative(
                    steamId,
                    upgrade.CommandName,
                    appliedUpgrade.Delta);
            }
        }
    }

    private void AnnounceResult()
    {
        PlayerAvatar host;
        try
        {
            host = SemiFunc.PlayerGetLocal();
        }
        catch
        {
            return;
        }
        if (host == null)
        {
            return;
        }
        string message = _selectedDelta == 0
            ? "NoChange"
            : _selected.Count == 0
                ? "NoUpgrade"
                : CompactSpokenResult(_selected[0]);
        _notifier.NotifyResponse(host, message);
    }

    private int PublishDrawStart()
    {
        if (!SemiFunc.IsMultiplayer() ||
            !PhotonNetwork.IsMasterClient ||
            PhotonNetwork.CurrentRoom == null)
        {
            return -1;
        }

        int startTimestamp = unchecked(
            PhotonNetwork.ServerTimestamp + DrawNetworkLeadMilliseconds);
        int drawId = unchecked(
            (PhotonNetwork.ServerTimestamp * 397) ^ ++_networkDrawSequence);
        if (drawId == 0)
        {
            drawId = 1;
        }
        string upgradeName = _selected.Count > 0
            ? _selected[0].CommandName
            : string.Empty;
        try
        {
            bool queued = PhotonNetwork.CurrentRoom.SetCustomProperties(
                new ExitGames.Client.Photon.Hashtable
                {
                    [DrawVersionKey] = DrawStateFormatVersion,
                    [DrawIdKey] = drawId,
                    [DrawUpgradeKey] = upgradeName,
                    [DrawDeltaKey] = _selectedDelta,
                    [DrawStartTimestampKey] = startTimestamp
                });
            return queued ? startTimestamp : -1;
        }
        catch (Exception exception)
        {
            StageRolesPlugin.ModLogger.LogDebug(
                $"Base Upgrade draw UI could not be synchronized: " +
                $"{exception.Message}");
            return -1;
        }
    }

    private void Update()
    {
        if (!SemiFunc.IsMultiplayer() ||
            PhotonNetwork.IsMasterClient ||
            !PhotonNetwork.InRoom)
        {
            if (_remoteRoomActive)
            {
                _remoteRoomActive = false;
                _lastRemoteDrawId = 0;
                _remoteGeneration++;
                SetVisible(false);
            }
            return;
        }

        _remoteRoomActive = true;
        if (Time.unscaledTime < _nextRemoteStateCheckAt)
        {
            return;
        }
        _nextRemoteStateCheckAt = Time.unscaledTime + 0.1f;
        ConsumeRoomDrawState();
    }

    private void ConsumeRoomDrawState()
    {
        try
        {
            if (!SemiFunc.IsMultiplayer() ||
                PhotonNetwork.IsMasterClient ||
                PhotonNetwork.CurrentRoom == null)
            {
                return;
            }
            ExitGames.Client.Photon.Hashtable properties =
                PhotonNetwork.CurrentRoom.CustomProperties;
            if (!TryGetRoomInt(properties, DrawVersionKey, out int version) ||
                version != DrawStateFormatVersion ||
                !TryGetRoomInt(properties, DrawIdKey, out int drawId) ||
                drawId == 0 ||
                drawId == _lastRemoteDrawId ||
                !TryGetRoomString(
                    properties,
                    DrawUpgradeKey,
                    out string upgradeName) ||
                !TryGetRoomInt(properties, DrawDeltaKey, out int delta) ||
                !TryGetRoomInt(
                    properties,
                    DrawStartTimestampKey,
                    out int startTimestamp) ||
                delta < -MaximumConfiguredDelta ||
                delta > MaximumConfiguredDelta ||
                !IsKnownDrawUpgrade(upgradeName))
            {
                return;
            }

            _lastRemoteDrawId = drawId;
            int hideTimestamp = unchecked(
                startTimestamp + Mathf.RoundToInt(
                    (TotalSpinDurationSeconds + ResultDurationSeconds) *
                    1000f));
            if (MillisecondsUntil(hideTimestamp) <= 0)
            {
                return;
            }

            int generation = ++_remoteGeneration;
            StartCoroutine(DisplayRemoteDraw(
                generation,
                upgradeName,
                delta,
                startTimestamp));
        }
        catch (Exception exception)
        {
            StageRolesPlugin.ModLogger.LogDebug(
                $"Base Upgrade draw room state was ignored safely: " +
                $"{exception.Message}");
        }
    }

    private IEnumerator DisplayRemoteDraw(
        int generation,
        string upgradeName,
        int delta,
        int startTimestamp)
    {
        int hideTimestamp = unchecked(
            startTimestamp + Mathf.RoundToInt(
                (TotalSpinDurationSeconds + ResultDurationSeconds) * 1000f));
        while (generation == _remoteGeneration &&
               PhotonNetwork.InRoom &&
               !PhotonNetwork.IsMasterClient &&
               !IsTruckContext() &&
               MillisecondsUntil(hideTimestamp) > 0)
        {
            yield return null;
        }
        if (!RemoteCanContinue(generation) ||
            MillisecondsUntil(hideTimestamp) <= 0)
        {
            yield break;
        }

        PrepareRemoteAnimation();
        EnsureUi();
        SetVisible(true);
        int upgradeCycle = -1;
        int deltaCycle = -1;
        while (RemoteCanContinue(generation) &&
               MillisecondsUntil(startTimestamp) > 0)
        {
            yield return null;
        }
        while (RemoteCanContinue(generation))
        {
            float elapsed = ElapsedSeconds(startTimestamp);
            if (elapsed >= UpgradeSpinDurationSeconds)
            {
                break;
            }
            AnimateReel(
                _upgradeSlotText,
                _upgradeSlotNextText,
                elapsed,
                RandomDisplayName,
                ref upgradeCycle);
            AnimateReel(
                _deltaSlotText,
                _deltaSlotNextText,
                elapsed,
                RandomDeltaText,
                ref deltaCycle);
            yield return null;
        }
        if (!RemoteCanContinue(generation))
        {
            HideRemoteDraw(generation);
            yield break;
        }

        SetUpgradeSlot(DisplayName(upgradeName));
        while (RemoteCanContinue(generation))
        {
            float elapsed = ElapsedSeconds(startTimestamp);
            if (elapsed >= TotalSpinDurationSeconds)
            {
                break;
            }
            AnimateReel(
                _deltaSlotText,
                _deltaSlotNextText,
                elapsed,
                RandomDeltaText,
                ref deltaCycle);
            yield return null;
        }
        if (!RemoteCanContinue(generation))
        {
            HideRemoteDraw(generation);
            yield break;
        }

        SetUpgradeSlot(DisplayName(upgradeName));
        SetDeltaSlot(DeltaText(delta));
        while (RemoteCanContinue(generation) &&
               MillisecondsUntil(hideTimestamp) > 0)
        {
            yield return null;
        }
        HideRemoteDraw(generation);
    }

    private void PrepareRemoteAnimation()
    {
        ParseDeltaRules(_config.TruckUpgradeDrawDeltaWeights.Value);
        if (_deltaRules.Count == 0)
        {
            ParseDeltaRules(DefaultDeltaWeights);
        }
    }

    private bool RemoteCanContinue(int generation) =>
        generation == _remoteGeneration &&
        SemiFunc.IsMultiplayer() &&
        PhotonNetwork.InRoom &&
        !PhotonNetwork.IsMasterClient &&
        IsTruckContext();

    private void HideRemoteDraw(int generation)
    {
        if (generation == _remoteGeneration)
        {
            SetVisible(false);
        }
    }

    private static bool IsKnownDrawUpgrade(string upgradeName) =>
        string.IsNullOrEmpty(upgradeName) ||
        upgradeName is "Health" or "Stamina" or "ExtraJump" or "Speed" or
            "Strength" or "Range" or "Launch" or "TumbleClimb" or
            "TumbleWings" or "CrouchRest" or "MapPlayerCount" or
            "DeathHeadBattery" or "AllUpgrades";

    private static int MillisecondsUntil(int timestamp) =>
        unchecked(timestamp - PhotonNetwork.ServerTimestamp);

    private static float ElapsedSeconds(int startTimestamp) =>
        Mathf.Max(
            0f,
            unchecked(PhotonNetwork.ServerTimestamp - startTimestamp) /
            1000f);

    private static bool TryGetRoomInt(
        ExitGames.Client.Photon.Hashtable properties,
        string key,
        out int value)
    {
        value = 0;
        return properties.TryGetValue(key, out object raw) &&
               raw is int parsed &&
               (value = parsed) == parsed;
    }

    private static bool TryGetRoomString(
        ExitGames.Client.Photon.Hashtable properties,
        string key,
        out string value)
    {
        value = string.Empty;
        return properties.TryGetValue(key, out object raw) &&
               raw is string parsed &&
               (value = parsed) == parsed;
    }

    private string RandomDisplayName()
    {
        if (_displayCandidates.Count == 0)
        {
            return FallbackDisplayNames[
                UnityEngine.Random.Range(0, FallbackDisplayNames.Length)];
        }
        return DisplayName(
            _displayCandidates[
                UnityEngine.Random.Range(0, _displayCandidates.Count)]);
    }

    private string ResultNames()
    {
        if (_selected.Count == 0)
        {
            return "none";
        }
        List<string> names = new(_selected.Count);
        foreach (UpgradeGrant upgrade in _selected)
        {
            names.Add(SpokenResult(upgrade));
        }
        return string.Join(", ", names);
    }

    private string SpokenResult(UpgradeGrant upgrade) =>
        _selectedDelta == 0
            ? $"{DisplayName(upgrade)} no change"
            : $"{DisplayName(upgrade)} {DeltaText(_selectedDelta)}";

    private string CompactSpokenResult(UpgradeGrant upgrade) =>
        upgrade.CommandName +
        (_selectedDelta > 0 ? "+" : string.Empty) +
        _selectedDelta;

    private static string DeltaText(int delta) =>
        delta > 0 ? $"+{delta}" : delta.ToString();

    private string RandomDeltaText()
    {
        int totalWeight = 0;
        foreach (WeightedDelta rule in _deltaRules)
        {
            totalWeight += rule.Weight;
        }
        if (totalWeight <= 0)
        {
            return "0";
        }
        int roll = UnityEngine.Random.Range(0, totalWeight);
        foreach (WeightedDelta rule in _deltaRules)
        {
            if (roll < rule.Weight)
            {
                return DeltaText(rule.Delta);
            }
            roll -= rule.Weight;
        }
        return DeltaText(_deltaRules[_deltaRules.Count - 1].Delta);
    }

    private static string DisplayName(UpgradeGrant upgrade) =>
        DisplayName(upgrade.CommandName);

    private static string DisplayName(string upgradeName) =>
        string.IsNullOrEmpty(upgradeName)
            ? "NONE"
            : upgradeName switch
        {
            "ExtraJump" => "EXTRA JUMP",
            "TumbleClimb" => "TUMBLE CLIMB",
            "TumbleWings" => "TUMBLE WINGS",
            "CrouchRest" => "CROUCH REST",
            "MapPlayerCount" => "MAP PLAYER COUNT",
            "DeathHeadBattery" => "DEATH HEAD BATTERY",
            "AllUpgrades" => "ALL UPGRADES",
            _ => upgradeName.ToUpperInvariant()
        };

    private static bool IsAllUpgrades(UpgradeGrant upgrade) =>
        string.Equals(
            upgrade.CommandName,
            "AllUpgrades",
            StringComparison.Ordinal);

    private static int AbsoluteMaximumLevel(string dictionaryName) =>
        string.Equals(
            dictionaryName,
            "playerUpgradeMapPlayerCount",
            StringComparison.Ordinal)
            ? 1
            : DefaultMaximumLevel;

    private int DrawMaximumLevel(string dictionaryName) =>
        Math.Min(
            AbsoluteMaximumLevel(dictionaryName),
            Math.Max(0, _config.TruckUpgradeDrawMaximumLevel.Value));

    private static bool IsTruckContext()
    {
        try
        {
            return RunManager.instance != null &&
                   LevelGenerator.Instance != null &&
                   LevelGenerator.Instance.Generated &&
                   SemiFunc.RunIsLobby();
        }
        catch
        {
            return false;
        }
    }

    private void EnsureUi()
    {
        if (_canvasObject != null)
        {
            TryApplyFont();
            return;
        }

        _canvasObject = new GameObject(
            "RoleShuffle_BaseUpgradeDrawCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler));
        _canvasObject.hideFlags = HideFlags.HideAndDontSave;
        _canvasObject.transform.SetParent(transform, false);
        Canvas canvas = _canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30000;
        CanvasScaler scaler = _canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        _panelObject = new GameObject(
            "BaseUpgradeDrawPanel",
            typeof(RectTransform),
            typeof(CanvasGroup),
            typeof(Image),
            typeof(Outline));
        _panelObject.transform.SetParent(_canvasObject.transform, false);
        RectTransform panel = _panelObject.GetComponent<RectTransform>();
        panel.anchorMin = new Vector2(0.5f, 1f);
        panel.anchorMax = new Vector2(0.5f, 1f);
        panel.pivot = new Vector2(0.5f, 1f);
        panel.anchoredPosition = new Vector2(0f, -28f);
        panel.sizeDelta = PanelSize;
        Image image = _panelObject.GetComponent<Image>();
        image.color = new Color(0.008f, 0.009f, 0.01f, 0.82f);
        image.raycastTarget = false;
        Outline outline = _panelObject.GetComponent<Outline>();
        outline.effectColor = RepoOrangeDim;
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        _titleText = CreateText(
            "Title",
            new Vector2(0f, -8f),
            new Vector2(820f, 34f),
            24f,
            RepoOrange);
        _titleText.text = "BASE UPGRADE DRAW";
        _upgradeSlotText = CreateSlot(
            "UpgradeSlot",
            "UPGRADE",
            new Vector2(-104f, -45f),
            new Vector2(590f, 142f),
            60f,
            out _upgradeSlotNextText);
        _deltaSlotText = CreateSlot(
            "DeltaSlot",
            "CHANGE",
            new Vector2(308f, -45f),
            new Vector2(190f, 142f),
            76f,
            out _deltaSlotNextText);
        SetUpgradeSlot("---");
        SetDeltaSlot("---");
        TryApplyFont();
        SetVisible(false);
    }

    private TextMeshProUGUI CreateText(
        string name,
        Vector2 anchoredPosition,
        Vector2 size,
        float fontSize,
        Color color)
    {
        GameObject textObject = new(
            name,
            typeof(RectTransform),
            typeof(TextMeshProUGUI));
        textObject.transform.SetParent(_panelObject!.transform, false);
        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        RectTransform rect = text.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.outlineColor = new Color(0f, 0f, 0f, 0.95f);
        text.outlineWidth = 0.16f;
        text.raycastTarget = false;
        return text;
    }

    private TextMeshProUGUI CreateSlot(
        string name,
        string header,
        Vector2 anchoredPosition,
        Vector2 size,
        float fontSize,
        out TextMeshProUGUI nextText)
    {
        GameObject frame = new(
            name + "Frame",
            typeof(RectTransform),
            typeof(Image),
            typeof(Outline),
            typeof(RectMask2D));
        frame.transform.SetParent(_panelObject!.transform, false);
        RectTransform frameRect = frame.GetComponent<RectTransform>();
        frameRect.anchorMin = new Vector2(0.5f, 1f);
        frameRect.anchorMax = new Vector2(0.5f, 1f);
        frameRect.pivot = new Vector2(0.5f, 1f);
        frameRect.anchoredPosition = anchoredPosition;
        frameRect.sizeDelta = size;
        Image image = frame.GetComponent<Image>();
        image.color = new Color(0.018f, 0.014f, 0.012f, 0.96f);
        image.raycastTarget = false;
        Outline outline = frame.GetComponent<Outline>();
        outline.effectColor = RepoOrange;
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        CreateFrameAccent(
            frame.transform,
            "TopAccent",
            new Vector2(0f, -1.5f),
            new Vector2(size.x - 4f, 3f),
            new Vector2(0.5f, 1f));
        CreateFrameAccent(
            frame.transform,
            "LeftAccent",
            new Vector2(1.5f, -16f),
            new Vector2(3f, 29f),
            new Vector2(0f, 1f));

        GameObject headerObject = new(
            name + "Header",
            typeof(RectTransform),
            typeof(TextMeshProUGUI));
        headerObject.transform.SetParent(frame.transform, false);
        TextMeshProUGUI headerText =
            headerObject.GetComponent<TextMeshProUGUI>();
        RectTransform headerRect = headerText.rectTransform;
        headerRect.anchorMin = new Vector2(0f, 1f);
        headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.pivot = new Vector2(0.5f, 1f);
        headerRect.anchoredPosition = new Vector2(10f, -6f);
        headerRect.sizeDelta = new Vector2(-20f, 20f);
        headerText.text = header;
        headerText.fontSize = 14f;
        headerText.fontStyle = FontStyles.Bold;
        headerText.color = RepoOrange;
        headerText.alignment = TextAlignmentOptions.Left;
        headerText.enableWordWrapping = false;
        headerText.raycastTarget = false;

        GameObject viewport = new(
            name + "ValueViewport",
            typeof(RectTransform),
            typeof(RectMask2D));
        viewport.transform.SetParent(frame.transform, false);
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = new Vector2(5f, 2f);
        viewportRect.offsetMax = new Vector2(-5f, -21f);
        viewport.transform.SetAsFirstSibling();

        TextMeshProUGUI text = CreateSlotValueText(
            viewport.transform,
            name,
            fontSize);
        nextText = CreateSlotValueText(
            viewport.transform,
            name + "Next",
            fontSize);
        viewport.transform.SetAsFirstSibling();
        headerObject.transform.SetAsLastSibling();
        SetReelValue(text, nextText, string.Empty);
        return text;
    }

    private static TextMeshProUGUI CreateSlotValueText(
        Transform parent,
        string name,
        float fontSize)
    {
        GameObject textObject = new(
            name,
            typeof(RectTransform),
            typeof(TextMeshProUGUI),
            typeof(Shadow));
        textObject.transform.SetParent(parent, false);
        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(3f, 0f);
        textRect.offsetMax = new Vector2(-3f, 0f);
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Overflow;
        text.outlineColor = new Color(0f, 0f, 0f, 0.95f);
        text.outlineWidth = 0.16f;
        text.raycastTarget = false;
        Shadow shadow = textObject.GetComponent<Shadow>();
        shadow.effectColor = new Color(
            RepoOrange.r,
            RepoOrange.g,
            RepoOrange.b,
            0.35f);
        shadow.effectDistance = new Vector2(2f, -2f);
        return text;
    }

    private static void CreateFrameAccent(
        Transform parent,
        string name,
        Vector2 anchoredPosition,
        Vector2 size,
        Vector2 pivot)
    {
        GameObject accent = new(
            name,
            typeof(RectTransform),
            typeof(Image));
        accent.transform.SetParent(parent, false);
        RectTransform rect = accent.GetComponent<RectTransform>();
        rect.anchorMin = pivot;
        rect.anchorMax = pivot;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        Image image = accent.GetComponent<Image>();
        image.color = RepoOrange;
        image.raycastTarget = false;
    }

    private void SetUpgradeSlot(string value)
    {
        SetReelValue(_upgradeSlotText, _upgradeSlotNextText, value);
    }

    private void SetDeltaSlot(string value)
    {
        SetReelValue(_deltaSlotText, _deltaSlotNextText, value);
    }

    private static void SetReelValue(
        TextMeshProUGUI? current,
        TextMeshProUGUI? next,
        string value)
    {
        if (current != null)
        {
            current.text = value;
            SetReelPosition(current, 0f);
        }
        if (next != null)
        {
            next.text = string.Empty;
            SetReelPosition(next, SlotReelTravel);
        }
    }

    private static void AnimateReel(
        TextMeshProUGUI? first,
        TextMeshProUGUI? second,
        float elapsed,
        Func<string> randomValue,
        ref int observedCycle)
    {
        if (first == null || second == null)
        {
            return;
        }
        int cycle = Math.Max(0, Mathf.FloorToInt(elapsed / SpinStepSeconds));
        if (cycle != observedCycle)
        {
            if (observedCycle < 0)
            {
                first.text = randomValue();
            }
            TextMeshProUGUI incoming = cycle % 2 == 0 ? second : first;
            incoming.text = randomValue();
            observedCycle = cycle;
        }

        float progress = Mathf.Repeat(elapsed, SpinStepSeconds) /
                         SpinStepSeconds;
        TextMeshProUGUI outgoingText = cycle % 2 == 0 ? first : second;
        TextMeshProUGUI incomingText = cycle % 2 == 0 ? second : first;
        SetReelPosition(outgoingText, -SlotReelTravel * progress);
        SetReelPosition(incomingText, SlotReelTravel * (1f - progress));
    }

    private static void SetReelPosition(TextMeshProUGUI text, float y)
    {
        RectTransform rect = text.rectTransform;
        rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, y);
    }

    private void SetVisible(bool visible)
    {
        if (_panelObject != null && _panelObject.activeSelf != visible)
        {
            _panelObject.SetActive(visible);
        }
    }

    private void CancelActiveDraw()
    {
        _generation++;
        _remoteGeneration++;
        _drawRunning = false;
        StopAllCoroutines();
        _notifier.ResetPendingLocally();
        SetVisible(false);
    }

    private void TryApplyFont()
    {
        TMP_Text? source = HealthUI.instance != null
            ? HealthUI.instance.GetComponent<TextMeshProUGUI>()
            : ChatUI.instance?.chatText;
        if (source?.font == null)
        {
            return;
        }
        if (_titleText != null)
        {
            _titleText.font = source.font;
        }
        if (_upgradeSlotText != null)
        {
            _upgradeSlotText.font = source.font;
        }
        if (_upgradeSlotNextText != null)
        {
            _upgradeSlotNextText.font = source.font;
        }
        if (_deltaSlotText != null)
        {
            _deltaSlotText.font = source.font;
        }
        if (_deltaSlotNextText != null)
        {
            _deltaSlotNextText.font = source.font;
        }
        if (_panelObject != null)
        {
            foreach (TMP_Text text in
                     _panelObject.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text != null && text.font != source.font)
                {
                    text.font = source.font;
                }
            }
        }
    }
}
