using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using IksAdmin.Functions;
using IksAdmin.Menus;

namespace IksAdmin.Commands;

public static class CmdBase
{
    public static AdminApi AdminApi = Main.AdminApi!;

    public static void AdminMenu(CCSPlayerController? caller, List<string> args, CommandInfo info)
    {
        AdminMenus.OpenAdminMenu(caller!);
    }

    public static void Reload(CCSPlayerController? caller, List<string> args, CommandInfo info)
    {
        Helper.Reply(info, "Reloading DB data...");
        Task.Run(async () =>
        {
            await AdminApi.ReloadDataFromDb();
            Helper.Reply(info, "DB data reloaded \u2714");
        });
    }
}