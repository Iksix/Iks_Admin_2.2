using CounterStrikeSharp.API.Core;
using IksAdminApi;
using Microsoft.Extensions.Localization;

namespace IksAdmin.Menus;

public static class MenuWarns
{
    private static IIksAdminApi _api = Main.AdminApi;
    private static IStringLocalizer _localizer = _api.Localizer;

    public static void OpenMain(CCSPlayerController caller, IDynamicMenu? backMenu = null)
    {
        var menu = _api.CreateMenu(
            id: Main.MenuId("main"),
            title: _localizer["MenuTitle.Warns.Main"],
            backMenu: backMenu
        );
        
        menu.AddMenuOption("add",  _localizer["MenuOption.Warns.Add"], (_, _) =>
        {
            MenuUtils.SelectItem<Admin?>(caller, "warn_add", "Name", 
                _api.ServerAdmins.Where(x => _api.CanDoActionWithPlayer(caller.GetSteamId(), x.SteamId)).ToList()!,
                (a, m) =>
                {
                    caller.Print(_localizer["Message.GL.ReasonSet"]);
                    _api.HookNextPlayerMessage(caller, reason =>
                    {
                        var warn = new Warn(caller.Admin()!.Id, a!.Id, 0, reason);
                        caller.Print(_localizer["Message.PrintOwnTime"]);
                        _api.HookNextPlayerMessage(caller, time =>
                        {
                            if (int.TryParse(time, out var timeInt))
                            {
                                warn.Duration = timeInt;
                                warn.SetEndAt();
                                Task.Run(async () =>
                                {
                                    await _api.CreateWarn(warn);
                                });
                            }
                            else
                            {
                                caller.Print(_localizer["Error.MustBeANumber"]);
                            }
                            m.Open(caller);
                        });
                    });
                },
                backMenu: menu
            );
        });
        menu.AddMenuOption("list",  _localizer["MenuOption.Warns.List"], (_, _) =>
        {
            MenuUtils.SelectItem<Admin?>(caller, "warn_list", "Name", 
                _api.ServerAdmins!.Where(x => x.Warns.Count > 0).ToList()!,
                (a, m) =>
                {
                    MsgOther.PrintWarns(caller, a!);
                },
                backMenu: menu
            );
        });
        menu.Open(caller);
    } 
}