using System;
using System.Reflection;

namespace REPOJP.StageRoles;

internal static class CompatibilityApiContract
{
    private const BindingFlags PublicStatic =
        BindingFlags.Public | BindingFlags.Static;

    internal static bool TryReadApiVersion(
        Type apiType,
        out int version,
        out string failure)
    {
        version = 0;
        failure = string.Empty;
        try
        {
            FieldInfo? field = apiType.GetField("ApiVersion", PublicStatic);
            if (field != null)
            {
                if (field.FieldType != typeof(int))
                {
                    failure = "ApiVersion field is not Int32.";
                    return false;
                }
                object? value = field.IsLiteral
                    ? field.GetRawConstantValue()
                    : field.GetValue(null);
                if (value is int fieldVersion)
                {
                    version = fieldVersion;
                    return true;
                }
                failure = "ApiVersion field did not return Int32.";
                return false;
            }

            PropertyInfo? property = apiType.GetProperty(
                "ApiVersion",
                PublicStatic);
            MethodInfo? getter = property?.GetGetMethod(nonPublic: false);
            if (property == null || property.PropertyType != typeof(int) ||
                property.GetIndexParameters().Length != 0 ||
                getter == null || !getter.IsStatic)
            {
                failure = "public static Int32 ApiVersion was not found.";
                return false;
            }
            if (property.GetValue(null) is int propertyVersion)
            {
                version = propertyVersion;
                return true;
            }
            failure = "ApiVersion property did not return Int32.";
            return false;
        }
        catch (Exception exception)
        {
            failure = exception.GetBaseException().Message;
            return false;
        }
    }

    internal static MethodInfo? FindMethod(
        Type apiType,
        string name,
        Type returnType,
        params Type[] parameterTypes)
    {
        try
        {
            MethodInfo? method = apiType.GetMethod(
                name,
                PublicStatic,
                null,
                parameterTypes,
                null);
            return method?.ReturnType == returnType ? method : null;
        }
        catch
        {
            return null;
        }
    }
}
