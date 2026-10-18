namespace REPOJP.StageRoles;

// Player-facing role numbers are contiguous. Existing room properties retain their
// established IDs so mixed builds cannot interpret Twins as the removed role.
internal static class RoleWireId
{
    internal static int Encode(StageRole role) => role switch
    {
        StageRole.Twins => 42,
        StageRole.Porter => 43,
        StageRole.DualWielder => 44,
        _ => (int)role
    };

    internal static StageRole Decode(int value) => value switch
    {
        41 => (StageRole)(-1),
        42 => StageRole.Twins,
        43 => StageRole.Porter,
        44 => StageRole.DualWielder,
        _ => (StageRole)value
    };
}
