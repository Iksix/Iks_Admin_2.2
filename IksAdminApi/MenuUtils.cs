using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;

namespace IksAdminApi;

public static class MenuUtils
{
    static IIksAdminApi _api = AdminUtils.AdminApi;
    static IStringLocalizer _localizer = _api.Localizer;
    public static string GenerateMenuId(string id)
    {
        return $"iksadmin:menu:{id}";
    }
    public static string GenerateOptionId(string id)
    {
        return $"iksadmin:option:{id}";
    }
    public static void OpenSelectPlayer(CCSPlayerController caller, string idPrefix, Action<PlayerInfo, IDynamicMenu> action, bool includeBots = false, IDynamicMenu? backMenu = null)
    {
        var menu = _api.CreateMenu(
            GenerateMenuId(idPrefix + "_select_player"),
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
    public static void OpenSelectItem<T>(CCSPlayerController caller, string idPrefix, string parameter, List<T> objects, Action<T?, IDynamicMenu> action, IDynamicMenu? backMenu = null, bool nullOption = true)
    {
        var menu = _api.CreateMenu(
            GenerateMenuId(idPrefix + "_select_item"),
            _localizer["MenuTitle." + "HELP_SelectItem"],
            titleColor: MenuColors.Gold,
            backMenu: backMenu
        );
        if (nullOption)
        {
            menu.AddMenuOption("null", "Nothing", (_, _) =>
            {
                action.Invoke(default, menu);
            });
        }
        
        foreach (var obj in objects)
        {
            var title = obj!.GetType().GetProperty(parameter)!.GetValue(obj)!.ToString();
            menu.AddMenuOption(title!, title!, (_, _) =>
            {
                action.Invoke(obj, menu);
            });
        }

        menu.Open(caller);
    }
    public static void OpenSelectItem<T>(CCSPlayerController caller, string idPrefix, List<T> objects, Action<T?, IDynamicMenu> action, IDynamicMenu? backMenu = null, bool nullOption = true)
    {
        var menu = _api.CreateMenu(
            GenerateMenuId(idPrefix + "_select_item"),
            _localizer["MenuTitle." + "HELP_SelectItem"],
            titleColor: MenuColors.Gold,
            backMenu: backMenu
        );
        if (nullOption)
        {
            menu.AddMenuOption("null", "Nothing", (_, _) =>
            {
                action.Invoke(default, menu);
            });
        }
        
        foreach (var obj in objects)
        {
            menu.AddMenuOption(obj!.ToString()!, obj.ToString()!, (_, _) =>
            {
                action.Invoke(obj, menu);
            });
        }

        menu.Open(caller);
    }
}