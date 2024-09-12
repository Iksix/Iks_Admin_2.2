using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using MenuManager;
using IksAdminApi;
using CounterStrikeSharp.API.Modules.Menu;
using IksAdmin.Menu;
using Microsoft.Extensions.Localization;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using Microsoft.Extensions.Logging;
using CounterStrikeSharp.API;
using System.Text.Json;
using CounterStrikeSharp.API.Modules.Config;
using IksAdmin.Functions;

namespace IksAdmin;

public class Main : BasePlugin, IPluginConfig<PluginConfig>
{
    public override string ModuleName => "IksAdmin";
    public override string ModuleVersion => "2.2";
    public override string ModuleAuthor => "iks [Discord: iks__]";

    public PluginConfig Config { get; set; } = null!;
    public static IMenuApi MenuApi = null!;
    private static readonly PluginCapability<IMenuApi?> MenuCapability = new("menu:nfcore");   
    public static AdminApi AdminApi = null!;
    private readonly PluginCapability<IIksAdminApi> _pluginCapability  = new("iksadmin:core");
    

    public static string GenerateMenuId(string id)
    {
        return $"iksadmin:menu:{id}";
    }
    public static string GenerateOptionId(string id)
    {
        return $"iksadmin:option:{id}";
    }
    public void OnConfigParsed(PluginConfig config)
    {
        Config = config;
    }
    public override void Load(bool hotReload)
    {
        AdminApi = new AdminApi(this, Config, Localizer, ModuleDirectory);
        AdminUtils.FindAdminMethod = UtilsFunctions.FindAdminMethod;
        AdminUtils.GetPremissions = UtilsFunctions.GetPermissions;
        AdminUtils.GetConfigMethod = UtilsFunctions.GetConfigMethod;
        AdminUtils.Debug = UtilsFunctions.SetDebugMethod;
        SetSortMenus();
    }





    public override void OnAllPluginsLoaded(bool hotReload)
    {
        MenuApi = MenuCapability.Get()!;
    }

    [ConsoleCommand("css_admin_reload_cfg")]
    public void OnReloadCfg(CCSPlayerController caller, CommandInfo info)
    {
        OnConfigParsed(Config);
        SetSortMenus();
    }

    [ConsoleCommand("css_menu")]
    public void OnMenuCmd(CCSPlayerController caller, CommandInfo info)
    {
        var menu = AdminApi.CreateMenu(GenerateMenuId("testmenu"), "Test menu", (MenuType)Config.MenuType);
        if (caller.Admin == null) return;
        menu.AddMenuOption(GenerateOptionId("1"), "Option 1", (_, _) => { 
            caller.PrintToChat("Option 1 executed");
        });
        menu.AddMenuOption(GenerateOptionId("2"), "Option 2", (_, _) => { 
            caller.PrintToChat("Option 2 executed");
        });
        menu.AddMenuOption(GenerateOptionId("3"), "Option 3", (_, _) => { 
            caller.PrintToChat("Option 3 executed");
        });
        menu.Open(caller);
    }
    public static void SetSortMenus()
    {
        using var streamReader = new StreamReader($"{AdminApi.ModuleDirectory}/sortmenus.json");
        string json = streamReader.ReadToEnd();
        var sortMenus = JsonSerializer.Deserialize<Dictionary<string, SortMenu[]>>(json, new JsonSerializerOptions() { ReadCommentHandling = JsonCommentHandling.Skip })!;
        AdminApi.SortMenus = sortMenus;
        AdminApi.Debug("Sort Menus setted!");
        foreach (var item in AdminApi.SortMenus)
        {
            AdminApi.Debug($@"Menu key: {item.Key}");
            AdminApi.Debug($@"Menu options: ");
            AdminApi.Debug($@"ID | ViewFlags | View");
            foreach (var option in item.Value)
            {
                AdminApi.Debug($@"{option.Id} | {option.ViewFlags} | {option.View}");
            }
        }
    }
}

public class AdminApi : IIksAdminApi
{
    public IAdminConfig Config { get; set; } 
    public BasePlugin Plugin { get; set; } 
    public IStringLocalizer Localizer { get; set; }
    public Dictionary<string, SortMenu[]> SortMenus { get; set; }
    public string ModuleDirectory { get; set; }
    public List<Admin> ServerAdmins { get; set; } = new();
    public List<Admin> AllAdmins { get; set; } = new();
    public Dictionary<string, string> RegistredPermissions {get; set;} = new();

    public AdminApi(BasePlugin plugin, IAdminConfig config, IStringLocalizer localizer, string moduleDirectory)
    {
        Plugin = plugin;
        Config = config;
        Localizer = localizer;
        ModuleDirectory = moduleDirectory;
    }
    public void CloseMenu(CCSPlayerController player)
    {
        throw new NotImplementedException();
    }
    public IDynamicMenu CreateMenu(string id, string title, MenuType type = MenuType.ButtonMenu, PostSelectAction postSelectAction = PostSelectAction.Nothing, Action<CCSPlayerController>? backAction = null, IMenu? backMenu = null)
    {
        return new DynamicMenu(id, title, type, postSelectAction, backAction, backMenu);
    }

    public void Debug(string message)
    {
        if (!Config.DebugMode) return;
        Server.NextFrame(() => {
            Plugin.Logger.LogInformation("[Admin Debug]: " +message);
        });
    }

    public void EDynamicMenuOpen(CCSPlayerController player, IDynamicMenu menu)
    {
        throw new NotImplementedException();
    }
    public event Action<CCSPlayerController, IDynamicMenu>? DynamicMenuOpen;
    public void EDynamicOptionRendered(CCSPlayerController player, IDynamicMenu menu, IDynamicMenuOption option)
    {
        throw new NotImplementedException();
    }

    public void RegisterPermission(string key, string defaultFlags)
    {
        RegistredPermissions.Add(key, defaultFlags);
    }
    public string GetCurrentPermissionFlags(string key)
    {
        return "";
    }

    public event Action<CCSPlayerController, IDynamicMenu, IDynamicMenuOption>? DynamicOptionRendered;
}