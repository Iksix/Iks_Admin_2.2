using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using MenuManager;
using IksAdminApi;
using CounterStrikeSharp.API.Modules.Menu;
using IksAdmin.Menu;
using Microsoft.Extensions.Localization;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using IksAdmin.Functions;
using MySqlConnector;
using SharpMenu = CounterStrikeSharp.API.Modules.Menu;
using IksAdmin.Menus;
using IksAdminApi.DataTypes;
using MenuType = IksAdminApi.DataTypes.MenuType;
namespace IksAdmin;

public class Main : BasePlugin, IPluginConfig<PluginConfig>
{
    public override string ModuleName => "IksAdmin";
    public override string ModuleVersion => "2.2";
    public override string ModuleAuthor => "iks [Discord: iks__]";

    public PluginConfig Config { get; set; } = null!;
    public static IMenuApi? MenuApi = null;
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
        var builder = new MySqlConnectionStringBuilder();
        builder.Password = config.Password;
        builder.Server = config.Host;
        builder.Database = config.Database;
        builder.UserID = config.User;
        builder.Port = uint.Parse(config.Port);
        Database.ConnectionString = builder.ConnectionString;
    }
    public override void Load(bool hotReload)
    {
        AdminApi = new AdminApi(this, Config, Localizer, ModuleDirectory, Database.ConnectionString);
        Capabilities.RegisterPluginCapability(_pluginCapability, () => AdminApi);
        Admin.GetCurrentFlagsFunc = UtilsFunctions.GetCurrentFlagsFunc;
        Admin.GetCurrentImmunityFunc = UtilsFunctions.GetCurrentImmunityFunc;
        Admin.GetGroupFunc = UtilsFunctions.GetGroupFunc;
        AdminUtils.FindAdminMethod = UtilsFunctions.FindAdminMethod;
        AdminUtils.GetPremissions = UtilsFunctions.GetPermissions;
        AdminUtils.GetConfigMethod = UtilsFunctions.GetConfigMethod;
        AdminUtils.Debug = UtilsFunctions.SetDebugMethod;
        Helper.SetSortMenus();
        AddCommandListener("say", OnSay);
        InitializePermissions();
    }

    private HookResult OnSay(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (player == null) return HookResult.Continue;
        bool toTeam = commandInfo.GetArg(0) == "say_team";
        var msg = commandInfo.GetCommandString;
        if (toTeam)
        {
            msg = msg.Remove(0, 9);
        } else {
            msg = msg.Remove(0, 4);
        }
        if (msg.StartsWith("\""))
        {
            msg = msg.Remove(0, 1);
            msg = msg.Remove(msg.Length - 1, 1);
        }
        AdminApi.Debug($"{player.PlayerName} message: {msg}");
        if (AdminApi.NextPlayerMessage.ContainsKey(player) && msg.StartsWith("!"))
        {
            AdminApi.Debug("Next player message: " + msg);
            AdminApi.NextPlayerMessage[player].Invoke(msg.Remove(0, 1));
            AdminApi.RemoveNextPlayerMessageHook(player);
            return HookResult.Handled;
        }

        return HookResult.Continue;
    }

    private void InitializePermissions()
    {
        // Admin manage ===
        AdminApi.RegisterPermission("admins_manage_add", "z");
        AdminApi.RegisterPermission("admins_manage_delete", "z");
        AdminApi.RegisterPermission("admins_manage_edit", "z");
        AdminApi.RegisterPermission("admins_manage_refresh", "z");
        // Groups manage ===
        AdminApi.RegisterPermission("groups_manage_add", "z");
        AdminApi.RegisterPermission("groups_manage_delete", "z");
        AdminApi.RegisterPermission("groups_manage_edit", "z");
        AdminApi.RegisterPermission("groups_manage_refresh", "z");
    }

    public override void OnAllPluginsLoaded(bool hotReload)
    {
        try
        {
            MenuApi = MenuCapability.Get()!;
            if (MenuApi == null)
            {
                AdminApi.Debug("Start without Menu Manager");
            }
        }
        catch (System.Exception)
        {
            AdminApi.Debug("Start without Menu Manager");
        }
        
    }

    [ConsoleCommand("css_admin_reload_cfg")]
    public void OnReloadCfg(CCSPlayerController caller, CommandInfo info)
    {
        OnConfigParsed(Config);
        Helper.SetSortMenus();
    }
    [ConsoleCommand("css_reload_admins")]
    public void OnReloadAdmins(CCSPlayerController caller, CommandInfo info)
    {
        Task.Run(async () => { await AdminApi.RefreshAdmins(); });
    }

    [ConsoleCommand("css_admin")]
    public void OnMenuCmd(CCSPlayerController caller, CommandInfo info)
    {
        var menu = AdminApi.CreateMenu(GenerateMenuId("testmenu"), "Test menu", titleColor: MenuColors.Lime);
        if (caller.Admin() == null)
        {
            info.ReplyToCommand("ты не админ");
            return;
        }
        menu.AddMenuOption(GenerateOptionId("admins_manage"), "Admin manage", (_, _) => { 
            AdminManageMenus.OpenAdminManageMenu(caller);
        }, viewFlags: AdminApi.GetMultipleCurrnetPermissionFlags([
            "admins_manage_add", "admins_manage_delete", "admins_manage_edit", "admins_manage_refresh",
            "groups_manage_add", "groups_manage_delete", "groups_manage_edit", "groups_manage_refresh"
            ]));

        menu.Open(caller);

    }
}

