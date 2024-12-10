using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using IksAdmin.Menus;

namespace IksAdmin.Commands;

public static class BaseCommands
{
    public static AdminApi AdminApi = Main.AdminApi!;

    public static void AdminMenu(CCSPlayerController? caller, List<string> args, CommandInfo info)
    {
        AdminMenus.OpenAdminMenu(caller!);
    }
}