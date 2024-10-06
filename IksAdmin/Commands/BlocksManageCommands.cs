using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using IksAdmin.Functions;
using IksAdminApi;

namespace IksAdmin.Commands;

public static class BlocksManageCommands
{
    public static void Ban(CCSPlayerController? caller, List<string> args, CommandInfo info)
    {
        //css_ban <#uid/#steamId/name> <time> <reason>
        var identity = args[0];
        var time = args[1];
        if (!int.TryParse(time, out int timeInt)) throw new ArgumentException("Time is not a number");
        var reason = string.Join(" ", args.Skip(2));
        Main.AdminApi.DoActionWithIdentity(caller, identity, target => 
        {
            var ban = new PlayerBan(
                new PlayerInfo(target),
                reason,
                timeInt,
                serverId: Main.AdminApi.ThisServer.Id
            );
            ban.AdminId = caller.Admin()!.Id;
            BlocksFunctions.Ban(ban);
        }, blockedArgs: ["@all", "@ct", "@t"]);
    }
}