public class AdminApi : IIksAdminApi
{
    public IAdminConfig Config { get; set; } 
    public BasePlugin Plugin { get; set; } 
    public IStringLocalizer Localizer { get; set; }
    public Dictionary<string, SortMenu[]> SortMenus { get; set; } = new();
    public string ModuleDirectory { get; set; }
    public List<Admin> ServerAdmins { get; set; } = new();
    public List<Admin> AllAdmins { get; set; } = new();
    public Dictionary<string, string> RegistredPermissions {get; set;} = new();
    public List<Group> Groups { get; set; } = new();
    public Admin ConsoleAdmin { get; set; } = null!;
    public string DbConnectionString {get; set;}
    public Dictionary<CCSPlayerController, Action<string>> NextPlayerMessage {get; set;} = new();

    public AdminApi(BasePlugin plugin, IAdminConfig config, IStringLocalizer localizer, string moduleDirectory, string dbConnectionString)
    {
        Plugin = plugin;
        Config = config;
        Localizer = localizer;
        ModuleDirectory = moduleDirectory;
        DbConnectionString = dbConnectionString;
        Task.Run(async () => {
            try
            {
                Debug("Init Database");
                await Database.Init();
                Debug("Refresh Admins");
                await RefreshAdmins();
            }
            catch (System.Exception e)
            {
                LogError(e.ToString());
                throw;
            }
            
        });
    }
    public void CloseMenu(CCSPlayerController player)
    {
        if (Main.MenuApi != null)
        {
            Main.MenuApi.CloseMenu(player);
        }
        SharpMenu.MenuManager.CloseActiveMenu(player);
    }
    public IDynamicMenu CreateMenu(string id, string title, MenuType? type = null, MenuColors titleColor = MenuColors.Default, PostSelectAction postSelectAction = PostSelectAction.Nothing, Action<CCSPlayerController>? backAction = null, IDynamicMenu? backMenu = null)
    {
        if (type == null) type = (MenuType)Config.MenuType;
        return new DynamicMenu(id, title, (MenuType)type, titleColor, postSelectAction, backAction, backMenu);
    }

