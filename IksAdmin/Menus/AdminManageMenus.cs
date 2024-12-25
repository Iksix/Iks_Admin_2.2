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
        
        menu.AddMenuOption(Main.GenerateOptionId("am"), Localizer["MenuOption.GroupsManage"], (_, _) => {
            if (GroupsManageMenus.AddGroupBuffer.ContainsKey(caller.Admin()!))
            {
                GroupsManageMenus.AddGroupBuffer[caller.Admin()!] = new Group("ExampleGroup", "abc", 0);
            } else {
                GroupsManageMenus.AddGroupBuffer.Add(caller.Admin()!, new Group("ExampleGroup", "abc", 0));
            }
            GroupsManageMenus.OpenGroupsManageMenu(caller);
        }, 
        viewFlags: AdminUtils.GetAllPermissionGroupFlags("admins_manage"));

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
}