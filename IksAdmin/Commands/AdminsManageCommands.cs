using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using IksAdmin.Functions;

namespace IksAdmin.Commands;

public static class AdminsManageCommands
{
    public static void Add(CCSPlayerController? caller, List<string> args, CommandInfo info)
    {
        var steamId = args[0];
        var name = args[1];
        var time = int.Parse(args[2]);
        var serverKey = args[3] == "-" ? null : args[3];
        switch (args.Count)
        {
            case 5:
                FAdminManage.Add(caller, info, steamId, name, time, serverKey, groupName: args[4]);
                break;
            case 6:
                FAdminManage.Add(caller, info, steamId, name, time, serverKey, flags: args[4], immunity: int.Parse(args[5]));
                break;
            default:
                throw new ArgumentException("Wrong command usage...");
        }
    }
}