using System.Reflection;
using CounterStrikeSharp.API;
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
    public static Dictionary<Admin, List<int>> EditAdminServerIdBuffer = new();

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
            MenuUtils.OpenSelectPlayer(caller, "am_add", (t, m) =>
            {
                OpenAdminAddMenu(caller, t, m);
            }, backMenu: menu);
        });
        menu.AddMenuOption(Main.GenerateOptionId("edit_this_server"), _localizer["MenuOption.AM.Edit.ThisServer"], (_, _) =>
        {
            MenuUtils.OpenSelectItem<Admin?>(caller, "am_edit", "Name", _api.ServerAdmins!, (t, m) =>
            {
                var newAdmin = new Admin(t!.Id, t.SteamId, t.Name, t.Flags, t.Immunity, t.GroupId, t.Discord, t.Vk, t.Disabled, t.EndAt, t.CreatedAt, t.UpdatedAt, t.DeletedAt);
                EditAdminBuffer[caller.Admin()!] = newAdmin;
                EditAdminServerIdBuffer[caller.Admin()!] = newAdmin.Servers.ToList();
                OpenAdminEditMenu(caller, newAdmin, m);
            }, backMenu: menu, nullOption: false);
        });
        menu.AddMenuOption(Main.GenerateOptionId("edit_all"), _localizer["MenuOption.AM.Edit.All"], (_, _) =>
        {
            MenuUtils.OpenSelectItem<Admin?>(caller, "am_edit", "Name", _api.AllAdmins!, (t, m) =>
            {
                var newAdmin = new Admin(t!.Id, t.SteamId, t.Name, t.Flags, t.Immunity, t.GroupId, t.Discord, t.Vk, t.Disabled, t.EndAt, t.CreatedAt, t.UpdatedAt, t.DeletedAt);
                EditAdminBuffer[caller.Admin()!] = newAdmin;
                EditAdminServerIdBuffer[caller.Admin()!] = newAdmin.Servers.ToList();
                OpenAdminEditMenu(caller, newAdmin, m);
            }, backMenu: menu, nullOption: false);
        });
        menu.AddMenuOption(Main.GenerateOptionId("delete"), _localizer["MenuOption.AdminDelete"], (_, _) =>
        {
            MenuUtils.OpenSelectItem<Admin?>(caller, "am_delete", "Name", _api.ServerAdmins!, (t, m) =>
            {
                var cAdmin = caller.Admin();
                Task.Run(async () =>
                {
                    await _api.DeleteAdmin(cAdmin, t);
                    Server.NextFrame(() =>
                    {
                        OpenAdminManageMenuSection(caller, menu);
                    });
                });
            }, nullOption: false, backMenu: menu);
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

    private static void OpenAdminEditMenu(CCSPlayerController caller, Admin? admin, IDynamicMenu backMenu)
    {
        var menu = _api.CreateMenu(
            Main.GenerateMenuId("am_edit"),
            _localizer["MenuTitle." + "AM_edit"].AReplace(["name"], [admin!.Name]),
            titleColor: MenuColors.Gold,
            backMenu: backMenu
        );
        menu.AddMenuOption(Main.GenerateOptionId("name"), _localizer["MenuOption.AM.Name"].AReplace(["value"], [admin.Name]), (_, _) =>
        {}, disabled: true);
        menu.AddMenuOption(Main.GenerateOptionId("steam_id"), _localizer["MenuOption.AM.SteamId"].AReplace(["value"], [admin.SteamId]), (_, _) =>
        {}, disabled: true);
        menu.AddMenuOption(Main.GenerateOptionId("server_id"), _localizer["MenuOption.AM.ServerId"].AReplace(["value"], [string.Join(";", admin.Servers)]), (_, _) =>
        {
            OpenServerIdEditMenu(caller, admin, backMenu);
        });
        menu.AddMenuOption(Main.GenerateOptionId("flags"), _localizer["MenuOption.AM.Flags"].AReplace(["value"], [admin.CurrentFlags]), (_, _) =>
        {
            caller.Print(_localizer["Message.AM.FlagsSet"]);
            _api.HookNextPlayerMessage(caller, flags =>
            {
                admin.Flags = flags;
                OpenAdminEditMenu(caller, admin, backMenu);
            });
        }, disabled: admin.GroupId != null);
        menu.AddMenuOption(Main.GenerateOptionId("immunity"), _localizer["MenuOption.AM.Immunity"].AReplace(["value"], [admin.CurrentImmunity]), (_, _) =>
        {
            caller.Print(_localizer["Message.AM.ImmunitySet"]);
            _api.HookNextPlayerMessage(caller, str =>
            {
                if (!int.TryParse(str, out var immunity))
                {
                    caller.Print(_localizer["Error.MustBeANumber"]);
                    OpenAdminEditMenu(caller, admin, backMenu);
                    return;
                }
                admin.Immunity = immunity;
                OpenAdminEditMenu(caller, admin, backMenu);
            });
        }, disabled: admin.GroupId != null);
        menu.AddMenuOption(Main.GenerateOptionId("group"), _localizer["MenuOption.AM.Group"].AReplace(["value"], [admin.Group?.Name ?? ""]), (_, _) =>
        {
            var groups = _api.Groups;
            MenuUtils.OpenSelectItem<Group?>(caller, "am_add", "Name", groups!, (g, m) =>
            {
                admin.GroupId = g?.Id ?? null;
                admin.Immunity = null;
                admin.Flags = null;
                OpenAdminEditMenu(caller, admin, backMenu);
            }, backMenu: menu);
        });
        menu.AddMenuOption(Main.GenerateOptionId("vk"), _localizer["MenuOption.AM.Vk"].AReplace(["value"], [admin.Vk ?? ""]), (_, _) =>
        {
            caller.Print(_localizer["Message.AM.VkSet"]);
            _api.HookNextPlayerMessage(caller, str =>
            {
                if (str != "-")
                    admin.Vk = str;
                else admin.Vk = null;
                OpenAdminEditMenu(caller, admin, backMenu);
            });
        });
        menu.AddMenuOption(Main.GenerateOptionId("discord"), _localizer["MenuOption.AM.Discord"].AReplace(["value"], [admin.Discord ?? ""]), (_, _) =>
        {
            caller.Print(_localizer["Message.AM.DiscordSet"]);
            _api.HookNextPlayerMessage(caller, str =>
            {
                if (str != "-")
                    admin.Discord = str;
                else admin.Discord = null;
                OpenAdminEditMenu(caller, admin, backMenu);
            });
        });
        
        menu.AddMenuOption(Main.GenerateOptionId("save"), _localizer["MenuOption.AM.Save"], (_, _) =>
        {
            caller.Print(_localizer["Message.AM.AdminSave"]);
            var serverIds = EditAdminServerIdBuffer[caller.Admin()!];
            var cAdmin = caller.Admin()!;
            Task.Run(async () =>
            {
                await _api.RemoveServerIdsFromAdmin(admin.Id);
                foreach (var serverId in serverIds)
                {
                    await _api.AddServerIdToAdmin(admin.Id, serverId);
                }
                var result = await _api.UpdateAdmin(cAdmin, admin);
                if (result.QueryStatus < 0)
                {
                    caller.Print(_localizer["ActionError.Other"]);
                    _api.LogError(result.QueryMessage);
                    return;
                }
                caller.Print(_localizer["Message.AM.AdminSaved"]);
            });
        });
        menu.Open(caller);
    }

    private static void OpenServerIdEditMenu(CCSPlayerController caller, Admin admin, IDynamicMenu backMenu)
    {
        var menu = _api.CreateMenu(
            Main.GenerateMenuId("am_edit_server_id"),
            _localizer["MenuTitle." + "AM_edit_server_id"],
            titleColor: MenuColors.Gold
        );
        var serverIds = EditAdminServerIdBuffer[caller.Admin()!];
        menu.BackAction = (_) =>
        {
            OpenAdminEditMenu(caller, admin, backMenu);
        };

        foreach (var serverId in _api.AllServers.Select(x => x.Id))
        {
            bool adminHas = serverIds.Contains(serverId);
            var server = _api.GetServerById(serverId);
            if (server == null) continue;
            menu.AddMenuOption(serverId.ToString(), $"{server.Name} {(adminHas ? "+" : "-")}",
                (p, m) =>
                {
                    if (adminHas)
                    {
                        serverIds.Remove(serverId);
                    }
                    else
                    {
                        serverIds.Add(serverId);
                    }
                    OpenServerIdEditMenu(caller, admin, backMenu);
                });
        }
        
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
        
        menu.AddMenuOption(Main.GenerateOptionId("name"), _localizer["MenuOption.AM.Name"].AReplace(["value"], [target.PlayerName]), (_, _) =>
        {}, disabled: true);
        menu.AddMenuOption(Main.GenerateOptionId("steam_id"), _localizer["MenuOption.AM.SteamId"].AReplace(["value"], [target.SteamId!]), (_, _) =>
        {}, disabled: true);
        menu.AddMenuOption(Main.GenerateOptionId("flags"), _localizer["MenuOption.AM.Flags"].AReplace(["value"], [admin.CurrentFlags]), (_, _) =>
        {
            caller.Print(_localizer["Message.AM.FlagsSet"]);
            _api.HookNextPlayerMessage(caller, flags =>
            {
                admin.Flags = flags;
                OpenAdminAddMenu(caller, target, backMenu);
            });
        }, disabled: admin.GroupId != null);
        menu.AddMenuOption(Main.GenerateOptionId("immunity"), _localizer["MenuOption.AM.Immunity"].AReplace(["value"], [admin.CurrentImmunity]), (_, _) =>
        {
            caller.Print(_localizer["Message.AM.ImmunitySet"]);
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
        menu.AddMenuOption(Main.GenerateOptionId("group"), _localizer["MenuOption.AM.Group"].AReplace(["value"], [admin.Group?.Name ?? ""]), (_, _) =>
        {
            var groups = _api.Groups;
            MenuUtils.OpenSelectItem<Group?>(caller, "am_add", "Name", groups!, (g, m) =>
            {
                admin.GroupId = g?.Id ?? null;
                admin.Immunity = null;
                admin.Flags = null;
                OpenAdminAddMenu(caller, target, m);
            }, backMenu: menu);
        });
        menu.AddMenuOption(Main.GenerateOptionId("vk"), _localizer["MenuOption.AM.Vk"].AReplace(["value"], [admin.Vk ?? ""]), (_, _) =>
        {
            caller.Print(_localizer["Message.AM.VkSet"]);
            _api.HookNextPlayerMessage(caller, str =>
            {
                if (str != "-")
                    admin.Vk = str;
                else admin.Vk = null;
                OpenAdminAddMenu(caller, target, backMenu);
            });
        });
        menu.AddMenuOption(Main.GenerateOptionId("discord"), _localizer["MenuOption.AM.Discord"].AReplace(["value"], [admin.Discord ?? ""]), (_, _) =>
        {
            caller.Print(_localizer["Message.AM.DiscordSet"]);
            _api.HookNextPlayerMessage(caller, str =>
            {
                if (str != "-")
                    admin.Discord = str;
                else admin.Discord = null;
                OpenAdminAddMenu(caller, target, backMenu);
            });
        });
        
        menu.AddMenuOption(Main.GenerateOptionId("save"), _localizer["MenuOption.AM.Save"], (_, _) =>
        {
            caller.Print(_localizer["Message.AM.AdminSave"]);
            var cAdmin = caller.Admin()!;
            Task.Run(async () =>
            {
                var result = await _api.CreateAdmin(cAdmin, AddAdminBuffer[cAdmin!], _api.ThisServer.Id);
                if (result.QueryStatus < 0)
                {
                    caller.Print(_localizer["ActionError.Other"]);
                    _api.LogError(result.QueryMessage);
                    return;
                }
                caller.Print(_localizer["Message.AM.AdminSaved"]);
            });
        });

        menu.Open(caller);
    }

    

}