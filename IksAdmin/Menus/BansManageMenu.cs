using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Menu;
using IksAdmin.Functions;
using IksAdminApi;
using Microsoft.Extensions.Localization;

namespace IksAdmin.Menus;

public static class BansManageMenu
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
        menu.AddMenuOption(Main.GenerateOptionId("bm_unban"), Localizer["MenuOption.Unban"], (_, _) => {
            Task.Run(async () => {
                var bans = await BansControllFunctions.GetLastBans(AdminApi.Config.LastPunishmentTime);
                Server.NextFrame(() => {
                    OpenRemoveBansMenu(caller, bans, menu);
                });
            });
        });
        menu.Open(caller);
    }

    private static void OpenRemoveBansMenu(CCSPlayerController caller, List<PlayerBan> bans, IDynamicMenu backMenu)
    {
        var menu = AdminApi.CreateMenu(Main.GenerateMenuId("bm_unban"), Localizer["MenuTitle.Unban"]);
        var admin = caller.Admin()!;
        foreach (var ban in bans)
        {
            bool isDisabled = false;
            if (!BansControllFunctions.CanUnban(admin, ban))
                isDisabled = true;

            menu.AddMenuOption(Main.GenerateOptionId("bm_unban_" + ban.SteamId), ban.NameString, (_, _) => {
                AdminApi.HookNextPlayerMessage(caller, r => {
                    Task.Run(async () => {
                        if (ban.BanIp == 0)
                            await BansFunctions.Unban(admin, ban.SteamId!, r);
                        else await BansFunctions.UnbanIp(admin, ban.Ip!, r);
                    });
                });
            }, disabled: isDisabled);
            
        }
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

        menu.AddMenuOption(Main.GenerateOptionId("own_ban_reason") ,Localizer["MenuOption.Other.OwnReason"], (_, _) => {
            Helper.Print(caller, Localizer["Message.PrintOwnReason"]);
            AdminApi.HookNextPlayerMessage(caller, reason => {
                OpenTimeSelectMenu(caller, target, reason, menu);
            });
        }, viewFlags: AdminUtils.GetCurrentPermissionFlags("blocks_manage.own_ban_reason"));

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
        ban.AdminId = admin.Id;
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
                OpenBanTypeSelectMenu(caller, ban);
            });
        }, viewFlags: AdminUtils.GetCurrentPermissionFlags("blocks_manage.own_ban_time"));

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
                AdminApi.CloseMenu(caller);
                ban.Duration = time.Key;
                OpenBanTypeSelectMenu(caller, ban);
            });
        }
        
        menu.Open(caller);
    }

    private static void OpenBanTypeSelectMenu(CCSPlayerController caller, PlayerBan ban)
    {
        var menu = AdminApi.CreateMenu(Main.GenerateMenuId("bm_ban_type"), Localizer["MenuTitle.Bans"]);
        var admin = caller.Admin();
        menu.AddMenuOption(Main.GenerateOptionId("bm_ban_steam_id"), Localizer["MenuOption.BanSteamId"], (_, _) => {
            AdminApi.CloseMenu(caller);
            Task.Run(async () => {
                await BansFunctions.Ban(ban);
            });
        });
            
        menu.AddMenuOption(Main.GenerateOptionId("bm_ban_ip"), Localizer["MenuOption.BanIp"], (_, _) => {
            AdminApi.CloseMenu(caller);
            ban.BanIp = 1;
            Task.Run(async () => {
                    await BansFunctions.Ban(ban);
                });
        }, viewFlags: AdminUtils.GetCurrentPermissionFlags("blocks_manage.ban_ip"));
        
        menu.Open(caller);
    }
}