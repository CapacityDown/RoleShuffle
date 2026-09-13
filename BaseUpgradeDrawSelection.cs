using System;
using System.Collections.Generic;

namespace REPOJP.StageRoles;

internal static class BaseUpgradeDrawSelection
{
    // Stable order is shared by the versioned settings payload.
    internal static readonly IReadOnlyList<string> UpgradeNames = Array.AsReadOnly(new[]
    {
        "Health", "Stamina", "ExtraJump", "Speed", "Strength", "Range", "Launch",
        "TumbleClimb", "TumbleWings", "CrouchRest", "MapPlayerCount", "DeathHeadBattery", "AllUpgrades"
    });
}
