using System;
using System.Reflection;
using BepInEx.Bootstrap;
using UnityEngine;

namespace REPOJP.StageRoles;

internal static class StageFluxCompatibility
{
    private const int MinimumApiVersion = 2;
    private const int MaximumApiVersion = 2;
    private const string ApiTypeName =
        "REPOJP.StagePhysicsEvents.StageFluxCompatibilityApi";
    private const string CarrierTypeName =
        "REPOJP.StagePhysicsEvents.StagePhysicsEffectCarrierMarker";
    private static Assembly? _resolvedAssembly;
    private static int _apiVersion;
    private static Type? _carrierType;
    private static MethodInfo? _markInternalCarrier;
    private static MethodInfo? _expectNotification;
    private static MethodInfo? _cancelNotification;
    private static MethodInfo? _clearNotifications;
    private static MethodInfo? _getValuableValueMultiplier;
    private static MethodInfo? _willHandleSecondChance;
    private static MethodInfo? _isNotificationBusy;
    private static MethodInfo? _tryReserveNotificationWindow;
    private static bool _contractFailureLogged;
    private static bool _invocationFailureLogged;

    internal static void MarkInternalCarrier(GameObject instance)
    {
        if (instance == null)
        {
            return;
        }
        ResolveSafely();
        if (TryInvoke(_markInternalCarrier, instance))
        {
            return;
        }
        if (_carrierType == null || instance.GetComponent(_carrierType) != null)
        {
            return;
        }
        try
        {
            instance.AddComponent(_carrierType);
        }
        catch (Exception exception)
        {
            StageRolesPlugin.ModLogger.LogDebug(
                $"Stage Flux carrier marker was not added: {exception.Message}");
        }
    }

    internal static void ExpectNotification(PlayerAvatar player, string message)
    {
        ResolveSafely();
        if (!TryInvoke(_expectNotification, player, message))
        {
            NotificationEnemyReactionGuard.Expect(player, message);
        }
    }

    internal static void CancelExpectedNotification(PlayerAvatar player)
    {
        ResolveSafely();
        TryInvoke(_cancelNotification, player);
        NotificationEnemyReactionGuard.CancelExpected(player);
    }

    internal static void ClearNotifications()
    {
        ResolveSafely();
        TryInvoke(_clearNotifications);
        NotificationEnemyReactionGuard.Clear();
    }

    internal static float GetValuableValueMultiplier(ValuableObject valuable)
    {
        ResolveSafely();
        if (_getValuableValueMultiplier == null || valuable == null)
        {
            return 1f;
        }
        try
        {
            object? result = _getValuableValueMultiplier.Invoke(
                null,
                new object?[] { valuable });
            return result is float multiplier &&
                   !float.IsNaN(multiplier) &&
                   !float.IsInfinity(multiplier) &&
                   multiplier > 0f
                ? multiplier
                : 1f;
        }
        catch (Exception exception)
        {
            LogInvocationFailure(exception);
            return 1f;
        }
    }

    internal static bool WillHandleSecondChance(PlayerAvatar player)
    {
        ResolveSafely();
        return TryInvokeBoolean(_willHandleSecondChance, out bool result, player) &&
               result;
    }

    internal static bool TryGetNotificationBusy(out bool busy)
    {
        ResolveSafely();
        if (TryInvokeBoolean(_isNotificationBusy, out bool result))
        {
            busy = result;
            return true;
        }
        busy = false;
        return false;
    }

    internal static bool TryReserveNotificationWindow(
        string owner,
        float seconds)
    {
        ResolveSafely();
        if (_tryReserveNotificationWindow == null)
        {
            return true;
        }
        return !TryInvokeBoolean(
                   _tryReserveNotificationWindow,
                   out bool reserved,
                   owner,
                   seconds) ||
               reserved;
    }

