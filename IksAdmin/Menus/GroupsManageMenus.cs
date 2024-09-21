using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using IksAdmin.Functions;
using IksAdminApi;
using Microsoft.Extensions.Localization;

namespace IksAdmin.Menus;

public static class GroupsManageMenus
{
    static IIksAdminApi AdminApi = Main.AdminApi;
    static IStringLocalizer Localizer = AdminApi.Localizer;
    public static Dictionary<Admin, Group> AddGroupBuffer = new();
    public static Dictionary<Admin, Group> EditGroupBuffer = new();
    public static void OpenGroupsManageMenu(CCSPlayerController caller)
    {
        var menu = AdminApi.CreateMenu(
            Main.GenerateMenuId("gm"),
            Localizer["MenuTitle.GroupsManage"],
            titleColor: MenuColors.Gold
        );
        menu.BackAction = (p) => {
            AdminManageMenus.OpenAdminManageMenu(caller);
        };
        
        menu.AddMenuOption(Main.GenerateOptionId("gm_add"), Localizer["MenuOption.GroupAdd"], (_, _) => {
            OpenGroupAddMenu(caller, menu);
        }, viewFlags: AdminApi.GetCurrentPermissionFlags("groups_manage_add"));

        menu.Open(caller);
    }

    private static void OpenGroupAddMenu(CCSPlayerController caller, IDynamicMenu backMenu)
    {
        var menu = AdminApi.CreateMenu(
            Main.GenerateMenuId("gm_add"),
            Localizer["MenuTitle.GroupAdding"],
            titleColor: MenuColors.Gold,
            backMenu: backMenu
        );
        var group = AddGroupBuffer[caller.Admin()!];
        menu.AddMenuOption(Main.GenerateOptionId("gm_add_name"), Localizer["MenuOption.GroupName"].Value.Replace("{value}", group.Name), (_, _) => {
            caller.PrintToChat("Введите название группы: !groupName");
            AdminApi.HookNextPlayerMessage(caller, msg => {
                group.Name = msg;
                OpenGroupAddMenu(caller, backMenu);
            });
        });
        menu.AddMenuOption(Main.GenerateOptionId("gm_add_flags"), Localizer["MenuOption.GroupFlags"].Value.Replace("{value}", group.Flags), (_, _) => {
            caller.PrintToChat("Введите флаги группы: !abcde");
            AdminApi.HookNextPlayerMessage(caller, msg => {
                group.Flags = msg;
                OpenGroupAddMenu(caller, backMenu);
            });
        });
        menu.AddMenuOption(Main.GenerateOptionId("gm_add_immunity"), Localizer["MenuOption.GroupImmunity"].Value.Replace("{value}", group.Immunity.ToString()), (_, _) => {
            caller.PrintToChat("Введите иммунитет группы: !10");
            AdminApi.HookNextPlayerMessage(caller, msg => {
                if (int.TryParse(msg, out var immunity))
                {
                    group.Immunity = immunity;
                    OpenGroupAddMenu(caller, backMenu);
                } else {
                    caller.PrintToChat("Иммунитет должен быть числом!");
                    OpenGroupAddMenu(caller, backMenu);
                }
            });
        });
        menu.AddMenuOption(Main.GenerateOptionId("gm_add_save"), Localizer["MenuOption.SaveGroup"], (_, _) => {
            caller.PrintToChat("Группа сохраняется...");
            OpenGroupsManageMenu(caller);
            Task.Run(async () => {
                await GroupsControllFunctions.AddGroup(group);
                Server.NextFrame(() => {
                    caller.PrintToChat("Группа сохранена ✔");
                });
            });
        });

        menu.Open(caller);
    }
}