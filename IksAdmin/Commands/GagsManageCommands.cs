
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;

namespace IksAdmin.Commands;


public class GagsManageCommands
{
    public static void Gag(CCSPlayerController? caller, List<string> args, CommandInfo info)
    {
        //css_gag <#uid/#sid/name/@...> <time> <reason>
        var identity = args[0];
        var time = int.Parse(args[1]);
        var reason = string.Join(" ", args.Skip(2));
        Main.AdminApi.DoActionWithIdentity(caller, identity, target =>
        {
        });
    }
}