using System.Security.Cryptography.X509Certificates;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using IksAdminApi;
using Microsoft.Extensions.Localization;

namespace IksAdmin.Menus;

public static class MenuPM
{
    private static IIksAdminApi _api = Main.AdminApi;
    private static IStringLocalizer _localizer = _api.Localizer;

    public static void OpenMain(CCSPlayerController caller, IDynamicMenu? backMenu = null)
    {
        var menu = _api.CreateMenu(
            id: Main.MenuId("pm_main"),
            title: _localizer["MenuTitle.PM.Main"],
            backMenu: backMenu
        );
        
        menu.AddMenuOption("kick", _localizer["MenuOption.PM.Kick"], (_, _) => {
            OpenKickMenu(caller, menu);
        }, viewFlags: AdminUtils.GetCurrentPermissionFlags("players_manage.kick"));

        menu.AddMenuOption("team", _localizer["MenuOption.PM.Team"], (_, _) => {
            OpenTeamMenu(caller, menu);
        }, viewFlags: AdminUtils.GetCurrentPermissionFlags("players_manage.changeteam") 
        + AdminUtils.GetCurrentPermissionFlags("players_manage.switchteam"));

        menu.AddMenuOption("slay", _localizer["MenuOption.PM.Slay"], (_, _) => {
            OpenSlayMenu(caller, menu);
        }, viewFlags: AdminUtils.GetCurrentPermissionFlags("players_manage.slay"));

        menu.AddMenuOption("respawn", _localizer["MenuOption.PM.Respawn"], (_, _) => {
            OpenRespawnMenu(caller, menu);
        }, viewFlags: AdminUtils.GetCurrentPermissionFlags("players_manage.respawn"));
        
        
        menu.Open(caller);
    }

    private static void OpenTeamMenu(CCSPlayerController caller, IDynamicMenu menu)
    {
        throw new NotImplementedException();
    }

    private static void OpenKickMenu(CCSPlayerController caller, IDynamicMenu menu)
    {
        MenuUtils.SelectItem<CCSPlayerController?>(caller, "kick", "PlayerName", PlayersUtils.GetOnlinePlayers().Where(x => _api.CanDoActionWithPlayer(caller.GetSteamId(), x.GetSteamId())).ToList()!,
                (p, pmenu) => {
                    var reasons = KicksConfig.Config.Reasons;

                    if (caller.HasPermissions("players_manage.kick_own_reason"))
                        reasons.Insert(0, new KickReason(_localizer["MenuOption.Other.OwnReason"]));

                    MenuUtils.SelectItem<KickReason?>(caller, "kick_reason", "Title", reasons!,
                        (reason, rmenu) => {

                            if (reason!.Title == _localizer["MenuOption.Other.OwnReason"]) {
                                caller.Print(_localizer["Message.PM.Kick.SetReason"].AReplace(["name"], [p!.PlayerName]));
                                _api.HookNextPlayerMessage(caller, s => {
                                    _api.Kick(caller.Admin()!, p!, s);
                                    OpenKickMenu(caller, menu);
                                });
                                return;
                            }
                            _api.Kick(caller.Admin()!, p!, reason.Text);
                            Server.NextFrame(() => {
                                OpenKickMenu(caller, menu);
                            });
                        }, backMenu: pmenu, nullOption: false
                    );

                }, backMenu: menu, nullOption: false
            );
    }
    private static void OpenSlayMenu(CCSPlayerController caller, IDynamicMenu menu)
    {
        MenuUtils.SelectItem<CCSPlayerController?>(caller, "slay", "PlayerName", PlayersUtils.GetOnlinePlayers(true).Where(x => (x.IsBot || _api.CanDoActionWithPlayer(caller.GetSteamId(), x.GetSteamId())) && x.PawnIsAlive).ToList()!,
            (p, pmenu) => {
                if (p!.PawnIsAlive)
                    _api.Slay(caller.Admin()!, p!);
                _api.Plugin.AddTimer(0.1f, () => {
                    OpenSlayMenu(caller, menu);
                });
            }, backMenu: menu, nullOption: false
        );
    }
    private static void OpenRespawnMenu(CCSPlayerController caller, IDynamicMenu menu)
    {
        MenuUtils.SelectItem<CCSPlayerController?>(caller, "respawn", "PlayerName", PlayersUtils.GetOnlinePlayers(true).Where(x => (x.IsBot || _api.CanDoActionWithPlayer(caller.GetSteamId(), x.GetSteamId())) && !x.PawnIsAlive).ToList()!,
            (p, pmenu) => {
                if (!p!.PawnIsAlive)
                    _api.Respawn(caller.Admin()!, p!);
                _api.Plugin.AddTimer(0.1f, () => {
                    OpenRespawnMenu(caller, menu);
                });
            }, backMenu: menu, nullOption: false
        );
    }
}