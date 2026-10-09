using System;

namespace REPOJP.StageRoles;

internal static class SignalmanRules
{
    internal static bool IsOrdinaryChat(string? message, string excludedCommands, string excludedPrefixes)
    {
        if (string.IsNullOrWhiteSpace(message)) return false;
        // Normalize only for classification. The relayed payload stays untouched.
        string input = message!.Trim();
        if (input.StartsWith("/", StringComparison.Ordinal)) return false;
        switch (input.ToLowerInvariant())
        {
            case "star": case "roll": case "gravity": case "void": case "laser": case "decoy": return false;
        }
        foreach (string prefix in excludedPrefixes.Split(','))
            if (prefix.Trim().Length > 0 && input.StartsWith(prefix.Trim(), StringComparison.OrdinalIgnoreCase)) return false;
        foreach (string command in excludedCommands.Split(','))
            if (command.Trim().Length > 0 && string.Equals(input, command.Trim(), StringComparison.OrdinalIgnoreCase)) return false;
        return true;
    }

    // Private vanilla playback has no completion acknowledgement. Reserve a
    // conservative window on the host; native chat/counts replace this reservation.
    internal static float PlaybackReservation(string message) => Math.Clamp(3f + message.Length * 0.35f, 4f, 120f);
}
