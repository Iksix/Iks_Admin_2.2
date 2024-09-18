namespace IksAdminApi;

using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Menu;
using IksAdminApi.DataTypes;
using Microsoft.Extensions.Localization;

public interface IIksAdminApi
{
    // GLOBALS ===
    public IAdminConfig Config { get; set; }
    public IStringLocalizer Localizer { get; set; }
    public BasePlugin Plugin { get; set; } 
    public string ModuleDirectory { get; set; }
    public Dictionary<string, SortMenu[]> SortMenus { get; set; }
    public Admin ConsoleAdmin {get; set;}
    public List<Admin> ServerAdmins { get; set; }
    public List<Admin> AllAdmins { get; set; }
    public List<Group> Groups {get; set;}
    public List<GroupLimitation> GroupLimitations {get; set;}
    public Dictionary<string, string> RegistredPermissions { get; set; }
    public string DbConnectionString {get; set;}
    public Dictionary<CCSPlayerController, Action<string>> NextPlayerMessage {get;}
    // MENU ===
    public IDynamicMenu CreateMenu(string id, string title, MenuType? type = null, MenuColors titleColor = MenuColors.Default, PostSelectAction postSelectAction = PostSelectAction.Nothing, Action<CCSPlayerController>? backAction = null, IDynamicMenu? backMenu = null);
    public void CloseMenu(CCSPlayerController player);
    // FUNC ===
    public void Debug(string message);
    public void LogError(string message);
    public void RegisterPermission(string key, string defaultFlags);
    public string GetCurrentPermissionFlags(string key);
    public string GetMultipleCurrnetPermissionFlags(string[] keys);
    public Task RefreshAdmins();
    public void HookNextPlayerMessage(CCSPlayerController player, Action<string> action);
    public void RemoveNextPlayerMessageHook(CCSPlayerController player);
    // EVENTS ===
    public delegate HookResult MenuOpenHandler(CCSPlayerController player, IDynamicMenu menu, IMenu gameMenu);
    public event MenuOpenHandler MenuOpenPre;
    public event MenuOpenHandler MenuOpenPost;
    public delegate HookResult OptionRenderHandler(CCSPlayerController player, IDynamicMenu menu, IMenu gameMenu, IDynamicMenuOption option);
    public event OptionRenderHandler OptionRenderPre;
    public event OptionRenderHandler OptionRenderPost;
    public delegate HookResult OptionExecuted(CCSPlayerController player, IDynamicMenu menu, IMenu gameMenu, IDynamicMenuOption option);
    public event OptionExecuted OptionExecutedPre;
    public event OptionExecuted OptionExecutedPost;
}
