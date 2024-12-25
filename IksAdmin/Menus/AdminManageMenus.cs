using System.Reflection;
using CounterStrikeSharp.API.Core;
using IksAdmin.Functions;
using IksAdminApi;
using Microsoft.Extensions.Localization;

namespace IksAdmin.Menus;

public static class AdminManageMenus
{
    static IIksAdminApi _api = Main.AdminApi;
    static IStringLocalizer _localizer = _api.Localizer;
    public static Dictionary<Admin, Admin> AddAdminBuffer = new();
    public static Dictionary<Admin, Admin> EditAdminBuffer = new();

    public static void OpenAdminsControllMenu(CCSPlayerController caller, IDynamicMenu backMenu)
    {
        var menu = _api.CreateMenu(
            Main.GenerateMenuId("ac"),
            _localizer["MenuTitle.AdminsControll"],
            titleColor: MenuColors.Gold,
            backMenu: backMenu
        );
        menu.AddMenuOption(Main.GenerateOptionId("am"), _localizer["MenuOption.AdminsManage"], (_, _) => { 
                OpenAdminManageMenuSection(caller, menu);
        }, 
        viewFlags: AdminUtils.GetAllPermissionGroupFlags("admins_manage"));
        menu.AddMenuOption(Main.GenerateOptionId("gm"), _localizer["MenuOption.GroupsManage"], (_, _) => {
            if (GroupsManageMenus.AddGroupBuffer.ContainsKey(caller.Admin()!))
            {
                GroupsManageMenus.AddGroupBuffer[caller.Admin()!] = new Group("ExampleGroup", "abc", 0);
            } else {
                GroupsManageMenus.AddGroupBuffer.Add(caller.Admin()!, new Group("ExampleGroup", "abc", 0));
            }
            GroupsManageMenus.OpenGroupsManageMenu(caller, menu);
        }, 
        viewFlags: AdminUtils.GetAllPermissionGroupFlags("groups_manage"));
        
        menu.Open(caller);
    }

    public static void OpenAdminManageMenuSection(CCSPlayerController caller, IDynamicMenu backMenu)
    {
        var menu = _api.CreateMenu(
            Main.GenerateMenuId("am"),
            _localizer["MenuTitle." + "AdminsManage"],
            titleColor: MenuColors.Gold,
            backMenu: backMenu
        );

        menu.AddMenuOption(Main.GenerateOptionId("add"), _localizer["MenuOption.AdminAdd"], (_, _) =>
        {
            HelpMenus.OpenSelectPlayer(caller, "am_add", (t, m) =>
            {
                OpenAdminAddMenu(caller, t, m);
            }, backMenu: menu);
        });
        menu.AddMenuOption(Main.GenerateOptionId("edit"), _localizer["MenuOption.AdminEdit"], (_, _) =>
        {
                
        });
        menu.AddMenuOption(Main.GenerateOptionId("delete"), _localizer["MenuOption.AdminDelete"], (_, _) =>
        {
                
        });
        menu.AddMenuOption(Main.GenerateOptionId("reload"), _localizer["MenuOption.ReloadData"], (_, _) =>
        {
            Task.Run(async () =>
            {
                await _api.ReloadDataFromDBOnAllServers();
                caller.Print(_localizer["Message.AM.DataReloaded"]);
            });
        });
        

        menu.Open(caller);
    }

    public static void OpenAdminAddMenu(CCSPlayerController caller, PlayerInfo target, IDynamicMenu backMenu)
    {
        var menu = _api.CreateMenu(
            Main.GenerateMenuId("am_add"),
            _localizer["MenuTitle." + "AM_add"],
            titleColor: MenuColors.Gold,
            backMenu: backMenu
        );
        if (!AddAdminBuffer.ContainsKey(caller.Admin()!))
        {
            AddAdminBuffer[caller.Admin()!] = new Admin(
                target.SteamId!, 
                target.PlayerName
                );
        }

        var admin = AddAdminBuffer[caller.Admin()!];
        
        menu.AddMenuOption(Main.GenerateOptionId("name"), _localizer["MenuOption.AM.ADD.Name"].AReplace(["value"], [target.PlayerName]), (_, _) =>
        {}, disabled: true);
        menu.AddMenuOption(Main.GenerateOptionId("steam_id"), _localizer["MenuOption.AM.ADD.SteamId"].AReplace(["value"], [target.SteamId!]), (_, _) =>
        {}, disabled: true);
        menu.AddMenuOption(Main.GenerateOptionId("flags"), _localizer["MenuOption.AM.ADD.Flags"].AReplace(["value"], [admin.CurrentFlags]), (_, _) =>
        {
            caller.Print(_localizer["Message.AM.ADD.FlagsSet"]);
            _api.HookNextPlayerMessage(caller, flags =>
            {
                admin.Flags = flags;
                OpenAdminAddMenu(caller, target, backMenu);
            });
        }, disabled: admin.GroupId != null);
        menu.AddMenuOption(Main.GenerateOptionId("immunity"), _localizer["MenuOption.AM.ADD.Immunity"].AReplace(["value"], [admin.CurrentImmunity]), (_, _) =>
        {
            caller.Print(_localizer["Message.AM.ADD.ImmunitySet"]);
            _api.HookNextPlayerMessage(caller, str =>
            {
                if (!int.TryParse(str, out var immunity))
                {
                    caller.Print(_localizer["Error.MustBeANumber"]);
                    OpenAdminAddMenu(caller, target, backMenu);
                    return;
                }
                admin.Immunity = immunity;
                OpenAdminAddMenu(caller, target, backMenu);
            });
        }, disabled: admin.GroupId != null);
        menu.AddMenuOption(Main.GenerateOptionId("group"), _localizer["MenuOption.AM.ADD.Group"].AReplace(["value"], [admin.Group?.Name ?? ""]), (_, _) =>
        {
            var groups = _api.Groups;
            HelpMenus.OpenSelectItem<Group?>(caller, "am_add", "Name", groups!, g =>
            {
                admin.GroupId = g?.Id ?? null;
                admin.Immunity = null;
                admin.Flags = null;
                OpenAdminAddMenu(caller, target, backMenu);
            }, backMenu: menu);
        });
        // menu.AddMenuOption(Main.GenerateOptionId("delete"), _localizer["MenuOption.AM.ADD."], (_, _) =>
        // {
        //         
        // });
        // menu.AddMenuOption(Main.GenerateOptionId("delete"), _localizer["MenuOption.AdminDelete"], (_, _) =>
        // {
        //         
        // });
        // menu.AddMenuOption(Main.GenerateOptionId("delete"), _localizer["MenuOption.AdminDelete"], (_, _) =>
        // {
        //         
        // });
        

        menu.Open(caller);
    }

    

}