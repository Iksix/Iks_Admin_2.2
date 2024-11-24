using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using IksAdmin.Functions;
using IksAdminApi;

namespace IksAdmin.Commands;

public static class AdminsManageCommands
{
    public static AdminApi AdminApi = Main.AdminApi!;
    public static void Add(CCSPlayerController? caller, List<string> args, CommandInfo info)
    {
        var steamId = args[0];
        var name = args[1];
        var time = args[2];
        if (!int.TryParse(time, out var timeInt))
        {
            throw new ArgumentException("Time must be a number");
        }
        int? serverId = args[3] == "this" ? null : int.Parse(args[3]);
        switch (args.Count)
        {
            case 5:
                AdminManageFunctions.Add(caller, info, steamId, name, timeInt, serverId, groupName: args[4]);
                break;
            case 6:
                AdminManageFunctions.Add(caller, info, steamId, name, timeInt, serverId, flags: args[4], immunity: int.Parse(args[5]));
                break;
            default:
                throw new ArgumentException("Wrong command usage...");
        }
    }

    public static void AddFlag(CCSPlayerController? caller, List<string> args, CommandInfo info)
    {
        var admin = AdminUtils.Admin(args[0]);
        if (admin == null)
        {
            return;
        }
        var flags = args[1];
        AdminManageFunctions.AddFlag(caller, info, admin, flags);
    }
    public static void AddFlagOrAdmin(CCSPlayerController? caller, List<string> args, CommandInfo info)
    {
        // am_addflag_or_admin <steamId> <name> <time/0> <server_id/this> <flags> <immunity>
        var admin = AdminUtils.Admin(args[0]);
        if (admin == null)
        {
            Add(caller, args, info);
            return;
        }
        var flags = args[1];
        AdminManageFunctions.AddFlag(caller, info, admin, flags);
    }

    public static void AddServerId(CCSPlayerController? caller, List<string> args, CommandInfo info)
    {
        // css_am_add_server_id <steamId> <server_id/this>
        var admin = AdminUtils.Admin(args[0]);
        if (admin == null)
        {
            Helper.Reply(info, "Admin not found ✖");
            return;
        }
        int? serverId = args[1] == "this" ? null : int.Parse(args[1]);
        AdminManageFunctions.AddServerId(caller, info, admin, serverId);
    }
}