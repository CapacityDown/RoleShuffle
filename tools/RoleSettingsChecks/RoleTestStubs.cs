namespace REPOJP.StageRoles;
internal sealed class TestAvatar(string name) { internal string Name = name; internal bool Alive = true; }
internal sealed class RoleAssignment(string id, string name, StageRole role)
{
    internal string SteamId = id;
    internal TestAvatar Player = new(name);
    internal StageRole AssignedRole = role;
}
internal static class PlayerIdentity { internal static string Name(TestAvatar player) => player.Name; }
internal static class PlayerState { internal static bool IsLiving(TestAvatar player) => player.Alive; }
internal sealed class StageRoleController
{
    internal bool RoleAssignmentsReady;
    internal List<RoleAssignment> Assignments = new();
    internal List<string> Calls = new();
    private bool Call(string text, out string response) { Calls.Add(text); response = text; return true; }
    internal bool TrySetRole(string id, StageRole role, out string response) => Call($"set:{id}:{role}", out response);
    internal bool TrySetRoleForAll(StageRole role, out string response) => Call($"all:{role}", out response);
    internal bool TryRandomizeRole(string id, out string response) => Call($"random:{id}", out response);
    internal bool TryRandomizeAllRoles(out string response) => Call("random-all", out response);
    internal bool TrySetTwins(string first, string second, out string response) => Call($"twins:{first}:{second}", out response);
}
