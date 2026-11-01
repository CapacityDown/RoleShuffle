using Photon.Pun;
namespace REPOJP.StageRoles;
internal static class RoleTestMenuChecks
{
    internal static void Run(StageRolesConfig config)
    {
        int checks = 0;
        void Check(bool ok,string name) { checks++; if(!ok)throw new Exception(name); }
        var controller = new StageRoleController { RoleAssignmentsReady = true };
        controller.Assignments.AddRange(new[] { new RoleAssignment("76561198000000001","Same",StageRole.Tank), new RoleAssignment("76561198000000002","Same",StageRole.Runner), new RoleAssignment("76561198000000003","Third",StageRole.Lifter) });
        string first = controller.Assignments[0].SteamId, second = controller.Assignments[1].SteamId, third = controller.Assignments[2].SteamId;
        var service = new RoleTestMenuService(config,controller);
        StageRolesPlugin.Instance.RoleTests = service;
        GameManager.instance = new(); SemiFunc.Multiplayer = true; PhotonNetwork.IsMasterClient = true;
        config.SetRoleCommandEnabled.Value = false;
        Check(!service.Visible && !service.TryChange(first,StageRole.Mage,"",out _), "Disabled testing rejects direct service call");
        config.SetRoleCommandEnabled.Value = true;
        PhotonNetwork.IsMasterClient = false;
        Check(!service.Visible && !service.TryChange(first,StageRole.Mage,"",out _), "Guest rejected despite local testing switch");
        PhotonNetwork.IsMasterClient = true; controller.RoleAssignmentsReady = false;
        Check(service.Visible && !service.TryChange(first,StageRole.Mage,"",out _), "No stage cannot modify roles");
        controller.RoleAssignmentsReady = true;
        Check(!service.TryChange("",StageRole.Mage,"",out _) && !service.TryChange("1",StageRole.Mage,"",out _) && !service.TryChange("Same",StageRole.Mage,"",out _), "Only stable exact identities accepted, never list number or ambiguous name");
        Check(!service.TryChange(first,(StageRole)999,"",out _), "Undefined role rejected");
        Check(service.TryChange(second,StageRole.Mage,"",out _) && controller.Calls[^1] == $"set:{second}:Mage", "Correct duplicate-name player sent to native command path");
        Check(service.TryChange(first,null,"",out _) && controller.Calls[^1] == $"random:{first}", "Single random draw uses production command");
        Check(service.TryChange(RoleTestMenuService.AllPlayers,null,"",out _) && controller.Calls[^1] == "random-all", "All random uses production planner");
        Check(service.TryChange(RoleTestMenuService.AllPlayers,StageRole.Runner,"",out _) && controller.Calls[^1] == "all:Runner", "All-target assignment uses production cleanup");
        Check(!service.TryChange(first,StageRole.Twins,first,out _) && !service.TryChange(RoleTestMenuService.AllPlayers,StageRole.Twins,second,out _), "Twins requires exactly two different targets");
        controller.Assignments[1].Player.Alive=false;
        Check(!service.TryChange(first,StageRole.Twins,second,out _), "Dead Twins partner rejected");
        controller.Assignments[1].Player.Alive=true;
        Check(service.TryChange(first,StageRole.Twins,second,out _) && controller.Calls[^1] == $"twins:{first}:{second}", "Twins dispatched together, never separate changes");
        controller.Assignments[2].AssignedRole=StageRole.Twins;
        Check(!service.TryChange(first,StageRole.Twins,second,out _), "Existing other Twins pair protected");
        controller.Assignments[2].AssignedRole=StageRole.Lifter;
        controller.Assignments[0].AssignedRole=StageRole.Twins;
        Check(!service.TryChange(first,StageRole.Runner,"",out _) && service.TryChange(first,StageRole.Influenza,"",out _), "Cannot split pair; infection uses existing linked infection path");
        controller.Assignments[0].AssignedRole=StageRole.Tank;
        RoleMenu.ShowTests(config);
        RoleMenu.Entries.Single(e=>e.Text=="1. Same — Tank").OnClick!();
        Action oldApply = RoleMenu.Entries.Single(e=>e.Text=="Apply: Mage").OnClick!;
        int calls = controller.Calls.Count;
        config.SetRoleCommandEnabled.Value=false; oldApply();
        Check(controller.Calls.Count==calls && RoleMenu.Entries.Any(e=>e.Text=="Testing is disabled."), "Already rendered button loses access immediately");
        RoleMenu.Entries.Single(e=>e.Text=="Back").OnClick!();
        Check(!RoleMenu.TestingPageVisible, "Back remains usable after authority/stage state invalidation");
        config.SetRoleCommandEnabled.Value=true; RoleMenu.ShowTests(config);
        RoleMenu.Entries.Single(e=>e.Text=="1. Same — Tank").OnClick!();
        int scrollBefore = RoleMenu.TestScrollResets;
        RoleMenu.Entries.Single(e=>e.Text=="Apply: Twins").OnClick!();
        Check(RoleMenu.TestScrollResets > scrollBefore, "Partner selection starts at top rather than retaining role-list scroll");
        Check(RoleMenu.Entries.Any(e=>e.Text=="Select a second player for Twins."), "Twins UI requests partner");
        RoleMenu.Entries.Single(e=>e.Text=="2. Same").OnClick!();
        Check(controller.Calls[^1]==$"twins:{first}:{second}", "Twins UI uses exact pair");
        oldApply=RoleMenu.Entries.Single(e=>e.Text=="Apply: Mage").OnClick!;
        controller.Assignments.RemoveAt(0); calls=controller.Calls.Count;
        oldApply();
        Check(controller.Calls.Count==calls && RoleMenu.Entries.Any(e=>e.Text=="Player is no longer available."), "Disconnect never falls back to another player");
        RoleMenu.ShowTests(config); RoleMenu.Entries.Single(e=>e.Text=="1. Same — Runner").OnClick!();
        oldApply=RoleMenu.Entries.Single(e=>e.Text=="Apply: Mage").OnClick!;
        RoleMenu.Entries.Single(e=>e.Text=="Back").OnClick!();
        RoleMenu.Entries.Single(e=>e.Text=="2. Third — Lifter").OnClick!();
        calls=controller.Calls.Count;oldApply();
        Check(controller.Calls.Count==calls,"Stale button cannot mutate a newly selected player");
        controller.RoleAssignmentsReady = false;
        RoleMenu.RefreshTests();
        calls = controller.Calls.Count;
        RoleMenu.Entries.Single(e=>e.Text=="Back").OnClick!();
        Check(controller.Calls.Count == calls && !RoleMenu.TestingPageVisible, "Back remains usable after stage ends with a player selected");
        SemiFunc.Multiplayer=false;
        Check(service.Visible,"Singleplayer supported");
        Console.WriteLine($"PASS: {checks} role-test menu, authority and production-dispatch checks (native gameplay simulated).");
    }
}
