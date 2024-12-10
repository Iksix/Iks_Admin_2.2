using CounterStrikeSharp.API.Core;
using IksAdminApi;
using Microsoft.Extensions.Localization;

namespace IksAdmin.Menus;

public static class AdminManageMenus
{
    static IIksAdminApi AdminApi = Main.AdminApi;
    static IStringLocalizer Localizer = AdminApi.Localizer;
    public static Dictionary<Admin, Admin> AddAdminBuffer = new();
    public static Dictionary<Admin, Admin> EditAdminBuffer = new();

    public static void OpenAdminManageMenu(CCSPlayerController caller, IDynamicMenu? backMenu = null)
    {
        var menu = AdminApi.CreateMenu(
            Main.GenerateMenuId("am"),
            Localizer["MenuTitle.AdminsManage"],
            titleColor: MenuColors.Gold
        );
        menu.BackAction = (p) => {
            OpenAdminManageMenu(caller);
        };

        menu.AddMenuOption(Main.GenerateOptionId("gm"), Localizer["MenuOption.GroupsManage"], (_, _) => {
            if (GroupsManageMenus.AddGroupBuffer.ContainsKey(caller.Admin()!))
            {
                GroupsManageMenus.AddGroupBuffer[caller.Admin()!] = new Group("ExampleGroup", "abc", 0);
            } else {
                GroupsManageMenus.AddGroupBuffer.Add(caller.Admin()!, new Group("ExampleGroup", "abc", 0));
            }
            GroupsManageMenus.OpenGroupsManageMenu(caller);
        }, 
        viewFlags: AdminUtils.GetAllPermissionGroupFlags("groups_manage"));
        
        menu.Open(caller);
    }

    // public static void OpenPlayersManageMenu(CCSPlayerController caller, IDynamicMenu? backMenu = null)
    // {
    //     var menu = AdminApi.CreateMenu(
    //         Main.GenerateMenuId("pm"), // айди (должно быть уникальным), в этом случае pm = players manage (управление игроками)
    //         Localizer["MenuTitle.PlayersManage"] // название из переводов
    //     );
    //     menu.BackAction = (p) => {
    //         // тут тоже реализация которую сделаю я
    //     };

    //     menu.AddMenuOption(Main.GenerateOptionId("pm_slay"), Localizer["MenuOption.Slay"], (_, _) => { // Пункт отвечающий за !slay к примеру
    //         // Какая то реализация которую я сделаю сам
    //     });
    //     menu.AddMenuOption(Main.GenerateOptionId("pm_kick"), Localizer["MenuOption.Kick"], (_, _) => { // Пункт отвечающий за !kick к примеру
    //         // Какая то реализация которую я сделаю сам
    //     });
        
    //     menu.Open(caller); // открытие меню
    // }

}