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
            MenuUtils.SelectItem<CCSPlayerController?>(caller, "kick", "PlayerName", PlayersUtils.GetOnlinePlayers().Where(x => _api.CanDoActionWithPlayer(caller.GetSteamId(), x.GetSteamId())).ToList()!,
                (p, pmenu) => {
                    var reasons = KicksConfig.Config.Reasons;

                    if (caller.HasPermissions("players_manage.kick_own_reason"))
                        reasons.Insert(0, new KickReason(_localizer["MenuOption.Other.OwnReason"]));

                    MenuUtils.SelectItem<KickReason?>(caller, "kick", "Title", reasons!,
                        (reason, rmenu) => {

                            if (reason!.Title == _localizer["MenuOption.Other.OwnReason"]) {
                                caller.Print(_localizer["Message.PM.Kick.SetReason"].AReplace(["name"], [p!.PlayerName]));
                                _api.HookNextPlayerMessage(caller, s => {
                                    _api.Kick(caller.Admin()!, p!, s);
                                    OpenMain(caller, backMenu);
                                });
                                return;
                            }
                            _api.Kick(caller.Admin()!, p!, reason.Text);
                            OpenMain(caller, backMenu);
                        }, backMenu: pmenu, nullOption: false
                    );

                }, backMenu: menu, nullOption: false
            );
        }, viewFlags: AdminUtils.GetCurrentPermissionFlags("players_manage.kick"));

        menu.AddMenuOption("team", _localizer["MenuOption.PM.Team"], (_, _) => {

        }, viewFlags: AdminUtils.GetCurrentPermissionFlags("players_manage.changeteam") 
        + AdminUtils.GetCurrentPermissionFlags("players_manage.switchteam"));

        menu.AddMenuOption("slay", _localizer["MenuOption.PM.Slay"], (_, _) => {

        }, viewFlags: AdminUtils.GetCurrentPermissionFlags("players_manage.slay"));

        menu.AddMenuOption("respawn", _localizer["MenuOption.PM.Respawn"], (_, _) => {

        }, viewFlags: AdminUtils.GetCurrentPermissionFlags("players_manage.respawn"));
        
        
        menu.Open(caller);
    }

}