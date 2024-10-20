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
        var serverKey = args[3] == "-" ? null : args[3];
        switch (args.Count)
        {
            case 5:
                AdminManageFunctions.Add(caller, info, steamId, name, timeInt, serverKey, groupName: args[4]);
                break;
            case 6:
                AdminManageFunctions.Add(caller, info, steamId, name, timeInt, serverKey, flags: args[4], immunity: int.Parse(args[5]));
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
        // am_addflag_or_admin <steamId> <name> <time> <serverKey> <flags> <immunity>
        var admin = AdminUtils.Admin(args[0]);
        if (admin == null)
        {
            var steamId = args[0];
            var name = args[1];
            var time = args[2];
            if (!int.TryParse(time, out var timeInt))
            {
                throw new ArgumentException("Time must be a number");
            }
            var immunity = args[5];
            if (!int.TryParse(immunity, out var immunityInt))
            {
                throw new ArgumentException("Immunity must be a number");
            }
            var serverKey = args[3] == "-" ? null : args[3];
            AdminManageFunctions.Add(caller, info, steamId, name, timeInt, serverKey, immunity: immunityInt);
            return;
        }
        var flags = args[1];
        AdminManageFunctions.AddFlag(caller, info, admin, flags);
    }
}