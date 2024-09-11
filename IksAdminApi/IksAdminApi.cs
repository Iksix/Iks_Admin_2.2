namespace IksAdminApi;

using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Utils;
using MenuManager;

public interface IIksAdminApi
{
    // GLOBALS ===
    public IAdminConfig Config { get; set; }
    // MENU ===
    public IDynamicMenu CreateMenu(string title, MenuType type = (MenuType)3, PostSelectAction postSelectAction = PostSelectAction.Nothing, Action<CCSPlayerController>? backAction = null);
    public void CloseMenu(CCSPlayerController player);
    // FUNC ===
    public void Debug(string message);
}

public interface IAdminConfig 
{
    public bool DebugMode { get; set; }
    public int MenuType { get; set; }
}
public interface IDynamicMenu
{
    public string Title {get; set;}
    public MenuType Type {get; set;}
    public Action<CCSPlayerController>? BackAction {get; set;}
    public PostSelectAction PostSelectAction {get; set;}
    public void Open(CCSPlayerController player);
    public void AddMenuOption(string title, Action<CCSPlayerController, IDynamicMenuOption> onExecute);
}
public interface IDynamicMenuOption
{
    public string Title {get; set;}
    public OptionColors Color {get; set;}
    public Action<CCSPlayerController, IDynamicMenuOption> OnExecute {get; set;}
}

public enum OptionColors
{
    Default,
    White,
    DarkRed,
    Green,
    LightYellow,
    LightBlue,
    Olive,
    Lime,
    Red,
    LightPurple,
    Purple,
    Grey,
    Yellow,
    Gold,
    Silver,
    Blue,
    DarkBlue,
    BlueGrey,
    Magenta,
    LightRed,
    Orange,
    Darkred
}