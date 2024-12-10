using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Menu;
using IksAdminApi;
using Microsoft.Extensions.Localization;

namespace IksAdmin.Menus;

public static class AdminMenus
{
    static IIksAdminApi AdminApi = Main.AdminApi;
    static IStringLocalizer Localizer = AdminApi.Localizer;
   
    public static void OpenAdminMenu(CCSPlayerController caller, IDynamicMenu? backMenu = null) {
        var menu = AdminApi.CreateMenu(
            id: Main.GenerateMenuId("main"),
            title: Localizer["MenuTitle.AdminMain"],
            backMenu: backMenu
        );
        menu.AddMenuOption(
            id: Main.GenerateOptionId("am"),
            title: Localizer["MenuOption.AdminsManage"],
            (p, _) => {
                AdminManageMenus.OpenAdminManageMenu(caller, menu);
            },
            viewFlags: AdminUtils.GetAllPermissionGroupFlags("admins_manage")
        );
        menu.AddMenuOption(
            id: Main.GenerateOptionId("bm"),
            title: Localizer["MenuOption.BlocksManage"],
            (p, _) => {
                OpenBlocksManageMenu(caller, menu);
            },
            viewFlags: AdminUtils.GetAllPermissionGroupFlags("blocks_manage")
        );
        menu.Open(caller);
    }

    public static void OpenBlocksManageMenu(CCSPlayerController caller, IDynamicMenu? backMenu = null)
    {
        var menu = AdminApi.CreateMenu(
            id: Main.GenerateMenuId("bm"),
            title: Localizer["MenuTitle.BlocksManage"],
            backMenu: backMenu
        );
        menu.AddMenuOption(
            id: Main.GenerateOptionId("bans"),
            title: Localizer["MenuOption.BansManage"],
            (p, _) => {
                BansManageMenu.OpenBansMenu(caller, menu);
            }
        );
        menu.AddMenuOption(
            id: Main.GenerateOptionId("bm"),
            title: Localizer["MenuOption.BlocksManage"],
            (p, _) => {
                OpenBlocksManageMenu(caller, menu);
            }
        );
        menu.Open(caller);
    }
}