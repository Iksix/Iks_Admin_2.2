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
using MenuType = IksAdminApi.MenuType;
using CoreRCON;
using System.Net;
using CounterStrikeSharp.API;
using static CounterStrikeSharp.API.Modules.Commands.CommandInfo;
using Group = IksAdminApi.Group;
using IksAdmin.Commands;
using System.Diagnostics.CodeAnalysis;
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
        AdminApi.OnModuleUnload += OnModuleUnload;
        Capabilities.RegisterPluginCapability(_pluginCapability, () => AdminApi);
        Admin.GetCurrentFlagsFunc = UtilsFunctions.GetCurrentFlagsFunc;
        Admin.GetCurrentImmunityFunc = UtilsFunctions.GetCurrentImmunityFunc;
        AdminUtils.GetGroupFromIdFunc = UtilsFunctions.GetGroupFromIdFunc;
        AdminUtils.FindAdminMethod = UtilsFunctions.FindAdminMethod;
        AdminUtils.GetPremissions = UtilsFunctions.GetPermissions;
        AdminUtils.GetConfigMethod = UtilsFunctions.GetConfigMethod;
        AdminUtils.Debug = UtilsFunctions.SetDebugMethod;
        Helper.SetSortMenus();
        AddCommandListener("say", OnSay);
        InitializePermissions();
        InitializeCommands();
    }

    private void OnModuleUnload(AdminModule module)
    {
        foreach (var commands in AdminApi.RegistredCommands)
        {
            if (commands.Key != module.ModuleName) continue;
            foreach (var command in commands.Value)
            {
                RemoveCommand(command.command, command.callback);
            }
        }
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
        AdminApi.RegisterPermission("admins_manage.add", "z");
        AdminApi.RegisterPermission("admins_manage.delete", "z");
        AdminApi.RegisterPermission("admins_manage.edit", "z");
        AdminApi.RegisterPermission("admins_manage.refresh", "z");
        // Groups manage ===
        AdminApi.RegisterPermission("groups_manage.add", "z");
        AdminApi.RegisterPermission("groups_manage.delete", "z");
        AdminApi.RegisterPermission("groups_manage.edit", "z");
        AdminApi.RegisterPermission("groups_manage.refresh", "z");
    }
    private void InitializeCommands()
    {
        AdminApi.SetCommandInititalizer(ModuleName);
        AdminApi.AddNewCommand(
            "am_add",
            "Create admin",
            "admins_manage.add",
            "css_am_add <steamId> <name> <time> <serverKey> <groupName>\n" +
            "css_am_add <steamId> <name> <time> <server key> <flags> <immunity>",
            AdminsManageCommands.Add,
            minArgs: 5 
        );
        AdminApi.ClearCommandInitializer();
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
    public override void Unload(bool hotReload)
    {
        foreach (var commands in AdminApi.RegistredCommands)
        {
            if (commands.Key != ModuleName) continue;
            foreach (var command in commands.Value)
            {
                RemoveCommand(command.command, command.callback);
            }
        }
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
    public List<ServerModel> AllServers { get; set; } = new();
    public ServerModel ThisServer { get; set; } = null!;
    public Dictionary<string, Dictionary<string, string>> RegistredPermissions {get; set;} = new();
    public List<Group> Groups { get; set; } = new();
    public List<GroupLimitation> GroupLimitations {get; set;} = new();
    public Admin ConsoleAdmin { get; set; } = null!;
    public string DbConnectionString {get; set;}
    public Dictionary<CCSPlayerController, Action<string>> NextPlayerMessage {get; set;} = new();
    private string CommandInitializer = "IksAdmin";
    public struct CommandStruct
    {
        public string command;
        public CommandCallback callback;
    }
    public Dictionary<string, List<CommandStruct>> RegistredCommands = new Dictionary<string, List<CommandStruct>>();

    public AdminApi(BasePlugin plugin, IAdminConfig config, IStringLocalizer localizer, string moduleDirectory, string dbConnectionString)
    {
        Plugin = plugin;
        Config = config;
        Localizer = localizer;
        ModuleDirectory = moduleDirectory;
        DbConnectionString = dbConnectionString;
        var serverModel = new ServerModel(
                Config.ServerKey,
                Config.ServerIp,
                Config.ServerName,
                Config.RconPassword
            );
            
        Task.Run(async () => {
            try
            {
                Debug("Init Database");
                await Database.Init();
                Debug("Refresh Admins");
                await RefreshAdmins();
                await ServersControllFunctions.Add(serverModel);
                AllServers = await ServersControllFunctions.GetAll();
                ThisServer = AllServers.First(x => x.ServerKey == serverModel.ServerKey);
                await SendRconToAllServers("css_am_reload_servers", true);
                await SendRconToAllServers("css_am_reload_admins", true);
                Server.NextFrame(() => {
                    OnReady?.Invoke();
                });
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
        // example key = "admin_manage.add"
        var firstKey = key.Split(".")[0]; // admin_manage
        var lastKey = string.Join(".", key.Split(".").Skip(1)); // add
        if (RegistredPermissions.ContainsKey(firstKey))
        {
            var perms = RegistredPermissions[firstKey];
            if (!perms.ContainsKey(lastKey))
            {
                perms.Add(lastKey, defaultFlags);
            }
        } else {
            RegistredPermissions.Add(firstKey, new Dictionary<string, string> { { lastKey, defaultFlags } });
        }
    }
    public string GetCurrentPermissionFlags(string key)
    {
        return AdminUtils.GetCurrentPermissionFlags(key);
    }
    public string GetCurrentPermissionFlags(string[] keys)
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
    public event Action? OnReady;
    public event Action<AdminModule>? OnModuleUnload;
    public event Action<AdminModule>? OnModuleLoad;

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

    public async Task SendRconToAllServers(string command, bool ignoreSelf = false)
    {
        foreach (var server in AllServers)
        {
            if (ignoreSelf && server.Ip == ThisServer.Ip) continue;
            await SendRconToServer(server, command);
        }
    }

    public async Task SendRconToServer(ServerModel server, string command)
    {
        var ip = server.Ip.Split(":")[0];
        var port = server.Ip.Split(":")[1];
        Debug($"Sending rcon command [{command}] to server ({server.Name})[{server.Ip}] ...");
        using var rcon = new RCON(new IPEndPoint(IPAddress.Parse(ip), int.Parse(port)), server.Rcon ?? "", 10000);
        await rcon.ConnectAsync();
        var result = await rcon.SendCommandAsync(command);
        Debug($"Success ✔");
        Debug($"Response from {server.Name} [{server.Ip}]: {result}");
    }

    public ServerModel? GetServerByKey(string serverKey)
    {
        return AllServers.FirstOrDefault(x => x.ServerKey == serverKey);
    }

    public ServerModel? GetServerByIp(string ip)
    {
        return AllServers.FirstOrDefault(x => x.Ip == ip);
    }
    
    public void AddNewCommand(
        string command,
        string description,
        string permission,
        string usage,
        Action<CCSPlayerController?, List<string>, CommandInfo> onExecute,
        CommandUsage commandUsage = CommandUsage.CLIENT_AND_SERVER,
        string? tag = null,
        string? notEnoughPermissionsMessage = null,
        int minArgs = 0)
    {
        var tagString = tag == null ? Localizer["Tag"] : tag;
        CommandCallback callback = (p, info) => {
            if (commandUsage == CommandUsage.CLIENT_ONLY && p == null)
            {
                info.Reply("It's client only command ✖", tagString);
                return;
            }
            if (commandUsage == CommandUsage.SERVER_ONLY && p != null)
            {
                info.Reply(Localizer["Error.OnlyServerCommand"], tagString);
                return;
            }
            var admin = p == null ? ConsoleAdmin : p.Admin();
            if (!admin.HasPermissions(permission))
            {
                info.Reply(notEnoughPermissionsMessage == null ?Localizer["Error.NotEnoughPermissions"] : notEnoughPermissionsMessage, tagString);
                return;
            }
            var args = AdminUtils.GetArgsFromCommandLine(info.GetCommandString);
            if (args.Count < minArgs)
            {
                info.Reply(Localizer["Error.DifferentNumberOfArgs"].Value.Replace("{usage}", usage), tagString);
                return;
            }
            try
            {
                onExecute.Invoke(p, args, info);
            }
            catch (ArgumentException)
            {
                info.Reply(Localizer["Error.DifferentNumberOfArgs"].Value.Replace("{usage}", usage), tagString);
                throw;
            }
            catch (Exception e)
            {
                info.Reply(Localizer["Error.OtherCommandError"].Value.Replace("{usage}", usage), tagString);
                LogError(e.Message);
            }
            
        };
        Plugin.AddCommand("css_" + command, description, callback);
        RegistredCommands[CommandInitializer].Add(new CommandStruct { command = "css_" + command, callback = callback });
    }

    public void SetCommandInititalizer(string moduleName)
    {
        CommandInitializer = moduleName;
        RegistredCommands.TryAdd(CommandInitializer, new List<CommandStruct>());
    }
    public void ClearCommandInitializer()
    {
        CommandInitializer = "unsorted";
    }

    public void EOnModuleLoad(AdminModule module)
    {
        OnModuleLoad?.Invoke(module);
    }

    public void EOnModuleUnload(AdminModule module)
    {
        OnModuleUnload?.Invoke(module);
    }
}