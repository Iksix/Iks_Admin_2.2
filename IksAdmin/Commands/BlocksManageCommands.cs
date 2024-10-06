using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using IksAdmin.Functions;
using IksAdminApi;
using SteamWebAPI2.Interfaces;
using SteamWebAPI2.Utilities;

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
        }, blockedArgs: ["@all", "@ct", "@t", "@players", "@spec", "@bot"]);
    }

    public static void AddBan(CCSPlayerController? caller, List<string> args, CommandInfo info)
    {
        //css_addban <steamId> <time> <reason> (для оффлайн бана, так же можно использовать для онлайн бана)
        var steamId = args[0];
        var time = args[1];
        if (!int.TryParse(time, out int timeInt)) throw new ArgumentException("Time is not a number");
        var reason = string.Join(" ", args.Skip(2));
        string? name = null;
        string? ip = null;
        var target = AdminUtils.GetControllerBySteamId(steamId);
        if (target != null)
        {
            ip = target.GetIp();
        }
        var adminId = caller.Admin()!.Id;
        Task.Run(async () => {
            
            if (Main.AdminApi.Config.WebApiKey != "") 
            {
                var webInterfaceFactory = new SteamWebInterfaceFactory(Main.AdminApi.Config.WebApiKey);
                var steamInterface = webInterfaceFactory.CreateSteamWebInterface<SteamUser>(new HttpClient());
                var playerSummaryResponse = await steamInterface.GetPlayerSummaryAsync(ulong.Parse(steamId));
                name = playerSummaryResponse?.Data.Nickname;
            }
            var ban = new PlayerBan(
                steamId,
                ip,
                name,
                reason,
                timeInt,
                serverId: Main.AdminApi.ThisServer.Id
            );
            ban.AdminId = adminId;
            BlocksFunctions.Ban(ban);
        });
    }
}