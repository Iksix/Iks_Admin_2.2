using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using IksAdmin.Functions;
using IksAdminApi;
using Microsoft.Extensions.Localization;

namespace IksAdmin.Menus;

public static class GroupsManageMenus
{
    static IIksAdminApi _api = Main.AdminApi;
    static IStringLocalizer Localizer = _api.Localizer;
    public static Dictionary<Admin, Group> AddGroupBuffer = new();
    public static Dictionary<Admin, Group> EditGroupBuffer = new();
    public static void OpenGroupsManageMenu(CCSPlayerController caller, IDynamicMenu backMenu)
    {
        var menu = _api.CreateMenu(
            Main.GenerateMenuId("gm"),
            Localizer["MenuTitle.GroupsManage"],
            titleColor: MenuColors.Gold,
            backMenu: backMenu
        );
        
        menu.AddMenuOption("add", Localizer["MenuOption.GroupAdd"], (_, _) => {
            OpenGroupAddMenu(caller, menu);
        }, viewFlags: _api.GetCurrentPermissionFlags("groups_manage.add"));
        menu.AddMenuOption("edit", Localizer["MenuOption.GroupEdit"], (_, _) => {
            MenuUtils.SelectItem<Group?>(caller, "group_edit", "Name", _api.Groups!, (g, m) =>
            {
                var newGroup = new Group(g.Id, g.Name, g.Flags, g.Immunity, g.Comment);
                OpenGroupEditMenu(caller, newGroup, m);
            }, nullOption: false, backMenu: menu);
        }, viewFlags: _api.GetCurrentPermissionFlags("groups_manage.edit"));
        menu.AddMenuOption("delete", Localizer["MenuOption.GroupDelete"], (_, _) => {
            OpenGroupAddMenu(caller, menu);
        }, viewFlags: _api.GetCurrentPermissionFlags("groups_manage.delete"));

        menu.Open(caller);
    }

    private static void OpenGroupEditMenu(CCSPlayerController caller, Group group, IDynamicMenu backMenu)
    {
        var menu = _api.CreateMenu(
            Main.GenerateMenuId("gm_edit"),
            Localizer["MenuTitle.GroupEditing"],
            titleColor: MenuColors.Gold,
            backMenu: backMenu
        );
        
        menu.AddMenuOption("name", Localizer["MenuOption.GroupName"].Value.Replace("{value}", group.Name), (_, _) => {
            caller.Print("Введите название группы: !groupName");
            _api.HookNextPlayerMessage(caller, msg => {
                group.Name = msg;
                OpenGroupEditMenu(caller, group, backMenu);
            });
        });
        menu.AddMenuOption("flags", Localizer["MenuOption.GroupFlags"].Value.Replace("{value}", group.Flags), (_, _) => {
            caller.Print("Введите флаги группы: !abcde");
            _api.HookNextPlayerMessage(caller, msg => {
                group.Flags = msg;
                OpenGroupEditMenu(caller, group, backMenu);
            });
        });
        menu.AddMenuOption("immunity", Localizer["MenuOption.GroupImmunity"].Value.Replace("{value}", group.Immunity.ToString()), (_, _) => {
            caller.Print("Введите иммунитет группы: !10");
            _api.HookNextPlayerMessage(caller, msg => {
                if (int.TryParse(msg, out var immunity))
                {
                    group.Immunity = immunity;
                    OpenGroupEditMenu(caller, group, backMenu);
                } else {
                    caller.Print("Иммунитет должен быть числом!");
                    OpenGroupEditMenu(caller, group, backMenu);
                }
            });
        });
        menu.AddMenuOption("comment", Localizer["MenuOption.GroupComment"].Value.Replace("{value}", group.Immunity.ToString()), (_, _) => {
            caller.Print("Введите комментарий для группы: !Низ пищевой цепочки");
            _api.HookNextPlayerMessage(caller, msg => {
                group.Comment = msg;
                OpenGroupEditMenu(caller, group, backMenu);
            });
        });
        menu.AddMenuOption("save", Localizer["MenuOption.SaveGroup"], (_, _) => {
            caller.Print("Группа сохраняется...");
            backMenu.Open(caller);
            Task.Run(async () =>
            {
                await _api.UpdateGroup(group);
                Server.NextFrame(() => {
                    caller.Print("Группа сохранена ✔");
                });
            });
        });
        
        menu.Open(caller);
    }

    private static void OpenGroupAddMenu(CCSPlayerController caller, IDynamicMenu backMenu)
    {
        var menu = _api.CreateMenu(
            Main.GenerateMenuId("gm_add"),
            Localizer["MenuTitle.GroupAdding"],
            titleColor: MenuColors.Gold,
            backMenu: backMenu
        );
        var group = AddGroupBuffer[caller.Admin()!];
        menu.AddMenuOption("name", Localizer["MenuOption.GroupName"].Value.Replace("{value}", group.Name), (_, _) => {
            caller.Print("Введите название группы: !groupName");
            _api.HookNextPlayerMessage(caller, msg => {
                group.Name = msg;
                OpenGroupAddMenu(caller, backMenu);
            });
        });
        menu.AddMenuOption("flags", Localizer["MenuOption.GroupFlags"].Value.Replace("{value}", group.Flags), (_, _) => {
            caller.Print("Введите флаги группы: !abcde");
            _api.HookNextPlayerMessage(caller, msg => {
                group.Flags = msg;
                OpenGroupAddMenu(caller, backMenu);
            });
        });
        menu.AddMenuOption("immunity", Localizer["MenuOption.GroupImmunity"].Value.Replace("{value}", group.Immunity.ToString()), (_, _) => {
            caller.Print("Введите иммунитет группы: !10");
            _api.HookNextPlayerMessage(caller, msg => {
                if (int.TryParse(msg, out var immunity))
                {
                    group.Immunity = immunity;
                    OpenGroupAddMenu(caller, backMenu);
                } else {
                    caller.Print("Иммунитет должен быть числом!");
                    OpenGroupAddMenu(caller, backMenu);
                }
            });
        });
        menu.AddMenuOption("comment", Localizer["MenuOption.GroupComment"].Value.Replace("{value}", group.Immunity.ToString()), (_, _) => {
            caller.Print("Введите комментарий для группы: !Низ пищевой цепочки");
            _api.HookNextPlayerMessage(caller, msg => {
                group.Comment = msg;
                OpenGroupAddMenu(caller, backMenu);
            });
        });
        menu.AddMenuOption("save", Localizer["MenuOption.SaveGroup"], (_, _) => {
            caller.Print("Группа сохраняется...");
            backMenu.Open(caller);
            Task.Run(async () =>
            {
                await _api.CreateGroup(group);
                Server.NextFrame(() => {
                    caller.Print("Группа сохранена ✔");
                });
            });
        });

        menu.Open(caller);
    }
}