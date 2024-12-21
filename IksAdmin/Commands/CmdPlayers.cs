using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using IksAdmin.Functions;
using IksAdmin.Menus;
using IksAdminApi;

namespace IksAdmin.Commands;

public static class CmdPlayers
{
    public static AdminApi AdminApi = Main.AdminApi;

    public static void Kick(CCSPlayerController? caller, List<string> args, CommandInfo info)
    {
        var identity = args[0];
        var reason = args[1];
        AdminApi.DoActionWithIdentity(caller, identity,
            target =>
            {
                if (target.IsBot || AdminApi.CanDoActionWithPlayer(caller.GetSteamId(), target.GetSteamId()))
                {
                    AdminApi.Kick(caller.Admin()!, target, reason);
                }
            }
            );
    }
}