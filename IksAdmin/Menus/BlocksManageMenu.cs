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
            OpenAddOfflineBanMenu(caller);
        });
        menu.AddMenuOption(Main.GenerateOptionId("bm_remove"), Localizer["MenuOption.AddBan"], (_, _) => {
            // Some functions
        });
        menu.Open(caller);
    }
    public static void OpenAddOfflineBanMenu(CCSPlayerController caller)
    {
        var menu = AdminApi.CreateMenu(Main.GenerateMenuId("bm_offline_ban_add"), Localizer["MenuTitle.AddOfflineBan"]);
        menu.BackAction = p => {
            OpenBansMenu(caller);
        };
        var players = AdminApi.DisconnectedPlayers;
        foreach (var player in players)
        {
            var admin = caller.Admin();
            if (!AdminApi.CanDoActionWithPlayer(caller.GetSteamId()!, player.SteamId!))
                continue;
            menu.AddMenuOption(Main.GenerateOptionId("bm_offline_ban_add_" + player.SteamId!), player.PlayerName, (_, _) => {
                OpenSelectReasonMenu(caller, player);
            });
        }
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
        var menu = AdminApi.CreateMenu(Main.GenerateMenuId("bm_ban_reason"), Localizer["MenuTitle.SelectReason"]);
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

    private static void OpenTimeSelectMenu(CCSPlayerController caller, PlayerInfo target, string reason, IDynamicMenu? backMenu = null)
    {
        var menu = AdminApi.CreateMenu(Main.GenerateMenuId("bm_ban_time"), Localizer["MenuTitle.SelectReason"], backMenu: backMenu);
        var config = BansConfig.Config;
        var times = config.Times;
        var admin = caller.Admin()!;

        var ban = new PlayerBan(target, reason, 0, serverId: AdminApi.ThisServer.Id);

        if (admin.HasPermissions("blocks_manage.own_ban_time"))
        {
            menu.AddMenuOption(Main.GenerateOptionId("own_ban_time") ,Localizer["MenuOption.Other.OwnTime"], (_, _) => {
                Helper.Print(caller, Localizer["Message.PrintOwnTime"]);
                AdminApi.HookNextPlayerMessage(caller, time => {
                    if (!int.TryParse(time, out var timeInt))
                    {
                        Helper.Print(caller, Localizer["Error.MustBeANumber"]);
                        return;
                    }
                    ban.Duration = timeInt;
                    Helper.Print(caller, Localizer["ActionSuccess.TimeSetted"]);
                });
            });
        }

        foreach (var time in times)
        {
            if (caller.Admin()!.MaxBanTime != 0)
            {
                if (time.Key > caller.Admin()!.MaxBanTime)
                    continue;
            }
            if (caller.Admin()!.MinBanTime != 0)
            {
                if (time.Key < caller.Admin()!.MinBanTime)
                    continue;
            }

            menu.AddMenuOption(Main.GenerateOptionId("ban_time_" + time.Key), time.Value, (_, _) => {
                ban.Duration = time.Key;
                Task.Run(async () => {
                    await BansFunctions.Ban(ban);
                });
            });
        }
        
        menu.Open(caller);
    }
}