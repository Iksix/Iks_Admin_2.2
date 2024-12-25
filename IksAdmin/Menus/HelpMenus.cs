using CounterStrikeSharp.API.Core;
using IksAdmin.Menu;
using IksAdminApi;
using Microsoft.Extensions.Localization;

namespace IksAdmin.Menus;

public static class HelpMenus
{
    static IIksAdminApi _api = Main.AdminApi;
    static IStringLocalizer _localizer = _api.Localizer;
    
    public static void OpenSelectPlayer(CCSPlayerController caller, string idPrefix, Action<PlayerInfo, IDynamicMenu> action, bool includeBots = false, IDynamicMenu? backMenu = null)
    {
        var menu = _api.CreateMenu(
            Main.GenerateMenuId(idPrefix + "_select_player"),
            _localizer["MenuTitle." + "HELP_SelectPlayer"],
            titleColor: MenuColors.Gold,
            backMenu: backMenu
        );

        var players = PlayersUtils.GetOnlinePlayers(includeBots);

        foreach (var player in players)
        {
            var p = new PlayerInfo(player);
            menu.AddMenuOption(p.SteamId!, p.PlayerName, (_, _) =>
            {
                action.Invoke(p, menu);
            });
        }

        menu.Open(caller);
    }
    public static void OpenSelectItem<T>(CCSPlayerController caller, string idPrefix, string parameter, List<T> objects, Action<T?> action, bool includeBots = false, IDynamicMenu? backMenu = null)
    {
        var menu = _api.CreateMenu(
            Main.GenerateMenuId(idPrefix + "_select_item"),
            _localizer["MenuTitle." + "HELP_SelectItem"],
            titleColor: MenuColors.Gold,
            backMenu: backMenu
        );
        menu.AddMenuOption("null", "Nothing", (_, _) =>
        {
            action.Invoke(default);
        });
        foreach (var obj in objects)
        {
            var title = obj!.GetType().GetProperty(parameter)!.GetValue(obj)!.ToString();
            menu.AddMenuOption(title!, title!, (_, _) =>
            {
                action.Invoke(obj);
            });
        }

        menu.Open(caller);
    }
}