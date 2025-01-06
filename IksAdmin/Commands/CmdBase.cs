using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using IksAdmin.Functions;
using IksAdmin.Menus;
using IksAdminApi;

namespace IksAdmin.Commands;

public static class CmdBase
{
    public static AdminApi _api = Main.AdminApi!;

    public static void AdminMenu(CCSPlayerController? caller, List<string> args, CommandInfo info)
    {
        MenuMain.OpenAdminMenu(caller!);
    }

    public static void Reload(CCSPlayerController? caller, List<string> args, CommandInfo info)
    {
        caller.Print("Reloading DB data...");
        Task.Run(async () =>
        {
            if (args.Count > 0 && args[0] == "all")
                await _api.ReloadDataFromDb();
            else await _api.ReloadDataFromDb(false);
            caller.Print( "DB data reloaded \u2714");
        });
    }

    public static void ReloadInfractions(CCSPlayerController? caller, List<string> args, CommandInfo info)
    {
        var steamId = args[0];
        var player = PlayersUtils.GetControllerBySteamId(steamId) ?? PlayersUtils.GetControllerByIp(steamId);
        if (player == null) return;
        string? ip = player.GetIp();
        Task.Run(async () =>
        {
            await _api.ReloadInfractions(steamId, ip);
        });
    }
}