    public void Debug(string message)
    {
        if (!Config.DebugMode) return;
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("[Admin Debug]: " +message);
        Console.ResetColor();

    }
    public void LogError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("[Admin Error]: " + message);
         Console.ResetColor();
    }

    public void RegisterPermission(string key, string defaultFlags)
    {
        RegistredPermissions.Add(key, defaultFlags);
    }
    public string GetCurrentPermissionFlags(string key)
    {
        if (Config.PermissionReplacement.ContainsKey(key)) return Config.PermissionReplacement[key];
        if (RegistredPermissions.ContainsKey(key)) return RegistredPermissions[key];
        Debug("Permission key not found in registred and replacement ✖ | returning empty string");
        return "";
    }
    public string GetMultipleCurrnetPermissionFlags(string[] keys)
    {
        string result = "";
        foreach(string key in keys)
        {
            result += GetCurrentPermissionFlags(key);
        }
        return result;
    }

    // EVENTS ===
 
    public event IIksAdminApi.MenuOpenHandler? MenuOpenPre;
    public bool OnMenuOpenPre(CCSPlayerController player, IDynamicMenu menu, IMenu gameMenu)
    {
        var result = MenuOpenPre?.Invoke(player, menu, gameMenu) ?? HookResult.Continue;
        if (result is HookResult.Stop or HookResult.Handled) {
            Debug("Some event handler stopped menu opening | Id: " + menu.Id);
            return false;
        }
        return true;
    }
    public event IIksAdminApi.MenuOpenHandler? MenuOpenPost;
    public bool OnMenuOpenPost(CCSPlayerController player, IDynamicMenu menu, IMenu gameMenu)
    {
        var result = MenuOpenPost?.Invoke(player, menu, gameMenu) ?? HookResult.Continue;
        if (result is HookResult.Stop or HookResult.Handled) {
            return false;
        }
        return true;
    }
    public event IIksAdminApi.OptionRenderHandler? OptionRenderPre;
    public bool OnOptionRenderPre(CCSPlayerController player, IDynamicMenu menu, IMenu gameMenu, IDynamicMenuOption option)
    {
        var result = OptionRenderPre?.Invoke(player, menu, gameMenu, option) ?? HookResult.Continue;
        if (result is HookResult.Stop or HookResult.Handled) {
            Debug("Some event handler skipped option render | Id: " + option.Id);
            return false;
        }
        return true;
    }
    public event IIksAdminApi.OptionRenderHandler? OptionRenderPost;
    public bool OnOptionRenderPost(CCSPlayerController player, IDynamicMenu menu, IMenu gameMenu, IDynamicMenuOption option)
    {
        var result = OptionRenderPost?.Invoke(player, menu, gameMenu, option) ?? HookResult.Continue;
        if (result is HookResult.Stop or HookResult.Handled) {
            return false;
        }
        return true;
    }
    public event IIksAdminApi.OptionExecuted? OptionExecutedPre;
    public bool OnOptionExecutedPre(CCSPlayerController player, IDynamicMenu menu, IMenu gameMenu, IDynamicMenuOption option)
    {
        var result = OptionExecutedPre?.Invoke(player, menu, gameMenu, option) ?? HookResult.Continue;
        if (result is HookResult.Stop or HookResult.Handled) {
            Debug("Some event handler stopped option executed | Id: " + option.Id);
            return false;
        }
        return true;
    }
    public event IIksAdminApi.OptionExecuted? OptionExecutedPost;
    public bool OnOptionExecutedPost(CCSPlayerController player, IDynamicMenu menu, IMenu gameMenu, IDynamicMenuOption option)
    {
        var result = OptionExecutedPost?.Invoke(player, menu, gameMenu, option) ?? HookResult.Continue;
        if (result is HookResult.Stop or HookResult.Handled) {
            return false;
        }
        return true;
    }

    public async Task RefreshAdmins()
    {
        await AdminsControllFunctions.RefreshAdmins();
    }

    public void HookNextPlayerMessage(CCSPlayerController player, Action<string> action)
    {
        Debug("Log next player message: " + player.PlayerName);
        if (NextPlayerMessage.ContainsKey(player))
        {
            NextPlayerMessage[player] = action;
        } else NextPlayerMessage.Add(player, action);
    }

    public void RemoveNextPlayerMessageHook(CCSPlayerController player)
    {
        Debug("Remove next player message hook: " + player.PlayerName);
        NextPlayerMessage.Remove(player);
    }
}