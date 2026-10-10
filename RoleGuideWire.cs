using System;
using System.Text;

namespace REPOJP.StageRoles;

internal static class RoleGuideWire
{
    internal const int MaximumStringBytes = short.MaxValue;
    internal const int MaximumPayloadBytes = 1024 * 1024;
    private static readonly UTF8Encoding Utf8 = new(false, true);

    // Photon Protocol18 limits strings to 32767 UTF-8 bytes. Byte arrays have
    // their own length framing and carry larger guides without dropping roles.
    internal static object Encode(string payload)
    {
        int bytes = Utf8.GetByteCount(payload);
        if (bytes > MaximumPayloadBytes) throw new ArgumentException("Role guide exceeds the payload limit.");
        return bytes <= MaximumStringBytes ? payload : Utf8.GetBytes(payload);
    }

    internal static string? Decode(object? value)
    {
        if (value is string text) return text.Length <= MaximumPayloadBytes ? text : null;
        if (value is not byte[] bytes || bytes.Length > MaximumPayloadBytes) return null;
        try { return Utf8.GetString(bytes); }
        catch (DecoderFallbackException) { return null; }
    }

    internal static bool Same(object? left, object right)
    {
        if (left is string a && right is string b) return a == b;
        if (left is not byte[] x || right is not byte[] y || x.Length != y.Length) return false;
        for (int i = 0; i < x.Length; i++) if (x[i] != y[i]) return false;
        return true;
    }
}
