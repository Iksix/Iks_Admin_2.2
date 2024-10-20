using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Menu;
using IksAdmin.Functions;
using IksAdminApi;
using Microsoft.Extensions.Localization;

namespace IksAdmin.Menus;

public static class BlocksManageMenu
{
    static IIksAdminApi AdminApi {get; set;} = Main.AdminApi;
    static IStringLocalizer Localizer {get; set;} = Main.AdminApi.Localizer;

    public static void OpenBansMenu(CCSPlayerController caller)
    {
        var menu = AdminApi.CreateMenu(Main.GenerateMenuId("bm_ban"), Localizer["MenuTitle.Bans"]);
        menu.AddMenuOption(Main.GenerateOptionId("bm_add_ban"), Localizer["MenuOption.AddBan"], (_, _) => {
            OpenAddBanMenu(caller);
        });
        menu.AddMenuOption(Main.GenerateOptionId("bm_add_offline_ban"), Localizer["MenuOption.AddBan"], (_, _) => {
            // Some functions
        });
        menu.AddMenuOption(Main.GenerateOptionId("bm_remove"), Localizer["MenuOption.AddBan"], (_, _) => {
            // Some functions
        });
        menu.Open(caller);
    }
    public static void OpenAddBanMenu(CCSPlayerController caller)
    {
        var menu = AdminApi.CreateMenu(Main.GenerateMenuId("bm_ban_add"), Localizer["MenuTitle.AddBan"]);
        menu.BackAction = p => {
            OpenBansMenu(caller);
        };
        var players = AdminUtils.GetOnlinePlayers();
        foreach (var player in players)
        {
            var admin = caller.Admin();
            if (!AdminApi.CanDoActionWithPlayer(caller.GetSteamId()!, player.AuthorizedSteamID!.SteamId64.ToString()))
                continue;
            menu.AddMenuOption(Main.GenerateOptionId("bm_ban_add_" + player.GetSteamId()), player.PlayerName, (_, _) => {
                OpenSelectReasonMenu(caller, new PlayerInfo(player));
            });
        }
        menu.Open(caller);
    }

    private static void OpenSelectReasonMenu(CCSPlayerController caller, PlayerInfo target)
    {
        var menu = AdminApi.CreateMenu(Main.GenerateMenuId("bm_ban"), Localizer["MenuTitle.AddBan"]);
        var config = BansConfig.Config;
        var reasons = config.Reasons;
        var admin = caller.Admin()!;

        if (admin.HasPermissions("blocks_manage.own_ban_reason"))
        {
            menu.AddMenuOption(Main.GenerateOptionId("own_ban_reason") ,Localizer["MenuOption.Other.OwnReason"], (_, _) => {
                Helper.Print(caller, Localizer["Message.PrintOwnReason"]);
                AdminApi.HookNextPlayerMessage(caller, reason => {
                    OpenTimeSelectMenu(caller, target, reason, menu);
                });
            });
        }

        foreach (var reason in reasons)
        {
            if (reason.Duration != null)
            {
                if (caller.Admin()!.MaxBanTime != 0)
                {
                    if (reason.Duration > caller.Admin()!.MaxBanTime)
                        continue;
                }
                if (caller.Admin()!.MinBanTime != 0)
                {
                    if (reason.Duration < caller.Admin()!.MinBanTime)
                        continue;
                }
            }

            menu.AddMenuOption(Main.GenerateOptionId(reason.Title), reason.Title, (_, _) => {
                if (reason.Duration == null)
                {
                    OpenTimeSelectMenu(caller, target, reason.Text, menu);
                }
            });
        }
        
        menu.Open(caller);
    }

    private static void OpenTimeSelectMenu(CCSPlayerController caller, PlayerInfo target, string reason, IDynamicMenu menu)
    {
        
    }
}