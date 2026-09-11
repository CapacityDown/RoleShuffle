using System;
using System.Reflection;
using BepInEx.Bootstrap;
using UnityEngine;

namespace REPOJP.StageRoles;

internal static class EliteEnemyVariantsCompatibility
{
    internal const string PluginGuid = "REPOJP.EliteEnemyVariants";
    private const int MinimumApiVersion = 1;
    private const int MaximumApiVersion = 1;
    private const int MinimumRewardTierBonus = 0;
    private const int MaximumRewardTierBonus = 1;
    private const string ApiTypeName =
        "REPOJP.EliteEnemyVariants.EliteEnemyVariantsCompatibilityApi";
    private static Assembly? _resolvedAssembly;
    private static int _apiVersion;
    private static MethodInfo? _getRewardTierBonus;
    private static bool _contractFailureLogged;
    private static bool _invocationFailureLogged;

    internal static int GetRewardTierBonus(Component component)
    {
        if (component == null)
        {
            return 0;
        }

        try
        {
            MethodInfo? method = ResolveRewardTierBonus();
            if (method == null)
            {
                return 0;
            }
            object? result = method.Invoke(null, new object?[] { component });
            if (result is int bonus &&
                bonus >= MinimumRewardTierBonus &&
                bonus <= MaximumRewardTierBonus)
            {
                return bonus;
            }
            LogInvocationFailure(
                $"GetRewardTierBonus returned an invalid value or type " +
                $"for API v{_apiVersion}; expected Int32 " +
                $"{MinimumRewardTierBonus}-{MaximumRewardTierBonus}.");
            return 0;
        }
        catch (Exception exception)
        {
            LogInvocationFailure(exception.GetBaseException().Message);
            return 0;
        }
    }

    private static MethodInfo? ResolveRewardTierBonus()
    {
        if (!Chainloader.PluginInfos.TryGetValue(
                PluginGuid,
                out BepInEx.PluginInfo pluginInfo) ||
            pluginInfo.Instance == null)
        {
            return null;
        }

        Assembly assembly = pluginInfo.Instance.GetType().Assembly;
        if (ReferenceEquals(_resolvedAssembly, assembly))
        {
            return _getRewardTierBonus;
        }

        _resolvedAssembly = assembly;
        _apiVersion = 0;
        _getRewardTierBonus = null;
        _contractFailureLogged = false;
        _invocationFailureLogged = false;
        Type? apiType = assembly.GetType(ApiTypeName, false);
        if (apiType == null || !apiType.IsPublic)
        {
            LogContractFailure("public compatibility API type was not found.");
            return null;
        }
        if (!CompatibilityApiContract.TryReadApiVersion(
                apiType,
                out _apiVersion,
                out string versionFailure))
        {
            LogContractFailure(versionFailure);
            return null;
        }
        if (_apiVersion < MinimumApiVersion ||
            _apiVersion > MaximumApiVersion)
        {
            LogContractFailure(
                $"API version {_apiVersion} is outside the supported range " +
                $"{MinimumApiVersion}-{MaximumApiVersion}.");
            return null;
        }

        _getRewardTierBonus = CompatibilityApiContract.FindMethod(
            apiType,
            "GetRewardTierBonus",
            typeof(int),
            typeof(Component));
        if (_getRewardTierBonus == null)
        {
            LogContractFailure(
                "GetRewardTierBonus(Component) : Int32 was not found.");
        }
        return _getRewardTierBonus;
    }

    private static void LogContractFailure(string detail)
    {
        if (_contractFailureLogged)
        {
            return;
        }
        _contractFailureLogged = true;
        StageRolesPlugin.ModLogger.LogDebug(
            $"Elite Enemy Variants compatibility contract was rejected; " +
            $"reward-tier bonus falls back to 0: {detail}");
    }

    private static void LogInvocationFailure(string detail)
    {
        if (_invocationFailureLogged)
        {
            return;
        }
        _invocationFailureLogged = true;
        StageRolesPlugin.ModLogger.LogDebug(
            $"Elite Enemy Variants reward-tier query failed open; " +
            $"bonus falls back to 0: {detail}");
    }
}
