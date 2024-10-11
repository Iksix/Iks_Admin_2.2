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
            Task.Run(async () => {
                await BlocksFunctions.Ban(ban);
            });
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
                var playerSummaryResponse = await Main.AdminApi.GetPlayerSummaries(ulong.Parse(steamId));
                name = playerSummaryResponse!.PersonaName;
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
            await BlocksFunctions.Ban(ban);
        });
    }

    public static void Unban(CCSPlayerController? caller, List<string> args, CommandInfo info)
    {
        var admin = caller.Admin()!;
        var steamId = args[0];
        var reason = args[1];
        Task.Run(async () => {
            await BlocksFunctions.Unban(admin, steamId, reason);
        });
    }
    public static void UnbanIp(CCSPlayerController? caller, List<string> args, CommandInfo info)
    {
        var admin = caller.Admin()!;
        var steamId = args[0];
        var reason = args[1];
        Task.Run(async () => {
            await BlocksFunctions.UnbanIp(admin, steamId, reason);
        });
    }

    public static void BanIp(CCSPlayerController? caller, List<string> args, CommandInfo info)
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
            Task.Run(async () => {
                await BlocksFunctions.Ban(ban);
            });
        }, blockedArgs: ["@all", "@ct", "@t", "@players", "@spec", "@bot"]);
    }

    public static void AddBanIp(CCSPlayerController? caller, List<string> args, CommandInfo info)
    {
        var ip = args[0];
        var time = args[1];
        if (!int.TryParse(time, out int timeInt)) throw new ArgumentException("Time is not a number");
        var reason = string.Join(" ", args.Skip(2));
        string? name = null;
        var target = AdminUtils.GetControllerByIp(ip);
        var adminId = caller.Admin()!.Id;
        string? steamId = null;
        if (target != null)
        {
            steamId = target.AuthorizedSteamID!.SteamId64.ToString();
        }
        Task.Run(async () => {
            if (Main.AdminApi.Config.WebApiKey != "") 
            {
                var playerSummaryResponse = await Main.AdminApi.GetPlayerSummaries(ulong.Parse(steamId));
                name = playerSummaryResponse!.PersonaName;
            }
            var ban = new PlayerBan(
                steamId,
                ip,
                name,
                reason,
                timeInt,
                serverId: Main.AdminApi.ThisServer.Id,
                banIp: true
            );
            ban.AdminId = adminId;
            await BlocksFunctions.Ban(ban);
        });
    }
}