    private static void Resolve()
    {
        if (!Chainloader.PluginInfos.TryGetValue(
                StageRolesPlugin.StageFluxGuid,
                out BepInEx.PluginInfo pluginInfo) ||
            pluginInfo.Instance == null)
        {
            return;
        }

        Assembly assembly = pluginInfo.Instance.GetType().Assembly;
        if (ReferenceEquals(_resolvedAssembly, assembly))
        {
            return;
        }

        _resolvedAssembly = assembly;
        _apiVersion = 0;
        _carrierType = null;
        ClearApiMethods();
        _contractFailureLogged = false;
        _invocationFailureLogged = false;
        _carrierType = assembly.GetType(CarrierTypeName, false);
        if (_carrierType != null && !typeof(Component).IsAssignableFrom(_carrierType))
        {
            _carrierType = null;
        }

        Type? apiType = assembly.GetType(ApiTypeName, false);
        if (apiType == null || !apiType.IsPublic)
        {
            RejectContract("public compatibility API type was not found.");
            return;
        }
        if (!CompatibilityApiContract.TryReadApiVersion(
                apiType,
                out _apiVersion,
                out string versionFailure))
        {
            RejectContract(versionFailure);
            return;
        }
        if (_apiVersion < MinimumApiVersion ||
            _apiVersion > MaximumApiVersion)
        {
            RejectContract(
                $"API version {_apiVersion} is outside the supported range " +
                $"{MinimumApiVersion}-{MaximumApiVersion}.");
            return;
        }

        _markInternalCarrier = FindFirstMethod(
            apiType,
            new[] { "MarkInternalCarrier", "RegisterInternalCarrier" },
            typeof(bool),
            typeof(GameObject));
        _expectNotification = FindFirstMethod(
            apiType,
            new[]
            {
                "ExpectNotification",
                "SuppressNotificationEnemyReaction"
            },
            typeof(bool),
            typeof(PlayerAvatar),
            typeof(string));
        _cancelNotification = FindFirstMethod(
            apiType,
            new[]
            {
                "CancelExpectedNotification",
                "CancelNotificationEnemyReactionSuppression"
            },
            typeof(void),
            typeof(PlayerAvatar));
        _clearNotifications = FindFirstMethod(
            apiType,
            new[]
            {
                "ClearNotifications",
                "ClearNotificationSoundSuppression"
            },
            typeof(void));
        _getValuableValueMultiplier = FindFirstMethod(
            apiType,
            new[] { "GetValuableValueMultiplier" },
            typeof(float),
            typeof(ValuableObject));
        _willHandleSecondChance = FindFirstMethod(
            apiType,
            new[] { "WillHandleSecondChance" },
            typeof(bool),
            typeof(PlayerAvatar));
        _isNotificationBusy = FindFirstMethod(
            apiType,
            new[] { "IsNotificationBusy" },
            typeof(bool));
        _tryReserveNotificationWindow = FindFirstMethod(
            apiType,
            new[] { "TryReserveNotificationWindow" },
            typeof(bool),
            typeof(string),
            typeof(float));

        if (_markInternalCarrier == null || _expectNotification == null ||
            _cancelNotification == null || _clearNotifications == null ||
            _getValuableValueMultiplier == null ||
            _willHandleSecondChance == null || _isNotificationBusy == null ||
            _tryReserveNotificationWindow == null)
        {
            RejectContract(
                "one or more required v2 methods have an incompatible signature.");
        }
    }

    private static void ResolveSafely()
    {
        try
        {
            Resolve();
        }
        catch (Exception exception)
        {
            _carrierType = null;
            ClearApiMethods();
            LogInvocationFailure(exception);
        }
    }

    private static MethodInfo? FindFirstMethod(
        Type apiType,
        string[] names,
        Type returnType,
        params Type[] parameters)
    {
        foreach (string name in names)
        {
            MethodInfo? method = CompatibilityApiContract.FindMethod(
                apiType,
                name,
                returnType,
                parameters);
            if (method != null)
            {
                return method;
            }
        }
        return null;
    }

    private static void ClearApiMethods()
    {
        _markInternalCarrier = null;
        _expectNotification = null;
        _cancelNotification = null;
        _clearNotifications = null;
        _getValuableValueMultiplier = null;
        _willHandleSecondChance = null;
        _isNotificationBusy = null;
        _tryReserveNotificationWindow = null;
    }

    private static bool TryInvoke(MethodInfo? method, params object?[] arguments)
    {
        if (method == null)
        {
            return false;
        }
        try
        {
            object? result = method.Invoke(null, arguments);
            return method.ReturnType == typeof(void) || result is not false;
        }
        catch (Exception exception)
        {
            LogInvocationFailure(exception);
            return false;
        }
    }

    private static bool TryInvokeBoolean(
        MethodInfo? method,
        out bool result,
        params object?[] arguments)
    {
        result = false;
        if (method == null || method.ReturnType != typeof(bool))
        {
            return false;
        }
        try
        {
            result = method.Invoke(null, arguments) is true;
            return true;
        }
        catch (Exception exception)
        {
            LogInvocationFailure(exception);
            return false;
        }
    }

    private static void LogInvocationFailure(Exception exception)
    {
        if (_invocationFailureLogged)
        {
            return;
        }
        _invocationFailureLogged = true;
        StageRolesPlugin.ModLogger.LogDebug(
            $"Stage Flux compatibility API call failed; using the legacy path: " +
            exception.GetBaseException().Message);
    }

    private static void LogContractFailure(string detail)
    {
        if (_contractFailureLogged)
        {
            return;
        }
        _contractFailureLogged = true;
        StageRolesPlugin.ModLogger.LogDebug(
            $"Stage Flux compatibility API v{MinimumApiVersion} contract was " +
            $"rejected; using RoleShuffle fallback behavior: {detail}");
    }

    private static void RejectContract(string detail)
    {
        _carrierType = null;
        ClearApiMethods();
        LogContractFailure(detail);
    }
}
