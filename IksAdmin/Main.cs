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
using Steam.Models.SteamCommunity;
using SteamWebAPI2.Interfaces;
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
        AdminApi.RegisterPermission("blocks_manage.ban", "b");
        AdminApi.RegisterPermission("blocks_manage.mute", "m"); 
        AdminApi.RegisterPermission("blocks_manage.gag", "g"); 
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
        AdminApi.AddNewCommand(
            "ban",
            "Забанить игрока",
            "blocks_manage.ban",
            "css_ban <#uid/#steamId/name> <time> <reason>",
            BlocksManageCommands.Ban,
            minArgs: 3 
        );
        AdminApi.AddNewCommand(
            "addban",
            "Забанить игрока по стим айди (оффлайн)",
            "blocks_manage.ban",
            "css_addban <steamId> <time> <reason>",
            BlocksManageCommands.AddBan,
            minArgs: 3 
        );
        AdminApi.AddNewCommand(
            "banip",
            "Забанить игрока по айпи (онлайн)",
            "blocks_manage.ban",
            "css_ban <#uid/#steamId/name> <time> <reason>",
            BlocksManageCommands.Ban,
            minArgs: 3 
        );
        AdminApi.AddNewCommand(
            "addbanip",
            "Забанить игрока по айпи (оффлайн)",
            "blocks_manage.ban",
            "css_addban <steamId> <time> <reason>",
            BlocksManageCommands.AddBan,
            minArgs: 3 
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
    public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        HtmlMessages.Remove(@event.Userid!);
        HtmlMessagesTimer.Remove(@event.Userid!);
        BlockTeamChange.Remove(@event.Userid!);
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
                    Server.NextFrame(() => {
                        var controller = ban.Admin!.Controller;
                        if (controller != null)
                        {
                            Helper.Print(controller, Localizer["ActionError.AlreadyBanned"]);
                        }
                    });
                    break;
                case -1:
                    Server.NextFrame(() => {
                        var controller = ban.Admin!.Controller;
                        if (controller != null)
                        {
                            Helper.Print(controller, Localizer["ActionError.Other"]);
                        }
                    });
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
        var result = await BansControllFunctions.Unban(admin, steamId, reason);
        return result;
    }

    public async Task<int> UnbanIp(Admin admin, string ip, string? reason, bool announce = true)
    {
        var result = await BansControllFunctions.UnbanIp(admin, ip, reason);
        return result;
    }

    public async Task<PlayerBan?> GetActiveBan(string steamId)
    {
        throw new NotImplementedException();
    }

    public async Task<List<PlayerBan>> GetAllBans(string steamId)
    {
        throw new NotImplementedException();
    }

    public async Task<PlayerBan?> GetActiveBanIp(string ip)
    {
        throw new NotImplementedException();
    }

    public async Task<List<PlayerBan>> GetAllIpBans(string ip)
    {
        throw new NotImplementedException();
    }

    public bool CanDoActionWithPlayer(Admin admin, string targetId)
    {
        throw new NotImplementedException();
    }

    public void DisconnectPlayer(CCSPlayerController player, string reason)
    {
        bool advanced = Config.AdvancedKick;
        if (!advanced) 
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
    public async Task<PlayerSummaries> GetPlayerSummaries(ulong steamId)
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
}