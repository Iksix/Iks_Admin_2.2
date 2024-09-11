using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Menu;
using MenuManager;
using IksAdminApi;
using CounterStrikeSharp.API.Modules.Utils;

namespace IksAdmin.Menu;

public class DynamicMenu : IDynamicMenu
{
    public string Id {get; set;}
    public string Title {get; set;} = "Dynamic Menu";
    public MenuType Type {get; set;} = MenuType.Default;
    public Action<CCSPlayerController>? BackAction {get; set;} = null;
    public PostSelectAction PostSelectAction {get; set;} = PostSelectAction.Nothing;
    public event Action<CCSPlayerController, IMenu>? OnOpen;
    public List<IDynamicMenuOption> Options {get; set;} = new();
    public DynamicMenu(string id, string title, MenuType type = (MenuType)3, PostSelectAction postSelectAction = PostSelectAction.Nothing, Action<CCSPlayerController>? backAction = null, IMenu? backMenu = null)
    {
        Id = id;
        Title = title;
        Type = type;
        PostSelectAction = postSelectAction;
        BackAction = backAction;
        if (backMenu != null)
        {
            BackAction = player => backMenu.Open(player);
        }

        Main.AdminApi.Debug($@"
            Menu created:
            Id: {Id}
            Title: {Title}
            Type: {Type}
        ");
    }

    public void Open(CCSPlayerController player, bool useSortMenu = true)
    {
        Main.AdminApi.Debug($@"
            Open menu... :
            Player: {player.PlayerName} | [{player.AuthorizedSteamID!.SteamId64}]
            Id: {Id}
            Title: {Title}
            Type: {Type}
            useSortMenu: {useSortMenu}
        ");
        var menu = Main.MenuApi.NewMenuForcetype(Title, Type, BackAction!);
        menu.PostSelectAction = PostSelectAction;

        if (useSortMenu)
        {
            var options = Options.ToList();
            if (Main.AdminApi.SortMenus.TryGetValue(Id, out var sortMenu))
            {
                Main.AdminApi.Debug("With sort menu");
                foreach (var sort in sortMenu)
                {
                    var option = options.First(x => x.Id == sort.Id);
                    if (!sort.View)
                    {
                        options.Remove(option);
                        continue;
                    }
                    menu.AddMenuOption(OptionTitle(option), (_, _) => {
                        option.OnExecute(player, option);
                    });
                    options.Remove(option);
                }
                foreach (var option in options)
                {
                    menu.AddMenuOption(OptionTitle(option), (_, _) => {
                        option.OnExecute(player, option);
                    });
                }
            } else {
                useSortMenu = false;
            }
        }
        if (!useSortMenu)
        {
            Main.AdminApi.Debug("Without sort menu");
            foreach (var option in Options)
            {
                menu.AddMenuOption(OptionTitle(option), (_, _) => {
                    option.OnExecute(player, option);
                });
            }
        }

        menu.Open(player);
    }

    public string OptionTitle(IDynamicMenuOption option)
    {
        return option.Title;
    }

    public void AddMenuOption(string id, string title, Action<CCSPlayerController, IDynamicMenuOption> onExecute, OptionColors? color = null, bool disabled = false)
    {
        if (Options.Any(x => x.Id == id))
        {
            throw new Exception("Option already exists");
        }
        Options.Add(new DynamicMenuOption(id, title, onExecute, color, disabled));
    }
}

public class DynamicMenuOption : IDynamicMenuOption
{
    public string Id {get; set;}
    public string Title { get; set; } = "Option";
    public OptionColors Color { get; set; }
    public Action<CCSPlayerController, IDynamicMenuOption> OnExecute {get; set;}
    public bool Disabled { get; set; }
    public DynamicMenuOption(string id, string title, Action<CCSPlayerController, IDynamicMenuOption> onExecute, OptionColors? color = null, bool disabled = false)
    {
        Id = id;
        Title = title;
        Color = color ?? OptionColors.Default;
        OnExecute = onExecute;
        Disabled = disabled;

        Main.AdminApi.Debug($@"
            Option created:
            Id: {id}
            Title: {title}
            Color: {color}
            Disabled: {disabled}
        ");
    }
}

