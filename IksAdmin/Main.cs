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
using MenuType = IksAdminApi.MenuType;
using CoreRCON;
using System.Net;
using CounterStrikeSharp.API;
using static CounterStrikeSharp.API.Modules.Commands.CommandInfo;
using Group = IksAdminApi.Group;
using IksAdmin.Commands;
using CounterStrikeSharp.API.Core.Commands;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;
using CounterStrikeSharp.API.ValveConstants.Protobuf;
using CounterStrikeSharp.API.Modules.Utils;
using SteamWebAPI2.Utilities;
using SteamWebAPI2.Interfaces;
using System.Security.Cryptography.X509Certificates;
using CounterStrikeSharp.API.Modules.Entities;
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
    public static Dictionary<CCSPlayerController, string> HtmlMessages = new();
    public static Dictionary<CCSPlayerController, Timer> HtmlMessagesTimer = new();
    public static List<CCSPlayerController> BlockTeamChange = new();
    public static Dictionary<string, bool> KickOnFullConnect = new();
    public static Dictionary<string, string> KickOnFullConnectReason = new();
    

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
        AdminModule.AdminApi = AdminApi;
        AdminUtils.AdminApi = AdminApi;
        AdminApi.OnModuleLoaded += OnModuleLoaded;
        AdminApi.OnModuleUnload += OnModuleUnload;
        Capabilities.RegisterPluginCapability(_pluginCapability, () => AdminApi);
        Admin.GetCurrentFlagsFunc = UtilsFunctions.GetCurrentFlagsFunc;
        Admin.GetCurrentImmunityFunc = UtilsFunctions.GetCurrentImmunityFunc;
        AdminUtils.GetGroupFromIdFunc = UtilsFunctions.GetGroupFromIdFunc;
        AdminUtils.FindAdminByControllerMethod = UtilsFunctions.FindAdminByControllerMethod;
        AdminUtils.FindAdminByIdMethod = UtilsFunctions.FindAdminByIdMethod;
        AdminUtils.GetPremissions = UtilsFunctions.GetPermissions;
        AdminUtils.GetConfigMethod = UtilsFunctions.GetConfigMethod;
        AdminUtils.Debug = UtilsFunctions.SetDebugMethod;
        AdminApi.SetConfigs();
        Helper.SetSortMenus();
        AddCommandListener("say", OnSay);
        AddCommandListener("say_team", OnSay);
        AddCommandListener("jointeam", OnJoinTeam);
        InitializePermissions();
        InitializeCommands();
        RegisterListener<Listeners.OnTick>(() => {
            foreach (var msg in HtmlMessages)
            {
                var player = msg.Key;
                var message = msg.Value;
                if (player == null || !player.IsValid || player.IsBot) continue;
                if (message == "") continue;
                player.PrintToCenterHtml(message);
            }
        });
        RegisterListener<Listeners.OnClientAuthorized>(OnAuthorized);

    }

    private void OnAuthorized(int playerSlot, SteamID steamId)
    {
        var steamId64 = steamId.SteamId64.ToString();
        var player = Utilities.GetPlayerFromSlot(playerSlot);
        var ip = player!.IpAddress;
        Task.Run(async () => {
            await AdminApi.ReloadInfractions(steamId64, ip, true);
        });
    }

    private HookResult OnJoinTeam(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (BlockTeamChange.Contains(player!)) return HookResult.Stop;
        return HookResult.Continue;
    }

    private void OnModuleLoaded(AdminModule module)
    {
        AdminApi.LoadedModules.Add(module);
    }

    private void OnModuleUnload(AdminModule module)
    {
        foreach (var commands in AdminApi.RegistredCommands)
        {
            if (commands.Key != module.ModuleName) continue;
            foreach (var command in commands.Value)
            {
                Console.WriteLine($"Removing command from {module.ModuleName} [{command.Command}]");
                CommandManager.RemoveCommand(command.Definition);
            }
        }
        AdminApi.LoadedModules.Remove(module);
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
        var gag = player.GetGag();
        if (gag != null)
        {
            Helper.Print(player, Localizer["MessageWhenGag"].Value
                .Replace("{date}", gag.EndAt == 0 ? Localizer["Other.Never"] : AdminUtils.GetDateString(gag.EndAt))
            );
            return HookResult.Stop;
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
        // Blocks manage ===

        // BAN ===
        AdminApi.RegisterPermission("blocks_manage.ban", "b");
        AdminApi.RegisterPermission("blocks_manage.own_ban_reason", "b"); // С этим флагом у админа появляется пункт в меню для выбора собственной причины
        AdminApi.RegisterPermission("blocks_manage.ban_ip", "b");
        AdminApi.RegisterPermission("blocks_manage.unban", "b");
        AdminApi.RegisterPermission("blocks_manage.unban_ip", "b");

        // MUTE
        AdminApi.RegisterPermission("blocks_manage.mute", "m"); 
        AdminApi.RegisterPermission("blocks_manage.unmute", "m"); 
        AdminApi.RegisterPermission("blocks_manage.own_mute_reason", "m"); // С этим флагом у админа появляется пункт в меню для выбора собственной причины
        // GAG
        AdminApi.RegisterPermission("blocks_manage.gag", "g"); 
        AdminApi.RegisterPermission("blocks_manage.ungag", "g"); 
        AdminApi.RegisterPermission("blocks_manage.own_gag_reason", "g"); // С этим флагом у админа появляется пункт в меню для выбора собственной причины

        AdminApi.RegisterPermission("blocks_manage.remove_immunity", "i"); // Снять наказание выданное админом ниже по иммунитету
        AdminApi.RegisterPermission("blocks_manage.remove_all", "u"); // Снять наказание выданное кем угодно (кроме консоли)
        AdminApi.RegisterPermission("blocks_manage.remove_console", "c"); // Снять наказание выданное консолью
        AdminApi.RegisterPermission("other.equals_immunity_action", "e"); // Разрешить взаймодействие с админами равными по иммунитету (Включая снятие наказаний если есть флаг blocks_manage.remove_immunity)
        // Players manage ===
    }
    private void InitializeCommands()
    {
        AdminApi.SetCommandInititalizer(ModuleName);
        AdminApi.AddNewCommand(
            "am_add",
            "Создать админа",
            "admins_manage.add",
            "css_am_add <steamId> <name> <time> <serverKey> <groupName>\n" +
            "css_am_add <steamId> <name> <time> <serverKey> <flags> <immunity>",
            AdminsManageCommands.Add,
            minArgs: 5 
        );

        // BLOCKS MANAGE ====
        // BANS ===
        AdminApi.AddNewCommand(
            "ban",
            "Забанить игрока",
            "blocks_manage.ban",
            "css_ban <#uid/#steamId/name/@...> <time> <reason>",
            BansManageCommands.Ban,
            minArgs: 3 
        );
        AdminApi.AddNewCommand(
            "unban",
            "Разбанить игрока",
            "blocks_manage.unban",
            "css_unban <steamId> <reason>",
            BansManageCommands.Unban,
            minArgs: 2 
        );
        AdminApi.AddNewCommand(
            "unbanip",
            "Разбанить игрока",
            "blocks_manage.unban_ip",
            "css_unbanip <ip> <reason>",
            BansManageCommands.UnbanIp,
            minArgs: 2 
        );
        AdminApi.AddNewCommand(
            "addban",
            "Забанить игрока по стим айди (оффлайн)",
            "blocks_manage.ban",
            "css_addban <steamId> <time> <reason>",
            BansManageCommands.AddBan,
            minArgs: 3 
        );
        AdminApi.AddNewCommand(
            "banip",
            "Забанить по айпи (онлайн)",
            "blocks_manage.ban_ip",
            "css_banip <#uid/#steamId/name/@...> <time> <reason>",
            BansManageCommands.BanIp,
            minArgs: 3 
        );
        AdminApi.AddNewCommand(
            "addbanip",
            "Забанить игрока по айпи (оффлайн)",
            "blocks_manage.ban_ip",
            "css_addbanip <ip> <time> <reason>",
            BansManageCommands.AddBanIp,
            minArgs: 3 
        );
        // GAG ===
        AdminApi.AddNewCommand(
            "gag",
            "Выдать гаг игроку (онлайн)",
            "blocks_manage.gag",
            "css_gag <#uid/#steamId/name/@...> <time> <reason>",
            GagsManageCommands.Gag,
            minArgs: 3 
        );
        AdminApi.AddNewCommand(
            "ungag",
            "Снять гаг с игрока (онлайн)",
            "blocks_manage.ungag",
            "css_ungag <#uid/#steamId/name/@...> <reason>",
            GagsManageCommands.Ungag,
            minArgs: 2 
        );
        AdminApi.AddNewCommand(
            "addgag",
            "Выдать гаг игроку (оффлайн)",
            "blocks_manage.gag",
            "css_addgag <steamId> <time> <reason>",
            GagsManageCommands.AddGag,
            minArgs: 3 
        );
        AdminApi.AddNewCommand(
            "removegag",
            "Снять гаг с игрока (оффлайн)",
            "blocks_manage.ungag",
            "css_ungag <steamId> <reason>",
            GagsManageCommands.RemoveGag,
            minArgs: 2 
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

    [GameEventHandler]
    public HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null) return HookResult.Continue;
        if (player.IsBot) return HookResult.Continue;
        var steamId = player!.AuthorizedSteamID!.SteamId64.ToString();
        if (KickOnFullConnect.ContainsKey(steamId))
        {
            var reason = KickOnFullConnectReason[steamId];
            bool instantlyKick = true;
            AdminApi.DisconnectPlayer(player, reason, instantlyKick);
            return HookResult.Continue;
        }
        return HookResult.Continue;
    }
    
    [GameEventHandler]
    public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        HtmlMessages.Remove(@event.Userid!);
        HtmlMessagesTimer.Remove(@event.Userid!);
        BlockTeamChange.Remove(@event.Userid!);
        var player = @event.Userid;
        if (player == null || player.IsBot) return HookResult.Continue;
        AdminApi.Gags.Remove(player.GetGag()!);
        AdminApi.Mutes.Remove(player.GetMute()!);
        return HookResult.Continue;
    }
    
    public override void Unload(bool hotReload)
    {
        foreach (var commands in AdminApi.RegistredCommands)
        {
            if (commands.Key != ModuleName) continue;
            foreach (var command in commands.Value)
            {
                CommandManager.RemoveCommand(command.Definition);
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
    public List<AdminModule> LoadedModules {get; set;} = new(); 

    private string CommandInitializer = "core";

    public Dictionary<string, List<CommandModel>> RegistredCommands {get; set;} = new Dictionary<string, List<CommandModel>>();
    public List<PlayerMute> Mutes {get; set; } = new();
    public List<PlayerGag> Gags {get; set; } = new();

    // CONFIGS ===
    public BansConfig BansConfig {get; set;} = new ();
    public MutesConfig MutesConfig {get; set;} = new ();
    public GagsConfig GagsConfig {get; set;} = new ();

    public AdminApi(BasePlugin plugin, IAdminConfig config, IStringLocalizer localizer, string moduleDirectory, string dbConnectionString)
    {
        Plugin = plugin;
        Config = config;
        Localizer = localizer;
        ModuleDirectory = moduleDirectory;
        DbConnectionString = dbConnectionString;
        Task.Run(async () => {
            await ReloadDataFromDb();
        });
    }

    public void SetConfigs()
    {
        BansConfig.Set();
        MutesConfig.Set();
        GagsConfig.Set();
    }

    public async Task ReloadDataFromDb()
    {
        var serverModel = new ServerModel(
                Config.ServerKey,
                Config.ServerIp,
                Config.ServerName,
                Config.RconPassword
            );
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
    public event Action<AdminModule>? OnModuleLoaded;

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
        if (Config.IgnoreCommandsRegistering.Contains(command))
        {
            Debug($"Adding new command [{command}] was skipped from config");
            return;
        }
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
            if (!p.HasPermissions(permission))
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
        var definition = new CommandDefinition("css_" + command, description, callback);
        Plugin.CommandManager.RegisterCommand(definition);
        RegistredCommands[CommandInitializer].Add(new CommandModel { 
            Command = "css_" + command, 
            Definition = definition,
            CommandUsage = commandUsage,
            Description = description,
            Pemission = permission,
            Usage = usage,
            Tag = tag
            });
    }

    public void SetCommandInititalizer(string moduleName)
    {
        CommandInitializer = moduleName;
        RegistredCommands.TryAdd(CommandInitializer, new List<CommandModel>());
    }
    public void ClearCommandInitializer()
    {
        CommandInitializer = "unsorted";
    }

    public void EOnModuleLoaded(AdminModule module)
    {
        OnModuleLoaded?.Invoke(module);
    }

    public void EOnModuleUnload(AdminModule module)
    {
        OnModuleUnload?.Invoke(module);
    }

    public async Task<int> AddBan(PlayerBan ban, bool announce = true)
    {
        try
        {
            Debug($"Baning player...");
            var reservedReason = BansConfig.Config.Reasons.FirstOrDefault(x => x.Title.ToLower() == ban.Reason.ToLower());
            if (reservedReason != null)
            {
                Debug($"Do reservedReason transformations...");
                if (reservedReason.BanOnAllServers)
                    ban.ServerId = null;
                ban.Reason = reservedReason.Text;
            }
            var admin = ban.Admin!;
            var group = admin.Group;
            if (group != null)
            {
                var limitations = group.Limitations;
                var maxTime = group.Limitations.FirstOrDefault(x => x.LimitationKey == "max_ban_time")?.LimitationKey;
                var minTime = group.Limitations.FirstOrDefault(x => x.LimitationKey == "min_ban_time")?.LimitationKey;
                var minTimeInt = minTime == null ? 0 : int.Parse(minTime);
                var maxTimeInt = maxTime == null ? int.MaxValue : int.Parse(maxTime);
                var maxByDay = group.Limitations.FirstOrDefault(x => x.LimitationKey == "max_bans_in_day")?.LimitationKey;
                var maxByDayInt = maxByDay == null ? int.MaxValue : int.Parse(maxByDay);
                if (ban.Duration > maxTimeInt || minTimeInt > ban.Duration)
                {
                    Helper.PrintToSteamId(admin.SteamId, AdminUtils.AdminApi.Localizer["Limitations.TimeLimit"].Value
                        .Replace("{min}", minTimeInt.ToString())
                        .Replace("{max}", maxTimeInt.ToString())
                    );
                    return 3;
                }
                if (maxByDay != null)
                {
                    var lastPunishments = await BansControllFunctions.GetLastAdminBans(admin, 60*60*24);
                    if (lastPunishments.Count > maxByDayInt)
                    {
                        Helper.PrintToSteamId(admin.SteamId, AdminUtils.AdminApi.Localizer["Limitations.MaxByDayLimit"].Value
                            .Replace("{date}", AdminUtils.GetDateString(lastPunishments[0].CreatedAt + 60*60*24))
                        );
                        return 3;
                    }
                }
            }
            var result = await BansControllFunctions.Add(ban);
            switch (result)
            {
                case 0:
                    Server.NextFrame(() => {
                        if (announce)
                            Announces.BanAdded(ban);
                        CCSPlayerController? player = null;
                        if (ban.BanIp == 0)
                            player = AdminUtils.GetControllerBySteamId(ban.SteamId!);
                        else 
                            player = AdminUtils.GetControllerByIp(ban.Ip!);
                        if (player != null)
                        {
                            DisconnectPlayer(player, ban.Reason);
                        }
                    });
                    break;
                case 1:
                    Debug("Ban already exists!");
                    break;
                case -1:
                    Debug("Some error while ban");
                    break;
            }
            return result;
        }
        catch (Exception e)
        {
            Main.AdminApi.LogError(e.ToString());
            throw;
        }
        
    }

    public async Task<int> Unban(Admin admin, string steamId, string? reason, bool announce = true)
    {
        var ban = await GetActiveBan(steamId);
        if (ban == null)
        {
            Debug("Ban not finded ✖!");
            return 1;
        }
        var result = await BansControllFunctions.Unban(admin, ban, reason);
        switch (result)
        {
            case 0:
                ban.UnbannedBy = admin.Id;
                ban.UnbanReason = reason;
                Server.NextFrame(() => {
                    if (announce)
                        Announces.Unbanned(ban);
                });
                break;
            case -1:
                Debug("Some error while unban");
                break;
        }
        return result;
    }

    public async Task<int> UnbanIp(Admin admin, string ip, string? reason, bool announce = true)
    {
        var ban = await GetActiveBanIp(ip);
        if (ban == null)
        {
            Debug("Ban not finded ✖!");
            return 1;
        }
        var result = await BansControllFunctions.UnbanIp(admin, ban, reason);
        switch (result)
        {
            case 0:
                ban.UnbannedBy = admin.Id;
                ban.UnbanReason = reason;
                Server.NextFrame(() => {
                    if (announce)
                        Announces.Unbanned(ban);
                });
                break;
            case -1:
                Debug("Some error while unban");
                break;
        }
        return result;
    }

    public async Task<PlayerBan?> GetActiveBan(string steamId)
    {
        var ban = await BansControllFunctions.GetActiveBan(steamId);
        return ban;
    }

    public async Task<List<PlayerBan>> GetAllBans(string steamId)
    {
        var ban = await BansControllFunctions.GetAllBans(steamId);
        return ban;
    }

    public async Task<PlayerBan?> GetActiveBanIp(string ip)
    {
        var ban = await BansControllFunctions.GetActiveBanIp(ip);
        return ban;
    }

    public async Task<List<PlayerBan>> GetAllIpBans(string ip)
    {
        var ban = await BansControllFunctions.GetAllIpBans(ip);
        return ban;
    }

    public bool CanDoActionWithPlayer(Admin admin, string targetId)
    {
        throw new NotImplementedException();
    }

    public void DisconnectPlayer(CCSPlayerController player, string reason, bool instantly = false)
    {
        bool advanced = Config.AdvancedKick;
        if (!advanced || instantly) 
        {
            player.Disconnect(NetworkDisconnectionReason.NETWORK_DISCONNECT_BANADDED);
            return;
        }
        player.ChangeTeam(CsTeam.Spectator);
        Main.BlockTeamChange.Add(player);
        Main.HtmlMessages.Add(player, Localizer["HTML.AdvancedKickMessage"].Value.Replace("{reason}", reason));
        Plugin.AddTimer(Config.AdvancedKickTime, () => {
            if (player != null)
            {
                Main.HtmlMessages.Remove(player);
                player.Disconnect(NetworkDisconnectionReason.NETWORK_DISCONNECT_KICKBANADDED);
            }
        });
    }

    public void DoActionWithIdentity(CCSPlayerController? actioneer, string identity, Action<CCSPlayerController> action, string[]? blockedArgs = null)
    {
        if (blockedArgs != null && blockedArgs.Contains(identity))
        {
            actioneer.Print("This identity is blocked for this action!");
            return;
        }
        if (identity == "@me" && actioneer == null)
        {
            actioneer.Print("This identity is blocked for NULL PLAYERS!");
            return;
        }
        List<CCSPlayerController> targets = new();
        switch (identity)
        {
            case "@all":
                targets = Utilities.GetPlayers().Where(x => x.IsValid).ToList();
                break;
            case "@me":
                targets = new List<CCSPlayerController>() { actioneer! };
                break;
            case "@ct":
                targets = Utilities.GetPlayers().Where(x => x.IsValid && x.TeamNum == 3).ToList();
                break;
            case "@t":
                targets = Utilities.GetPlayers().Where(x => x.IsValid && x.TeamNum == 2).ToList();
                break;
            case "@spec":
                targets = Utilities.GetPlayers().Where(x => x.IsValid && x.TeamNum == 1).ToList();
                break;
            case "@bots":
                targets = Utilities.GetPlayers().Where(x => x.IsValid && x.IsBot).ToList();
                break;
            case "@players":
                targets = Utilities.GetPlayers().Where(x => x.IsValid && !x.IsBot).ToList();
                break;
        }
        if (targets.Count > 0)
        {
            foreach (var target1 in targets)
            {
                action.Invoke(target1);
            }
            return;
        }
        if (identity.StartsWith("#"))
        {
            var target = AdminUtils.GetControllerBySteamId(identity.Remove(0, 1));
            if (target != null)
            {
                action.Invoke(target);
                return;
            }
            else {
                if (uint.TryParse(identity.Remove(0, 1), out uint uid))
                    target = AdminUtils.GetControllerByUid(uid);
                if (target != null)
                {
                    action.Invoke(target);
                    return;
                }
            }
            return;
        }
        var targetName = AdminUtils.GetControllerByName(identity);
        if (targetName != null)
        {
            action.Invoke(targetName);
            return;
        }
        Helper.Print(actioneer, Localizer["ActionError.TargetNotFound"]);
        return;
    }
    /// <summary>
    /// Нужен SteamWebApiKey установленный в кфг
    /// </summary>
    public async Task<PlayerSummaries?> GetPlayerSummaries(ulong steamId)
    {
        var webInterfaceFactory = new SteamWebInterfaceFactory(Main.AdminApi.Config.WebApiKey);
        var steamInterface = webInterfaceFactory.CreateSteamWebInterface<SteamUser>(new HttpClient());
        var playerSummaryResponse = await steamInterface.GetPlayerSummaryAsync(steamId);
        var data = playerSummaryResponse.Data;
        var summaries = new PlayerSummaries(
            data.SteamId,
            data.Nickname,
            data.ProfileUrl,
            data.AvatarUrl,
            data.AvatarFullUrl,
            data.AvatarUrl
        );
        return summaries;
    }
    /// <summary>
    /// Перезагрузка/проверка и выдача/снятие наказаний игрока
    /// </summary>
    public async Task ReloadInfractions(string steamId, string? ip = null, bool instantlyKick = false)
    {
        // Проверяем наличие бана и кикаем если есть =)
        Debug("Reload infractions for: " + steamId);
        var ban = await GetActiveBan(steamId);
        Debug("Has ban: " + (ban != null).ToString());
        if (ban != null)
        {
            Main.KickOnFullConnect.Add(steamId, instantlyKick);
            Main.KickOnFullConnectReason.Add(steamId, ban.Reason);
            return;
        }
        if (ip != null)
        {
            Debug("Reload infractions for: " + ip);
            ban = await GetActiveBanIp(ip);
            Debug("Has ip ban: " + (ban != null).ToString());
            if (ban != null)
            {
                Main.KickOnFullConnect.Add(steamId, instantlyKick);
                Main.KickOnFullConnectReason.Add(steamId, ban.Reason);
                return;
            }
        }
        var gag = await GetActiveGag(steamId);
        if (gag != null)
        {
            Server.NextFrame(() => {
                GagPlayerInGame(gag);
            });
        }
        var mute = await GetActiveMute(steamId);
        if (mute != null)
        {
            Server.NextFrame(() => {
                MutePlayerInGame(mute);
            });
        }
    }

    public void MutePlayerInGame(PlayerMute mute)
    {
        var player = AdminUtils.GetControllerBySteamId(mute.SteamId);
        if (player != null)
        {
            Mutes.Add(mute);
            player.VoiceFlags = VoiceFlags.Muted;
        }
    }
    public void UnmutePlayerInGame(PlayerMute mute)
    {
        var player = AdminUtils.GetControllerBySteamId(mute.SteamId);
        if (player != null)
        {
            player.VoiceFlags = VoiceFlags.Normal;
        }
        Mutes.Remove(mute);
    }
    public void GagPlayerInGame(PlayerGag gag)
    {
        var player = AdminUtils.GetControllerBySteamId(gag.SteamId);
        if (player != null)
        {
            Gags.Add(gag);
        }
    }
    public void UngagPlayerInGame(PlayerGag gag)
    {
        Gags.Remove(gag);
    }
    public bool IsPlayerGagged(string steamId)
    {
        var gag = Gags.FirstOrDefault(x => x.SteamId == steamId);
        return gag != null;
    }

    public async Task<PlayerMute?> GetActiveMute(string steamId)
    {
        return await MutesControllFunctions.GetActiveMute(steamId);
    }

    public async Task<List<PlayerMute>> GetAllMutes(string steamId)
    {
        return await MutesControllFunctions.GetAllMutes(steamId);
    }

    public async Task<PlayerGag?> GetActiveGag(string steamId)
    {
        return await GagsControllFunctions.GetActiveGag(steamId);
    }

    public async Task<List<PlayerGag>> GetAllGags(string steamId)
    {
        return await GagsControllFunctions.GetAllGags(steamId);
    }

    public async Task<int> AddGag(PlayerGag gag, bool announce = true)
    {
        try
        {
            Debug($"Gaging player {gag.SteamId}!");
            var reservedReason = GagsConfig.Config.Reasons.FirstOrDefault(x => x.Title.ToLower() == gag.Reason.ToLower());
            if (reservedReason != null)
            {
                Debug($"Do reservedReason transformations...");
                if (reservedReason.BanOnAllServers)
                    gag.ServerId = null;
                gag.Reason = reservedReason.Text;
            }

            var admin = gag.Admin!;
            var group = admin.Group;
            if (group != null)
            {
                var limitations = group.Limitations;
                var maxTime = group.Limitations.FirstOrDefault(x => x.LimitationKey == "max_gag_time")?.LimitationKey;
                var minTime = group.Limitations.FirstOrDefault(x => x.LimitationKey == "min_gag_time")?.LimitationKey;
                var minTimeInt = minTime == null ? 0 : int.Parse(minTime);
                var maxTimeInt = maxTime == null ? int.MaxValue : int.Parse(maxTime);
                var maxByDay = group.Limitations.FirstOrDefault(x => x.LimitationKey == "max_gags_in_day")?.LimitationKey;
                var maxByDayInt = maxByDay == null ? int.MaxValue : int.Parse(maxByDay);
                if (gag.Duration > maxTimeInt || minTimeInt > gag.Duration)
                {
                    Helper.PrintToSteamId(admin.SteamId, AdminUtils.AdminApi.Localizer["Limitations.TimeLimit"].Value
                        .Replace("{min}", minTimeInt.ToString())
                        .Replace("{max}", maxTimeInt.ToString())
                    );
                    return 3;
                }
                if (maxByDay != null)
                {
                    var lastPunishments = await GagsControllFunctions.GetLastAdminGags(admin, 60*60*24);
                    if (lastPunishments.Count > maxByDayInt)
                    {
                        Helper.PrintToSteamId(admin.SteamId, AdminUtils.AdminApi.Localizer["Limitations.MaxByDayLimit"].Value
                            .Replace("{date}", AdminUtils.GetDateString(lastPunishments[0].CreatedAt + 60*60*24))
                        );
                        return 3;
                    }
                }
            }

            var result = await GagsControllFunctions.Add(gag);
            switch (result)
            {
                case 0:
                    Server.NextFrame(() => {
                        if (announce)
                            Announces.GagAdded(gag);
                        Server.NextFrame(() => {
                            GagPlayerInGame(gag);
                        });
                    });
                    break;
                case 1:
                    Debug("Gag already exists!");
                    break;
                case -1:
                    Debug("Some error while gag");
                    break;
            }
            return result;
        }
        catch (Exception e)
        {
            Main.AdminApi.LogError(e.ToString());
            throw;
        }
    }

    public async Task<int> Ungag(Admin admin, string steamId, string? reason, bool announce = true)
    {
        Debug($"Ungag player {steamId}!");
        var gag = await GetActiveGag(steamId);
        if (gag == null)
        {
            Debug("Gag not finded ✖!");
            return 1;
        }
        var result = await GagsControllFunctions.Ungag(admin, gag, reason);
        switch (result)
        {
            case 0:
                gag.UnbannedBy = admin.Id;
                gag.UnbanReason = reason;
                Server.NextFrame(() => {
                    UngagPlayerInGame(gag);
                    if (announce)
                        Announces.Ungagged(gag);
                });
                break;
            case 2:
                Debug("Not enough permissions for ungag this player");
                break;
            case -1:
                Debug("Some error while ungag");
                break;
        }
        return result;
    }

    public async Task<int> AddMute(PlayerMute mute, bool announce = true)
    {
        try
        {
            Debug($"Muting player {mute.SteamId}!");
            var reservedReason = MutesConfig.Config.Reasons.FirstOrDefault(x => x.Title.ToLower() == mute.Reason.ToLower());
            if (reservedReason != null)
            {
                Debug($"Do reservedReason transformations..." );
                if (reservedReason.BanOnAllServers)
                    mute.ServerId = null;
                mute.Reason = reservedReason.Text;
            }

            var admin = mute.Admin!;
            var group = admin.Group;
            if (group != null)
            {
                var limitations = group.Limitations;
                var maxTime = group.Limitations.FirstOrDefault(x => x.LimitationKey == "max_mute_time")?.LimitationKey;
                var minTime = group.Limitations.FirstOrDefault(x => x.LimitationKey == "min_mute_time")?.LimitationKey;
                var minTimeInt = minTime == null ? 0 : int.Parse(minTime);
                var maxTimeInt = maxTime == null ? int.MaxValue : int.Parse(maxTime);
                var maxByDay = group.Limitations.FirstOrDefault(x => x.LimitationKey == "max_mutes_in_day")?.LimitationKey;
                var maxByDayInt = maxByDay == null ? int.MaxValue : int.Parse(maxByDay);
                if (mute.Duration > maxTimeInt || minTimeInt > mute.Duration)
                {
                    Helper.PrintToSteamId(admin.SteamId, AdminUtils.AdminApi.Localizer["Limitations.TimeLimit"].Value
                        .Replace("{min}", minTimeInt.ToString())
                        .Replace("{max}", maxTimeInt.ToString())
                    );
                    return 3;
                }
                if (maxByDay != null)
                {
                    var lastPunishments = await MutesControllFunctions.GetLastAdminMutes(admin, 60*60*24);
                    if (lastPunishments.Count > maxByDayInt)
                    {
                        Helper.PrintToSteamId(admin.SteamId, AdminUtils.AdminApi.Localizer["Limitations.MaxByDayLimit"].Value
                            .Replace("{date}", AdminUtils.GetDateString(lastPunishments[0].CreatedAt + 60*60*24))
                        );
                        return 3;
                    }
                }
            }
            var result = await MutesControllFunctions.Add(mute);
            switch (result)
            {
                case 0:
                    Server.NextFrame(() => {
                        if (announce)
                            Announces.MuteAdded(mute);
                        Server.NextFrame(() => {
                            MutePlayerInGame(mute);
                        });
                    });
                    break;
                case 1:
                    Debug("Mute already exists!");
                    break;
                case -1:
                    Debug("Some error while mute");
                    break;
            }
            return result;
        }
        catch (Exception e)
        {
            Main.AdminApi.LogError(e.ToString());
            throw;
        }
    }

    public async Task<int> Unmute(Admin admin, string steamId, string? reason, bool announce = true)
    {
        Debug($"Unmute player {steamId}!");
        var mute = await GetActiveMute(steamId);
        if (mute == null)
        {
            Debug("Mute not finded ✖!");
            return 1;
        }
        var result = await MutesControllFunctions.Unmute(admin, mute, reason);
        switch (result)
        {
            case 0:
                mute.UnbannedBy = admin.Id;
                mute.UnbanReason = reason;
                Server.NextFrame(() => {
                    UnmutePlayerInGame(mute);
                    if (announce)
                        Announces.Unmuted(mute);
                });
                break;
            case 2:
                Debug("Not enough permissions for unmute this player");
                break;
            case -1:
                Debug("Some error while unmute");
                break;
        }
        return result;
    }
}