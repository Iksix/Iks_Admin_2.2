using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using CounterStrikeSharp.API.Core;
using IksAdminApi;
using Microsoft.Extensions.Localization;

namespace IksAdmin.Menus;

public static class AdminManageMenus
{
    static IIksAdminApi AdminApi = Main.AdminApi;
    static IStringLocalizer Localizer = AdminApi.Localizer;

    public static void OpenAdminManageMenu(CCSPlayerController caller)
    {
        var menu = AdminApi.CreateMenu(
            Main.GenerateMenuId("admin_manage"),
            Localizer["MenuTitle.AdminManage"],
            titleColor: MenuColors.Gold
        );

        menu.AddMenuOption(Main.GenerateOptionId("am_add"), Localizer["MenuOption.AdminAdd"], (_, _) => {
            OpenAdminAddMenu(caller, menu);
        }, viewFlags: AdminApi.GetCurrentPermissionFlags("admin_manage_add"), color: MenuColors.Lime);
        menu.AddMenuOption(Main.GenerateOptionId("am_delete"), Localizer["MenuOption.AdminDelete"], (_, _) => {
            OpenAdminAddMenu(caller, menu);
        }, viewFlags: AdminApi.GetCurrentPermissionFlags("admin_manage_delete"), color: MenuColors.Red);
        menu.AddMenuOption(Main.GenerateOptionId("am_edit"), Localizer["MenuOption.AdminEdit"], (_, _) => {
            OpenAdminAddMenu(caller, menu);
        }, viewFlags: AdminApi.GetCurrentPermissionFlags("admin_manage_edit"), color: MenuColors.Gold);
        menu.AddMenuOption(Main.GenerateOptionId("am_refresh"), Localizer["MenuOption.AdminRefresh"], (_, _) => {
            OpenAdminAddMenu(caller, menu);
        }, viewFlags: AdminApi.GetCurrentPermissionFlags("admin_manage_refresh"), color: MenuColors.Gold);
        
        menu.Open(caller);
    }

    private static void OpenAdminAddMenu(CCSPlayerController caller, IDynamicMenu backMenu)
    {
        var menu = AdminApi.CreateMenu(
            Main.GenerateMenuId("admin_manage"),
            Localizer["MenuTitle.AdminAdd"],
            titleColor: MenuColors.Lime,
            backMenu: backMenu
        );

        var players = AdminUtils.GetOnlinePlayers();
        foreach(var player in players)
        {
            var info = new PlayerInfo(player);
            menu.AddMenuOption(Main.GenerateOptionId(info.SteamId), info.PlayerName, (_, _) => {
                OpenAdminAddPlayerMenu(caller, info, menu);
            });
        }

        menu.Open(caller);
    }

    private static void OpenAdminAddPlayerMenu(CCSPlayerController caller, PlayerInfo target, IDynamicMenu backMenu)
    {
        var menu = AdminApi.CreateMenu(
            Main.GenerateMenuId("admin_manage"),
            Localizer["MenuTitle.Ad"],
            titleColor: MenuColors.Gold,
            backMenu: backMenu
        );

        var players = AdminUtils.GetOnlinePlayers();
        foreach(var player in players)
        {
            var info = new PlayerInfo(player);
            menu.AddMenuOption(Main.GenerateOptionId(info.SteamId), info.PlayerName, (_, _) => {
                OpenAdminAddPlayerMenu(caller, info, menu);
            });
        }

        menu.Open(caller);
    }
}