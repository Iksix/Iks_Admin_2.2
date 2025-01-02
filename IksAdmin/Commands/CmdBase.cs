using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using IksAdmin.Functions;
using IksAdmin.Menus;
using IksAdminApi;

namespace IksAdmin.Commands;

public static class CmdBase
{
    public static AdminApi AdminApi = Main.AdminApi!;

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
                await AdminApi.ReloadDataFromDb();
            else await AdminApi.ReloadDataFromDb(false);
            caller.Print( "DB data reloaded \u2714");
        });
    }
}