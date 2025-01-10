using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using IksAdmin.Functions;
using IksAdmin.Menus;
using IksAdminApi;

namespace IksAdmin.Commands;

public static class CmdPlayers
{
    public static AdminApi _api = Main.AdminApi;

    public static void Kick(CCSPlayerController? caller, List<string> args, CommandInfo info)
    {
        // css_kick <#uid/#steamId/name/@...> <reason>
        var identity = args[0];
        var reason = args[1];
        _api.DoActionWithIdentity(caller, identity,
            (target, _) =>
            {
                if (target!.IsBot || _api.CanDoActionWithPlayer(caller.GetSteamId(), target.GetSteamId()))
                {
                    _api.Kick(caller.Admin()!, target, reason);
                }
            },
            blockedArgs: AdminUtils.BlockedIdentifiers("css_kick")
        );
    }
    public static void Slay(CCSPlayerController? caller, List<string> args, CommandInfo info)
    {
        // css_slay <#uid/#steamId/name/@...>
        var identity = args[0];
        _api.DoActionWithIdentity(caller, identity,
            (target, _) =>
            {
                if (target!.PawnIsAlive)
                {
                    _api.Slay(caller.Admin()!, target);
                }
            },
            blockedArgs: AdminUtils.BlockedIdentifiers("css_slay")
        );
    }
    public static void Respawn(CCSPlayerController? caller, List<string> args, CommandInfo info)
    {
        // css_respawn <#uid/#steamId/name/@...> [alive also(true/false*)]
        var identity = args[0];
        _api.DoActionWithIdentity(caller, identity,
            (target, _) =>
            {
                if (!target!.PawnIsAlive || (args.Count > 1 && args[1] == "true"))
                {
                    _api.Respawn(caller.Admin()!, target);
                }
            },
            blockedArgs: AdminUtils.BlockedIdentifiers("css_respawn")
        );
    }
    public static void ChangeTeam(CCSPlayerController? caller, List<string> args, CommandInfo info)
    {
        // css_changeteam <#uid/#steamId/name/@...> <ct/t/spec>
        var identity = args[0];
        _api.DoActionWithIdentity(caller, identity,
            (target, _) =>
            {
                if (target!.PawnIsAlive)
                {
                    _api.Respawn(caller.Admin()!, target);
                }
            },
            blockedArgs: AdminUtils.BlockedIdentifiers("css_changeteam")
        );
    }
}