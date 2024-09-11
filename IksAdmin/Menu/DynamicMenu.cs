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
    public string Title {get; set;} = "Dynamic Menu";
    public MenuType Type {get; set;} = MenuType.Default;
    public Action<CCSPlayerController>? BackAction {get; set;} = null;
    public PostSelectAction PostSelectAction {get; set;} = PostSelectAction.Nothing;
    public event Action<CCSPlayerController, IMenu>? OnOpen;
    public List<IDynamicMenuOption> Options {get; set;} = new();
    public string UniqueID {get; set;}
    public DynamicMenu(string uniqueId, string title, MenuType type = (MenuType)3, PostSelectAction postSelectAction = PostSelectAction.Nothing, Action<CCSPlayerController>? backAction = null, IMenu? backMenu = null)
    {
        UniqueID = uniqueId;
        Title = title;
        Type = type;
        PostSelectAction = postSelectAction;
        BackAction = backAction;
        if (backMenu != null)
        {
            BackAction = player => backMenu.Open(player);
        }
    }

    public void Open(CCSPlayerController player)
    {
        var menu = Main.MenuApi.NewMenuForcetype(Title, Type, BackAction!);
        menu.PostSelectAction = PostSelectAction;
        menu.Open(player);
    }

    public void OptionTitle()
    {

    }

    public void AddMenuOption(string title, Action<CCSPlayerController, IDynamicMenuOption> onExecute)
    {
        throw new NotImplementedException();
    }
}

public class DynamicMenuOption : IDynamicMenuOption
{
    public string Title { get; set; } = "Option";
    public OptionColors Color { get; set; }
    public Action<CCSPlayerController, IDynamicMenuOption> OnExecute {get; set;}
    public bool Disabled { get; set; }
    public DynamicMenuOption(string title, Action<CCSPlayerController, IDynamicMenuOption> onExecute, OptionColors? color = null, bool disabled = false)
    {
        Title = title;
        Color = color ?? OptionColors.Default;
        OnExecute = onExecute;
        Disabled = disabled;
    }
}

