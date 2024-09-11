namespace IksAdminApi;

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Utils;
using MenuManager;
using Microsoft.Extensions.Localization;

public interface IIksAdminApi
{
    // GLOBALS ===
    public IAdminConfig Config { get; set; }
    public IStringLocalizer Localizer { get; set; }
    public BasePlugin Plugin { get; set; } 
    public string ModuleDirectory { get; set; }
    public Dictionary<string, SortMenu[]> SortMenus { get; set; }
    public List<Admin> ServerAdmins { get; set; }
    public List<Admin> AllAdmins { get; set; }
    // MENU ===
    public IDynamicMenu CreateMenu(string id, string title, MenuType type = (MenuType)3, PostSelectAction postSelectAction = PostSelectAction.Nothing, Action<CCSPlayerController>? backAction = null, IMenu? backMenu = null);
    public void CloseMenu(CCSPlayerController player);
    // FUNC ===
    public void Debug(string message);
    // EVENTS ===
    public void EDynamicMenuOpen(CCSPlayerController player, IDynamicMenu menu);
    public event Action<CCSPlayerController, IDynamicMenu>? DynamicMenuOpen;
    public void EDynamicOptionRendered(CCSPlayerController player, IDynamicMenu menu, IDynamicMenuOption option);
    public event Action<CCSPlayerController, IDynamicMenu, IDynamicMenuOption>? DynamicOptionRendered;
}

public class SortMenu
{
    public string Id {get; set;}
    public string ViewFlags {get; set;} = "not override";
    public bool View {get; set;} = true;
    public SortMenu(string id, string viewFlags = "not override", bool view = true)
    {
        Id = id;
        ViewFlags = viewFlags;
        View = view;
    }
}

public interface IAdminConfig 
{
    public bool DebugMode { get; set; }
    public int MenuType { get; set; }
}
public interface IDynamicMenu
{
    public string Id {get; set;}
    public string Title {get; set;}
    public MenuType Type {get; set;}
    public Action<CCSPlayerController>? BackAction {get; set;}
    public PostSelectAction PostSelectAction {get; set;}
    public void Open(CCSPlayerController player, bool useSortMenu = true);
    public void AddMenuOption(string id, string title, Action<CCSPlayerController, IDynamicMenuOption> onExecute, OptionColors? color = null, bool disabled = false);
}
public interface IDynamicMenuOption
{
    public string Id {get; set;}
    public string Title {get; set;}
    public OptionColors Color {get; set;}
    public Action<CCSPlayerController, IDynamicMenuOption> OnExecute {get; set;}
}

public class Admin 
{
    public int Id {get; set;}
    public string Name {get; set;}
    public string SteamId {get; set;} = "";
    public string Flags {get; set;}
    public string ServerId {get; set;}
    public bool Online {get; set;} = false;
    public CCSPlayerController? Controller { get => AdminUtils.GetControllerBySteamId(SteamId); } 
}

public static class AdminUtils 
{
    public delegate Admin? AdminFinder(CCSPlayerController player);
    public static AdminFinder FindAdminMethod = null!;
    public static CCSPlayerController? GetControllerBySteamId(string steamId)
    {
        return Utilities.GetPlayers().FirstOrDefault(x => x != null && x.IsValid && x.AuthorizedSteamID != null && x.AuthorizedSteamID.SteamId64.ToString() == steamId);
    }
    public static Admin? Admin(this CCSPlayerController player)
    {
        return FindAdminMethod(player);
    }
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