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
using CounterStrikeSharp.API.ValveConstants.Protobuf;
using CounterStrikeSharp.API.Modules.Utils;
using SteamWebAPI2.Utilities;
using SteamWebAPI2.Interfaces;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Timers;
namespace IksAdmin;

public class Main : BasePlugin
{
    public override string ModuleName => "IksAdmin";
    public override string ModuleVersion => "2.2";
    public override string ModuleAuthor => "iks [Discord: iks__]";

    public static IMenuApi MenuApi = null!;
    private static readonly PluginCapability<IMenuApi?> MenuCapability = new("menu:nfcore");   
    public static AdminApi AdminApi = null!;
    private readonly PluginCapability<IIksAdminApi> _pluginCapability  = new("iksadmin:core");
    
    public static List<CCSPlayerController> BlockTeamChange = new();
    public static Dictionary<string, bool> KickOnFullConnect = new();
    public static Dictionary<string, string> KickOnFullConnectReason = new();

    // INSTANT PUNISHMENT ON CONNECT
    public static Dictionary<string, PlayerComm> InstantComm = new();
    

    public static string GenerateMenuId(string id)
    {
        return $"iksadmin:menu:{id}";
    }
    public static string GenerateOptionId(string id)
    {
        return $"iksadmin:option:{id}";
    }
    public override void Load(bool hotReload)
    {
        AdminUtils.CoreInstance = this;
        AdminApi = new AdminApi(this, Localizer, ModuleDirectory, DB.ConnectionString);
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
        AdminApi.SetConfigs();
        Helper.SetSortMenus();
        AddCommandListener("say", OnSay);
        AddCommandListener("say_team", OnSay);
        AddCommandListener("jointeam", OnJoinTeam);
        InitializePermissions();
        InitializeCommands();
        RegisterListener<Listeners.OnTick>(() =>
        {
            MessageOnTick();
        });
        RegisterListener<Listeners.OnClientAuthorized>(OnAuthorized);
        AddTimer(1, () => {
            foreach (var comm in AdminApi.Comms.ToArray())
            {
                if (comm.EndAt != 0 && comm.EndAt < AdminUtils.CurrentTimestamp()) { 
                    AdminApi.RemoveCommFromPlayer(comm);
                }
            }
        }, TimerFlags.REPEAT);
    }
    
    public static void MessageOnTick()
    {
        foreach (var msg in PlayersUtils.HtmlMessages)
        {
            var player = msg.Key;
            var message = msg.Value;
            if (player == null || !player.IsValid || player.IsBot) continue;
            if (message == "") continue;
            player.PrintToCenterHtml(message);
        }
    }

    private void OnAuthorized(int playerSlot, SteamID steamId)
    {
        var steamId64 = steamId.SteamId64.ToString();
        var player = Utilities.GetPlayerFromSlot(playerSlot);
        var disconnected = AdminApi.DisconnectedPlayers.FirstOrDefault(x => x.SteamId == steamId64);
        AdminApi.DisconnectedPlayers.Remove(disconnected!);
        var ip = player!.GetIp();
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
        AdminUtils.LogDebug($"{player.PlayerName} message: {msg}");
        if (msg.StartsWith("!") || msg.StartsWith("/")) {
            if (AdminApi.NextPlayerMessage.ContainsKey(player))
            {
                AdminUtils.LogDebug("Next player message: " + msg);
                AdminApi.NextPlayerMessage[player].Invoke(msg.Remove(0, 1));
                AdminApi.RemoveNextPlayerMessageHook(player);
                return HookResult.Handled;
            }
            return HookResult.Continue;
        }
        
        var comm = player.GetComms();
        if (comm.HasGag())
        {
            var gag = comm.GetGag()!;
            Helper.Print(player, Localizer["Message.WhenGag"].Value
                .Replace("{date}", gag.EndAt == 0 ? Localizer["Other.Never"] : Utils.GetDateString(gag.EndAt))
            );
            return HookResult.Stop;
        }
        if (comm.HasSilence())
        {
            var silence = comm.GetSilence()!;
            Helper.Print(player, Localizer["Message.WhenSilence"].Value
                .Replace("{date}", silence.EndAt == 0 ? Localizer["Other.Never"] : Utils.GetDateString(silence.EndAt))
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
        AdminApi.RegisterPermission("blocks_manage.own_ban_reason", "b"); // С этим флагом у админа появляется пункт в меню для выбора собственной причины и возможность банить по кастомной причине через команду
        AdminApi.RegisterPermission("blocks_manage.own_ban_time", "b"); // С этим флагом у админа появляется пункт в меню для выбора собственного времени и банить через команду с сообственным временем
        AdminApi.RegisterPermission("blocks_manage.ban_ip", "b");
        AdminApi.RegisterPermission("blocks_manage.unban", "b");
        AdminApi.RegisterPermission("blocks_manage.unban_ip", "b");

        // MUTE
        AdminApi.RegisterPermission("comms_manage.mute", "m"); 
        AdminApi.RegisterPermission("comms_manage.unmute", "m"); 
        AdminApi.RegisterPermission("comms_manage.own_mute_reason", "m"); // С этим флагом у админа появляется пункт в меню для выбора собственной причины
        AdminApi.RegisterPermission("comms_manage.own_mute_time", "m"); 
        // SILENCE
        AdminApi.RegisterPermission("comms_manage.silence", "mg"); 
        AdminApi.RegisterPermission("comms_manage.unsilence", "mg"); 
        AdminApi.RegisterPermission("comms_manage.own_silence_reason", "mg"); // С этим флагом у админа появляется пункт в меню для выбора собственной причины
        AdminApi.RegisterPermission("comms_manage.own_silence_time", "mg"); 
        // GAG
        AdminApi.RegisterPermission("comms_manage.gag", "g"); 
        AdminApi.RegisterPermission("comms_manage.ungag", "g"); 
        AdminApi.RegisterPermission("comms_manage.own_gag_reason", "g"); // С этим флагом у админа появляется пункт в меню для выбора собственной причины
        AdminApi.RegisterPermission("comms_manage.own_gag_time", "g"); 
        // OTHER
        AdminApi.RegisterPermission("blocks_manage.remove_immunity", "i"); // Снять наказание выданное админом ниже по иммунитету
        AdminApi.RegisterPermission("blocks_manage.remove_all", "u"); // Снять наказание выданное кем угодно (кроме консоли)
        AdminApi.RegisterPermission("blocks_manage.remove_console", "c"); // Снять наказание выданное консолью
        // Players manage ===
        AdminApi.RegisterPermission("players_manage.kick", "k");
        AdminApi.RegisterPermission("players_manage.change_team", "k");
        // SERVERS MANAGE === 
        AdminApi.RegisterPermission("servers_manage.reload_data", "z");
        AdminApi.RegisterPermission("servers_manage.rcon", "z");
        // Other ===
        AdminApi.RegisterPermission("other.equals_immunity_action", "e"); // Разрешить взаймодействие с админами равными по иммунитету (Включая снятие наказаний если есть флаг blocks_manage.remove_immunity)
        AdminApi.RegisterPermission("other.admin_chat", "b");
        AdminApi.RegisterPermission("other.reload_infractions", "z");
    }
    private void InitializeCommands()
    {
        AdminApi.SetCommandInititalizer(ModuleName);
        AdminApi.AddNewCommand(
            "reload_infractions",
            "Перезагрузить данные игрока",
            "other.reload_infractions",
            "css_reload_infractions <SteamID/IP(WITHOUT PORT)>",
            CmdBase.ReloadInfractions,
            minArgs: 1,
            commandUsage: CommandUsage.CLIENT_AND_SERVER
        );
        AdminApi.AddNewCommand(
            "admin",
            "Открыть админ меню",
            ">*",
            "css_admin",
            CmdBase.AdminMenu,
            minArgs: 0,
            commandUsage: CommandUsage.CLIENT_ONLY
        );
        AdminApi.AddNewCommand(
            "kick",
            "Кикнуть игрока",
            "players_manage.kick",
            "css_kick <#uid/#steamId/name/@...> <reason>",
            CmdPlayers.Kick,
            minArgs: 0,
            commandUsage: CommandUsage.CLIENT_ONLY
        );
        AdminApi.AddNewCommand(
            "am_reload",
            "Перезагружает данные с БД",
            "servers_manage.reload_data",
            "css_am_reload",
            CmdBase.Reload,
            minArgs: 0,
            commandUsage: CommandUsage.CLIENT_AND_SERVER
        );
        AdminApi.AddNewCommand(
            "am_add",
            "Создать админа",
            "admins_manage.add",
            "css_am_add <steamId> <name> <time/0> <server_id/this> <groupName>\n" +
            "css_am_add <steamId> <name> <time/0> <server_id/this> <flags> <immunity>",
            CmdAdminManage.Add,
            minArgs: 5 
        );
        AdminApi.AddNewCommand(
            "am_add_server_id",
            "Добавить Server Id админу",
            "admins_manage.add",
            "css_am_add_server_id <steamId> <server_id/this>",
            CmdAdminManage.AddServerId,
            minArgs: 2 
        );
        AdminApi.AddNewCommand(
            "am_addflag",
            "Добавить флаг админу",
            "admins_manage.edit",
            "css_am_addflag <steamId> <flagsToAdd>",
            CmdAdminManage.AddFlag,
            minArgs: 2 
        );
        AdminApi.AddNewCommand(
            "am_addflag_or_admin",
            "Добавить флаг админу или создать админа(В случае если такого админа нет)",
            "admins_manage.edit,admins_manage.add",
            "am_addflag_or_admin <steamId> <name> <time/0> <server_id/this> <flags> <immunity>",
            CmdAdminManage.AddFlagOrAdmin,
            minArgs: 6
        );
        AdminApi.AddNewCommand(
            "am_remove",
            "Удалить админа",
            "admins_manage.edit,admins_manage.add",
            "am_remove <steamId> <server_id/this>",
            CmdAdminManage.AddFlagOrAdmin,
            minArgs: 6
        );

        // BLOCKS MANAGE ====
        // BANS ===
        AdminApi.AddNewCommand(
            "ban",
            "Забанить игрока",
            "blocks_manage.ban",
            "css_ban <#uid/#steamId/name/@...> <time> <reason>",
            CmdBans.Ban,
            minArgs: 3 
        );
        AdminApi.AddNewCommand(
            "unban",
            "Разбанить игрока",
            "blocks_manage.unban",
            "css_unban <steamId> <reason>",
            CmdBans.Unban,
            minArgs: 2 
        );
        AdminApi.AddNewCommand(
            "unbanip",
            "Разбанить игрока",
            "blocks_manage.unban_ip",
            "css_unbanip <ip> <reason>",
            CmdBans.UnbanIp,
            minArgs: 2 
        );
        AdminApi.AddNewCommand(
            "addban",
            "Забанить игрока по стим айди (оффлайн)",
            "blocks_manage.ban",
            "css_addban <steamId> <time> <reason>",
            CmdBans.AddBan,
            minArgs: 3 
        );
        AdminApi.AddNewCommand(
            "banip",
            "Забанить по айпи (онлайн)",
            "blocks_manage.ban_ip",
            "css_banip <#uid/#steamId/name/@...> <time> <reason>",
            CmdBans.BanIp,
            minArgs: 3 
        );
        AdminApi.AddNewCommand(
            "addbanip",
            "Забанить игрока по айпи (оффлайн)",
            "blocks_manage.ban_ip",
            "css_addbanip <ip> <time> <reason>",
            CmdBans.AddBanIp,
            minArgs: 3 
        );
        // GAG ===
        AdminApi.AddNewCommand(
            "gag",
            "Выдать гаг игроку (онлайн)",
            "comms_manage.gag",
            "css_gag <#uid/#steamId/name/@...> <time> <reason>",
            CmdGags.Gag,
            minArgs: 3 
        );
        AdminApi.AddNewCommand(
            "ungag",
            "Снять гаг с игрока (онлайн)",
            "comms_manage.ungag",
            "css_ungag <#uid/#steamId/name/@...> <reason>",
            CmdGags.Ungag,
            minArgs: 2 
        );
        AdminApi.AddNewCommand(
            "addgag",
            "Выдать гаг игроку (оффлайн)",
            "comms_manage.gag",
            "css_addgag <steamId> <time> <reason>",
            CmdGags.AddGag,
            minArgs: 3 
        );
        AdminApi.AddNewCommand(
            "removegag",
            "Снять гаг с игрока (оффлайн)",
            "comms_manage.ungag",
            "css_ungag <steamId> <reason>",
            CmdGags.RemoveGag,
            minArgs: 2 
        );
        // MUTE ===
        AdminApi.AddNewCommand(
            "mute",
            "Выдать мут игроку (онлайн)",
            "comms_manage.mute",
            "css_mute <#uid/#steamId/name/@...> <time> <reason>",
            CmdMutes.Mute,
            minArgs: 3 
        );
        AdminApi.AddNewCommand(
            "unmute",
            "Снять мут с игрока (онлайн)",
            "comms_manage.unmute",
            "css_unmute <#uid/#steamId/name/@...> <reason>",
            CmdMutes.Unmute,
            minArgs: 2 
        );
        AdminApi.AddNewCommand(
            "addmute",
            "Выдать мут игроку (оффлайн)",
            "comms_manage.mute",
            "css_addmute <steamId> <time> <reason>",
            CmdMutes.AddMute,
            minArgs: 3 
        );
        AdminApi.AddNewCommand(
            "removemute",
            "Снять мут с игрока (оффлайн)",
            "comms_manage.unmute",
            "css_removemute <steamId> <reason>",
            CmdMutes.RemoveMute,
            minArgs: 2 
        );
        // Silence ===
        AdminApi.AddNewCommand(
            "silence",
            "Выдать silence игроку (онлайн)",
            "comms_manage.silence",
            "css_silence <#uid/#steamId/name/@...> <time> <reason>",
            CmdSilences.Silence,
            minArgs: 3 
        );
        AdminApi.AddNewCommand(
            "unsilence",
            "Снять silence с игрока (онлайн)",
            "comms_manage.unsilence",
            "css_unsilence <#uid/#steamId/name/@...> <reason>",
            CmdSilences.UnSilence,
            minArgs: 2 
        );
        AdminApi.AddNewCommand(
            "addsilence",
            "Выдать silence игроку (оффлайн)",
            "comms_manage.silence",
            "css_addsilence <steamId> <time> <reason>",
            CmdSilences.AddSilence,
            minArgs: 3 
        );
        AdminApi.AddNewCommand(
            "removesilence",
            "Снять silence с игрока (оффлайн)",
            "comms_manage.unsilence",
            "css_removesilence <steamId> <reason>",
            CmdSilences.RemoveSilence,
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
                AdminUtils.LogDebug("Start without Menu Manager");
            }
        }
        catch (Exception)
        {
            AdminUtils.LogDebug("Start without Menu Manager");
        }
        
    }

    [GameEventHandler]
    public HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null) return HookResult.Continue;
        if (player.IsBot) return HookResult.Continue;
        var steamId = player.AuthorizedSteamID!.SteamId64.ToString();
        if (KickOnFullConnect.ContainsKey(steamId))
        {
            var reason = KickOnFullConnectReason[steamId];
            bool instantlyKick = true;
            AdminApi.DisconnectPlayer(player, reason, instantlyKick);
            return HookResult.Continue;
        }
        if (InstantComm.TryGetValue(steamId, out var comm)) {
            AdminApi.ApplyCommForPlayer(comm);
        }
        return HookResult.Continue;
    }
    
    [GameEventHandler]
    public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        var player = @event.Userid;
        BlockTeamChange.Remove(player!);
        PlayersUtils.ClearHtmlMessage(player!);
        if (player == null || player.IsBot || player.AuthorizedSteamID == null) return HookResult.Continue;
        AdminApi.DisconnectedPlayers.Insert(0, new PlayerInfo(player));
        KickOnFullConnect.Remove(player.GetSteamId());
        KickOnFullConnectReason.Remove(player.GetSteamId());
        var comms = player.GetComms();
        foreach (var comm in comms)
        {
            AdminApi.Comms.Remove(comm);
        }
        return HookResult.Continue;
    }
    
    public override void Unload(bool hotReload)
    {
        RemoveCommandListener("say", OnSay, HookMode.Pre);
        RemoveCommandListener("say_team", OnSay, HookMode.Pre);
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
    public AdminConfig Config { get; set; } 
    public BasePlugin Plugin { get; set; } 
    public IStringLocalizer Localizer { get; set; }
    public Dictionary<string, SortMenu[]> SortMenus { get; set; } = new();
    public string ModuleDirectory { get; set; }

    public List<Admin> ServerAdmins
    {
        get
        {
            return AllAdmins.Where(x => x.Servers.Contains(ThisServer.Id)).ToList();
        }
    }

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
    public async Task<DBResult> CreateAdmin(Admin actioneer, Admin admin, int? serverId)
    {
        try
        {
            var admins = await DBAdmins.GetAllAdmins(serverId, false);
            var existingAdmin = admins.FirstOrDefault(x =>
                x.SteamId == admin.SteamId && x.Servers.Contains((int)serverId!));
            if (
                // Проверка существует ли админ с таким же serverId как у добавляемого
                existingAdmin != null
            )
            {
                // Если да то обновляем админа в базе
                admin.Id = existingAdmin.Id;
                await UpdateAdmin(actioneer, admin);
                return new DBResult(admin.Id, 1, "admin has been updated");
            }

            // Если нет то добавляем админа и севрер айди к нему
            var newAdmin = await DBAdmins.AddAdminToBase(admin);
            await AddServerIdToAdmin(newAdmin.Id, serverId ?? ThisServer.Id);
            await ReloadDataFromDBOnAllServers();
            return new DBResult(newAdmin.Id, 0, "Admin has been added");
        }
        catch (Exception e)
        {
            return new DBResult(null, -1, e.ToString());
        }
    }

    public async Task AddServerIdToAdmin(int adminId, int serverId)
    {
        await DBAdmins.AddServerIdToAdmin(adminId, serverId);
    }

    public async Task RemoveServerIdFromAdmin(int adminId, int serverId)
    {
        await DBAdmins.RemoveServerIdFromAdmin(adminId, serverId);
    }

    public async Task RemoveServerIdsFromAdmin(int adminId)
    {
        await DBAdmins.RemoveServerIdsFromAdmin(adminId);
    }

    public async Task<DBResult> DeleteAdmin(Admin actioneer, Admin admin)
    {
        await DBAdmins.DeleteAdmin(admin.Id);
        await ReloadDataFromDBOnAllServers();
        return new DBResult(null, 0);
    }

    public async Task<DBResult> UpdateAdmin(Admin actioneer, Admin admin)
    {
        await DBAdmins.UpdateAdminInBase(admin);
        await ReloadDataFromDBOnAllServers();
        return new DBResult(admin.Id, 0, "Admin has been updated");
    }
    
    public async Task<List<Admin>> GetAdminsBySteamId(string steamId, bool ignoreDeleted = true)
    {
        return (await DBAdmins.GetAllAdminsBySteamId(steamId, ignoreDeleted));
    }

    private string _commandInitializer = "core";

    public Dictionary<string, List<CommandModel>> RegistredCommands {get; set;} = new Dictionary<string, List<CommandModel>>();
    public List<PlayerComm> Comms {get; set; } = new();
    public List<Warn> Warns {get; set;} = new();

    // CONFIGS ===
    public BansConfig BansConfig {get; set;} = new ();
    public MutesConfig MutesConfig {get; set;} = new ();
    public SilenceConfig SilenceConfig {get; set;} = new ();
    public GagsConfig GagsConfig {get; set;} = new ();
    public AdminConfig AdminConfig {get; set;} = new ();

    public List<PlayerInfo> DisconnectedPlayers {get; set;} = new();
    public List<AdminToServer> AdminsToServer {get; set;} = new();

    public AdminApi(BasePlugin plugin, IStringLocalizer localizer, string moduleDirectory, string dbConnectionString)
    {
        Plugin = plugin;
        SetConfigs();
        Config = AdminConfig.Config;
        var builder = new MySqlConnectionStringBuilder();
        builder.Password = Config.Password;
        builder.Server = Config.Host;
        builder.Database = Config.Database;
        builder.UserID = Config.User;
        builder.Port = uint.Parse(Config.Port);
        DB.ConnectionString = builder.ConnectionString;
        Localizer = localizer;
        ModuleDirectory = moduleDirectory;
        DbConnectionString = dbConnectionString;
        Task.Run(async () => {
            await ReloadDataFromDb();
        });
    }

    public void SetConfigs()
    {
        AdminConfig.Set();
        BansConfig.Set();
        MutesConfig.Set();
        GagsConfig.Set();
        SilenceConfig.Set();
    }

    public async Task ReloadDataFromDb(bool sendRcon = true)
    {
        if (sendRcon)
        {
            await SendRconToAllServers("css_am_reload", true);
        }
        var serverModel = new ServerModel(
                Config.ServerId,
                Config.ServerIp,
                Config.ServerName,
                Config.RconPassword
            );
        var adminsBeforeReload = ServerAdmins.ToArray();
        try
        {
            AdminUtils.LogDebug("Init Database");
            await DB.Init();
            AdminUtils.LogDebug("Refresh Servers");
            await DBServers.Add(serverModel);
            AllServers = await DBServers.GetAll();
            ThisServer = AllServers.First(x => x.Id == serverModel.Id);
            AdminUtils.LogDebug("Refresh Admins");
            await RefreshAdmins();
            Warns = await DBWarns.GetAllActive();
            Server.NextFrame(() => {
                var noAdmins = adminsBeforeReload.Where(x => !ServerAdmins.Contains(x));
                foreach (var player in noAdmins)
                {
                    var controller = player.Controller;
                    if (controller != null)
                    {
                        CloseMenu(controller);
                    }
                }
            });
        }
        catch (Exception e)
        {
            AdminUtils.LogError(e.ToString());
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

    public void ApplyCommForPlayer(PlayerComm comm)
    {
        switch (comm.MuteType)
        {
            case 0:
                GagPlayerInGame(comm);
                break;
            case 1:
                MutePlayerInGame(comm);
                break;
            case 2:
                SilencePlayerInGame(comm);
                break;
        }
    }

    private void SilencePlayerInGame(PlayerComm comm)
    {
        var player = PlayersUtils.GetControllerBySteamId(comm.SteamId);
        if (player == null) return;
        player.VoiceFlags = VoiceFlags.Muted;
        Comms.Add(comm);
    }

    public void RemoveCommFromPlayer(PlayerComm comm)
    {
        switch (comm.MuteType)
        {
            case 0:
                UnmutePlayerInGame(comm);
                break;
            case 1:
                UnGagPlayerInGame(comm);
                break;
            case 2:
                UnSilencePlayerInGame(comm);
                break;
        }
    }

    public IDynamicMenu CreateMenu(string id, string title, MenuType? type = null, MenuColors titleColor = MenuColors.Default, PostSelectAction postSelectAction = PostSelectAction.Nothing, Action<CCSPlayerController>? backAction = null, IDynamicMenu? backMenu = null)
    {
        if (type == null) type = (MenuType)Config.MenuType;
        return new DynamicMenu(id, title, (MenuType)type, titleColor, postSelectAction, backAction, backMenu);
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
            AdminUtils.LogDebug("Some event handler stopped menu opening | Id: " + menu.Id);
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
            AdminUtils.LogDebug("Some event handler skipped option render | Id: " + option.Id);
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
            AdminUtils.LogDebug("Some event handler stopped option executed | Id: " + option.Id);
            return false;
        }
        return true;
    }
    public event IIksAdminApi.OptionExecuted? OptionExecutedPost;
    public event IIksAdminApi.DynamicEvent? OnDynamicEvent;
    public HookResult InvokeDynamicEvent(EventData data)
    {
        return OnDynamicEvent?.Invoke(data) ?? HookResult.Continue;
    }

    public event IIksAdminApi.BanHandler? OnBanPre;
    public event IIksAdminApi.BanHandler? OnBanPost;
    public event IIksAdminApi.UnBanHandler? OnUnBanPre;
    public event IIksAdminApi.UnBanHandler? OnUnBanPost;
    public event IIksAdminApi.UnBanHandler? OnUnBanIpPre;
    public event IIksAdminApi.UnBanHandler? OnUnBanIpPost;
    public event IIksAdminApi.CommHandler? OnCommPre;
    public event IIksAdminApi.CommHandler? OnCommPost;
    public event IIksAdminApi.UnCommHandler? OnUnCommPre;
    public event IIksAdminApi.UnCommHandler? OnUnCommPost;
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
        await DBAdmins.RefreshAdmins();
    }
    public async Task RefreshAdminsOnAllServers()
    {
        await Main.AdminApi.SendRconToAllServers("css_am_reload_admins");
    }
    public async Task ReloadDataFromDBOnAllServers()
    {
        await Main.AdminApi.SendRconToAllServers("css_am_reload");
    }
    
    public void HookNextPlayerMessage(CCSPlayerController player, Action<string> action)
    {
        AdminUtils.LogDebug("Log next player message: " + player.PlayerName);
        NextPlayerMessage[player] = action;
    }

    public void RemoveNextPlayerMessageHook(CCSPlayerController player)
    {
        AdminUtils.LogDebug("Remove next player message hook: " + player.PlayerName);
        NextPlayerMessage.Remove(player);
    }

    public async Task SendRconToAllServers(string command, bool ignoreSelf = false)
    {
        foreach (var server in AllServers)
        {
            if (ignoreSelf && server.Ip == ThisServer.Ip) continue;
            try
            {
                await SendRconToServer(server, command);
            }
            catch (Exception)
            {
                // ignored
            }
        }
    }

    public async Task SendRconToServer(ServerModel server, string command)
    {
        var ip = server.Ip.Split(":")[0];
        var port = server.Ip.Split(":")[1];
        AdminUtils.LogDebug($"Sending rcon command [{command}] to server ({server.Name})[{server.Ip}] ...");
        using var rcon = new RCON(new IPEndPoint(IPAddress.Parse(ip), int.Parse(port)), server.Rcon ?? "");
        await rcon.ConnectAsync();
        var result = await rcon.SendCommandAsync(command);
        AdminUtils.LogDebug($"Success ✔");
        AdminUtils.LogDebug($"Response from {server.Name} [{server.Ip}]: {result}");
    }

    public ServerModel? GetServerById(int id)
    {
        return AllServers.FirstOrDefault(x => x.Id == id);
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
            AdminUtils.LogDebug($"Adding new command [{command}] was skipped from config");
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
            var perms = permission.Split(",");
            foreach (var perm in perms)
            {
                if (!p.HasPermissions(perm))
                {
                    info.Reply(notEnoughPermissionsMessage == null ? Localizer["Error.NotEnoughPermissions"] : notEnoughPermissionsMessage, tagString);
                    return;
                }
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
                AdminUtils.LogError(e.Message);
            }
            
        };
        var definition = new CommandDefinition("css_" + command, description, callback);
        Plugin.CommandManager.RegisterCommand(definition);
        RegistredCommands[_commandInitializer].Add(new CommandModel { 
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
        _commandInitializer = moduleName;
        RegistredCommands.TryAdd(_commandInitializer, new List<CommandModel>());
    }
    public void ClearCommandInitializer()
    {
        _commandInitializer = "unsorted";
    }

    public void EOnModuleLoaded(AdminModule module)
    {
        OnModuleLoaded?.Invoke(module);
    }

    public void EOnModuleUnload(AdminModule module)
    {
        OnModuleUnload?.Invoke(module);
    }

    public async Task<DBResult> AddBan(PlayerBan ban, bool announce = true)
    {
        try
        {
            AdminUtils.LogDebug($"Baning player...");
            var reservedReason = BansConfig.Config.Reasons.FirstOrDefault(x => x.Title.ToLower() == ban.Reason.ToLower());
            if (reservedReason != null)
            {
                AdminUtils.LogDebug($"Do reservedReason transformations...");
                if (reservedReason.BanOnAllServers)
                    ban.ServerId = null;
                ban.Reason = reservedReason.Text;
            }
            var admin = ban.Admin!;
            var group = admin.Group;
            if (group != null)
            {
                var limitations = group.Limitations;
                var maxTime = limitations.FirstOrDefault(x => x.LimitationKey == "max_ban_time")?.LimitationValue;
                var minTime = limitations.FirstOrDefault(x => x.LimitationKey == "min_ban_time")?.LimitationValue;
                var minTimeInt = minTime == null ? 0 : int.Parse(minTime);
                var maxTimeInt = maxTime == null ? int.MaxValue : int.Parse(maxTime);
                var maxByDay = limitations.FirstOrDefault(x => x.LimitationKey == "max_bans_in_day")?.LimitationValue;
                var maxByDayInt = maxByDay == null ? int.MaxValue : int.Parse(maxByDay);
                if (ban.Duration > maxTimeInt || minTimeInt > ban.Duration)
                {
                    Helper.PrintToSteamId(admin.SteamId, AdminUtils.AdminApi.Localizer["Limitations.TimeLimit"].Value
                        .Replace("{min}", minTimeInt.ToString())
                        .Replace("{max}", maxTimeInt.ToString())
                    );
                    return new DBResult(null, 3, "limitations limit reached");
                }
                if (maxByDay != null)
                {
                    var lastPunishments = await DBBans.GetLastAdminBans(admin, 60*60*24);
                    if (lastPunishments.Count > maxByDayInt)
                    {
                        Helper.PrintToSteamId(admin.SteamId, AdminUtils.AdminApi.Localizer["Limitations.MaxByDayLimit"].Value
                            .Replace("{date}", Utils.GetDateString(lastPunishments[0].CreatedAt + 60*60*24))
                        );
                        return new DBResult(null, 3, "limitations limit reached");
                    }
                }
            }
            
            // Проверка на существование бана
            PlayerBan? existingBan = null;
            if (ban.BanType is 1 or 2 && ban.Ip != null)
                existingBan = await GetActiveBanIp(ban.Ip);
            else if (ban.BanType is 0 or 2 && ban.SteamId != null) existingBan = await GetActiveBan(ban.SteamId);
            if (existingBan != null)
                return new DBResult(null, 1, "ban exists");
            // ====
            
            var onBanPre = OnBanPre?.Invoke(ban, ref announce) ?? HookResult.Continue;
            if (onBanPre != HookResult.Continue)
            {
                return new DBResult(null, -2, "stopped by event PRE");
            }
            
            var result = await DBBans.Add(ban);
            switch (result.QueryStatus)
            {
                case 0:
                    Server.NextFrame(() => {
                        if (announce)
                            Announces.BanAdded(ban);
                        CCSPlayerController? player = null;
                        if (ban.BanType == 0)
                            player = PlayersUtils.GetControllerBySteamId(ban.SteamId!);
                        else 
                            player = PlayersUtils.GetControllerByIp(ban.Ip!);
                        if (player != null)
                        {
                            DisconnectPlayer(player, ban.Reason, customMessageTemplate: Localizer["HTML.AdvancedBanMessage"], admin: admin,
                                disconnectionReason: NetworkDisconnectionReason.NETWORK_DISCONNECT_STEAM_BANNED);
                        }
                    });
                    break;
                case 1:
                    AdminUtils.LogDebug("Ban already exists!");
                    break;
                case -1:
                    AdminUtils.LogDebug("Some error while ban");
                    break;
            }
            var onBanPost = OnBanPost?.Invoke(ban, ref announce) ?? HookResult.Continue;
            if (onBanPost != HookResult.Continue)
            {
                return new DBResult(null, -2, "stopped by event POST");
            }
            return result;
        }
        catch (Exception e)
        {
            AdminUtils.LogError(e.ToString());
            throw;
        }
        
    }

    public async Task<DBResult> Unban(Admin admin, string steamId, string? reason, bool announce = true)
    {
        var ban = await GetActiveBan(steamId);
        if (ban == null)
        {
            AdminUtils.LogDebug("Ban not finded ✖!");
            return new DBResult(null, 1, "Ban not finded ✖!");
        }
        if (!DBBans.CanUnban(admin, ban)) return new DBResult(null, 2, "admin can't unban");
        
        var onUnBanPre = OnUnBanPre?.Invoke(admin, ref steamId, ref reason, ref announce) ?? HookResult.Continue;
        if (onUnBanPre != HookResult.Continue)
        {
            return new DBResult(null, -2, "stopped by event PRE");
        }
        
        var result = await DBBans.Unban(admin, ban, reason);
        switch (result.QueryStatus)
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
                AdminUtils.LogDebug("Some error while unban");
                break;
        }
        
        var onUnBanPost = OnUnBanPost?.Invoke(admin, ref steamId, ref reason, ref announce) ?? HookResult.Continue;
        if (onUnBanPost != HookResult.Continue)
        {
            return new DBResult(null, -2, "stopped by event PRE");
        }
        
        return result;
    }

    public async Task<DBResult> UnbanIp(Admin admin, string ip, string? reason, bool announce = true)
    {
        var ban = await GetActiveBanIp(ip);
        if (ban == null)
        {
            AdminUtils.LogDebug("Ban not finded ✖!");
            return new DBResult(null, 1, "Ban not finded ✖!");
        }
        
        if (!DBBans.CanUnban(admin, ban)) return new DBResult(null, 2, "admin can't unban");
        
        var onUnBanIpPre = OnUnBanPre?.Invoke(admin, ref ip, ref reason, ref announce) ?? HookResult.Continue;
        if (onUnBanIpPre != HookResult.Continue)
        {
            return new DBResult(null, -2, "stopped by event PRE");
        }
        
        var result = await DBBans.Unban(admin, ban, reason);
        switch (result.QueryStatus)
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
                AdminUtils.LogDebug("Some error while unban");
                break;
        }
        
        var onUnBanIpPost = OnUnBanPost?.Invoke(admin, ref ip, ref reason, ref announce) ?? HookResult.Continue;
        if (onUnBanIpPost != HookResult.Continue)
        {
            return new DBResult(null, -2, "stopped by event POST");
        }

        return result;
    }

    public async Task<PlayerBan?> GetActiveBan(string steamId)
    {
        var ban = await DBBans.GetActiveBan(steamId);
        return ban;
    }

    public async Task<List<PlayerBan>> GetAllBans(string steamId)
    {
        var ban = await DBBans.GetAllBans(steamId);
        return ban;
    }

    public async Task<PlayerBan?> GetActiveBanIp(string ip)
    {
        var ban = await DBBans.GetActiveBanIp(ip);
        return ban;
    }

    public async Task<List<PlayerBan>> GetAllIpBans(string ip)
    {
        var ban = await DBBans.GetAllIpBans(ip);
        return ban;
    }

    public async Task<List<PlayerBan>> GetLastBans(int time)
    {
        return await DBBans.GetLastBans(time);
    }

    public bool CanDoActionWithPlayer(string callerId, string targetId)
    {
        var callerAdmin = AdminUtils.ServerAdmin(callerId);
        var targetAdmin = AdminUtils.ServerAdmin(callerId);

        if (targetAdmin == null) return true;

        if (targetAdmin != null)
        {
            if (callerAdmin == null) return false;
            if (callerAdmin.HasPermissions("other.equals_immunity_action"))
            {
                if (callerAdmin.CurrentImmunity >= targetAdmin.CurrentImmunity) return true;
            } else {
                if (callerAdmin.CurrentImmunity > targetAdmin.CurrentImmunity) return true;
            }
        }

        return false;
    }

    public void DisconnectPlayer(
        CCSPlayerController player, 
        string reason, bool instantly = false, 
        string? customMessageTemplate = null, 
        Admin? admin = null, 
        string? customByAdminTemplate = null,
        NetworkDisconnectionReason? disconnectionReason = null)
    {
        var messageTemplate = customMessageTemplate ?? Localizer["HTML.AdvancedKickMessage"];
        bool advanced = Config.AdvancedKick;
        disconnectionReason = disconnectionReason ?? NetworkDisconnectionReason.NETWORK_DISCONNECT_KICKED;
        if (!advanced || instantly) 
        {
            player.Disconnect((NetworkDisconnectionReason)disconnectionReason);
            return;
        }
        player.ChangeTeam(CsTeam.Spectator);
        Main.BlockTeamChange.Add(player);
        var byAdminMessageTemplate = customByAdminTemplate ?? Localizer["HTML.ByAdminTemplate"];
        var byAdminMessage = admin != null ? byAdminMessageTemplate.Replace("{admin}", admin.Name) : "";
        for (int i = 0; i < Config.AdvancedKickTime; i++)
        {
            var sec = i;
            Plugin.AddTimer(sec, () =>
            {
                if (player == null || !player.IsValid) return;
                player.HtmlMessage(messageTemplate
                        .Replace("{reason}", reason)
                        .Replace("{time}", (Config.AdvancedKickTime - sec).ToString())
                        .Replace("{byAdmin}", byAdminMessage)
                    , Config.AdvancedKickTime);
            });
        }
        Plugin.AddTimer(Config.AdvancedKickTime, () => {
            if (player != null)
            {
                player.ClearHtmlMessage();
                player.Disconnect((NetworkDisconnectionReason)disconnectionReason);
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
            var target = PlayersUtils.GetControllerBySteamId(identity.Remove(0, 1));
            if (target != null)
            {
                action.Invoke(target);
                return;
            }
            else {
                if (uint.TryParse(identity.Remove(0, 1), out uint uid))
                    target = PlayersUtils.GetControllerByUid(uid);
                if (target != null)
                {
                    action.Invoke(target);
                    return;
                }
            }
            return;
        }
        var targetName = PlayersUtils.GetControllerByName(identity);
        if (targetName != null)
        {
            action.Invoke(targetName);
            return;
        }
        
        Helper.Print(actioneer, Localizer["ActionError.TargetNotFound"]);
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
        AdminUtils.LogDebug("Reload infractions for: " + steamId);
        var ban = await GetActiveBan(steamId);
        AdminUtils.LogDebug("Has ban: " + (ban != null).ToString());
        if (ban != null)
        {
            Main.KickOnFullConnect.Add(steamId, instantlyKick);
            Main.KickOnFullConnectReason.Add(steamId, ban.Reason);
            return;
        }
        if (ip != null && !Config.MirrorsIp.Contains(ip))
        {
            
            AdminUtils.LogDebug("Check ban for: " + ip);
            ban = await GetActiveBanIp(ip);
            AdminUtils.LogDebug("Has ip ban: " + (ban != null).ToString());
            if (ban != null)
            {
                Main.KickOnFullConnect.Add(steamId, instantlyKick);
                Main.KickOnFullConnectReason.Add(steamId, ban.Reason);
                return;
            }
        }
        var comms = await GetActiveComms(steamId);
        Server.NextFrame(() => {
            foreach (var comm in comms)
            {
                ApplyCommForPlayer(comm);
            }
        });
        AdminUtils.LogDebug("Has gag: " + comms.HasGag());
        AdminUtils.LogDebug("Has mute: " + comms.HasMute());
        AdminUtils.LogDebug("Has silence: " + comms.HasSilence());
        AdminUtils.LogDebug("Getting admin data");
        var admins = await GetAdminsBySteamId(steamId);
        if (admins.Count == 0)
        {
            AdminUtils.LogDebug("Admin data is empty \u2716");
            return;
        }

        List<int> correctIds = new(); // ID валидных админов
        for (int i = 0; i < AllAdmins.Count; i++)
        {
            var a = AllAdmins[i];
            var playerAdmin = admins.FirstOrDefault(x => x.Id == a.Id);
            if (playerAdmin == null) continue;
            AllAdmins[i] = playerAdmin;
            correctIds.Add(playerAdmin.Id);
            AdminUtils.LogDebug("Update ID on server: " + playerAdmin.Id);
        }
        var adminsForDelete = AllAdmins.Where(x => x.SteamId == steamId && !correctIds.Contains(x.Id)).ToList();
        foreach (var adminForDelete in adminsForDelete)
        {
            AdminUtils.LogDebug("Remove non valid ids: " + adminForDelete.Id);
            AllAdmins.Remove(adminForDelete);
        }
    }

    public void MutePlayerInGame(PlayerComm mute)
    {
        var player = PlayersUtils.GetControllerBySteamId(mute.SteamId);
        if (player != null)
        {
            AdminUtils.LogDebug($"Mute player: {mute.Name} | {mute.SteamId} in game!");
            Comms.Add(mute);
            player.VoiceFlags = VoiceFlags.Muted;
        } else {
            Main.InstantComm.Add(mute.SteamId, mute);
        }
    }
    public void UnmutePlayerInGame(PlayerComm mute)
    {
        var player = PlayersUtils.GetControllerBySteamId(mute.SteamId);
        if (player != null)
        {
            Helper.Print(player, Localizer["Message.WhenMuteEnd"]);
            player.VoiceFlags = VoiceFlags.Normal;
        }
        var exComm = Comms.GetMute();
        Comms.Remove(exComm!);
    }
    
    public void GagPlayerInGame(PlayerComm gag)
    {
        var player = PlayersUtils.GetControllerBySteamId(gag.SteamId);
        if (player != null)
        {
            AdminUtils.LogDebug($"Gag player: {gag.Name} | {gag.SteamId} in game!");
            Comms.Add(gag);
        } else {
            Main.InstantComm.Add(gag.SteamId, gag);
        }
    }
    public void UnGagPlayerInGame(PlayerComm gag)
    {
        var player = PlayersUtils.GetControllerBySteamId(gag.SteamId);
        if (player != null)
        {
            Helper.Print(player, Localizer["Message.WhenGagEnd"]);
        }
        var exGag = Comms.GetGag();
        Comms.Remove(exGag!);
    }
    private void UnSilencePlayerInGame(PlayerComm comm)
    {
        var player = PlayersUtils.GetControllerBySteamId(comm.SteamId);
        if (player != null)
        {
            Helper.Print(player, Localizer["Message.WhenSilenceEnd"]);
            player.VoiceFlags = VoiceFlags.Normal;
        }
        var exComm = Comms.GetSilence();
        Comms.Remove(exComm!);
    }


    public async Task<DBResult> UnComm(Admin admin, PlayerComm comm, bool announce = true)
    {
        DBResult result = new DBResult(null, -1, "ERROR!");
        switch (comm.MuteType)
        {
            case 0:
                result = await UnMute(admin, comm.SteamId, comm.UnbanReason, announce);
                break;
            case 1:
                result = await UnGag(admin, comm.SteamId, comm.UnbanReason, announce);
                break;
            case 2:
                result = await UnSilence(admin, comm.SteamId, comm.UnbanReason, announce);
                break;
        }

        return result;
    }

    private async Task<DBResult> UnSilence(Admin admin, string steamId, string? reason, bool announce)
    {
        AdminUtils.LogDebug($"Ungag player {steamId}!");
        var comms = await GetActiveComms(steamId);
        if (!comms.HasSilence())
        {
            AdminUtils.LogDebug("Silence not finded ✖!");
            return new DBResult(0, 1, "Silence not finded ✖!");
        }
        
        var onUnCommPre = OnUnCommPre?.Invoke(admin, ref steamId, ref reason, ref announce) ?? HookResult.Continue;
        if (onUnCommPre != HookResult.Continue)
        {
            return new DBResult(null, -2, "Stopped by event PRE");
        }
        
        var silence = comms.GetSilence()!;
        var result = await DBComms.UnComm(admin, silence, reason);
        switch (result.QueryStatus)
        {
            case 0:
                silence.UnbannedBy = admin.Id;
                silence.UnbanReason = reason;
                Server.NextFrame(() => {
                    UnSilencePlayerInGame(silence);
                    if (announce)
                        Announces.UnSilenced(silence);
                });
                break;
            case 2:
                AdminUtils.LogDebug("Not enough permissions for unSilence this player");
                break;
            case -1:
                AdminUtils.LogDebug("Some error while unSilence");
                break;
        }
        var onUnCommPost = OnUnCommPost?.Invoke(admin, ref steamId, ref reason, ref announce) ?? HookResult.Continue;
        if (onUnCommPost != HookResult.Continue)
        {
            return new DBResult(null, -2, "Stopped by event PRE");
        }
        return result;
    }

    

    public async Task<List<PlayerComm>> GetActiveComms(string steamId)
    {
        return await DBComms.GetActiveComms(steamId);
    }

    public async Task<List<PlayerComm>> GetAllComms(string steamId)
    {
        return await DBComms.GetAllComms(steamId);
    }

    public async Task<List<PlayerComm>> GetLastComms(int time)
    {
        return await DBComms.GetLastComms(time);
    }

    public async Task<DBResult> AddComm(PlayerComm comm, bool announce = true)
    {
        DBResult result = new DBResult(-1, -1, "ERROR!");
        try
        {
            switch (comm.MuteType)
            {
                case 0:
                    result = await AddMute(comm, announce);
                    break;
                case 1:
                    result = await AddGag(comm, announce);
                    break;
                case 2:
                    result = await AddSilence(comm, announce);
                    break;
            }
        }
        catch (Exception e)
        {
            AdminUtils.LogError(e.ToString());
            throw;
        }
        return result;
    }

    private async Task<DBResult> AddSilence(PlayerComm comm, bool announce)
    {
        try
        {
            comm.MuteType = 2;
            AdminUtils.LogDebug($"Silence player {comm.SteamId}!");
            var reservedReason = SilenceConfig.Config.Reasons.FirstOrDefault(x => x.Title.ToLower() == comm.Reason.ToLower());
            if (reservedReason != null)
            {
                AdminUtils.LogDebug("Do reservedReason transformations...");
                if (reservedReason.BanOnAllServers)
                    comm.ServerId = null;
                comm.Reason = reservedReason.Text;
            }

            var admin = comm.Admin!;
            var group = admin.Group;
            if (group != null)
            {
                var limitations = group.Limitations;
                var maxTime = limitations.FirstOrDefault(x => x.LimitationKey == "max_silence_time")?.LimitationValue;
                var minTime = limitations.FirstOrDefault(x => x.LimitationKey == "min_silence_time")?.LimitationValue;
                var minTimeInt = minTime == null ? 0 : int.Parse(minTime);
                var maxTimeInt = maxTime == null ? int.MaxValue : int.Parse(maxTime);
                var maxByDay = limitations.FirstOrDefault(x => x.LimitationKey == "max_silences_in_day")?.LimitationValue;
                var maxByDayInt = maxByDay == null ? int.MaxValue : int.Parse(maxByDay);
                if (comm.Duration/60 > maxTimeInt || minTimeInt > comm.Duration/60)
                {
                    Helper.PrintToSteamId(admin.SteamId, AdminUtils.AdminApi.Localizer["Limitations.TimeLimit"].Value
                        .Replace("{min}", minTimeInt.ToString())
                        .Replace("{max}", maxTimeInt.ToString())
                    );
                    return new DBResult(null, 3, "limitations limit reached");;
                }
                if (maxByDay != null)
                {
                    var lastPunishments = (await DBComms.GetLastAdminComms(admin, 60 * 60 * 24)).Where(x => x.MuteType == 1).ToList();
                    if (lastPunishments.Count > maxByDayInt)
                    {
                        Helper.PrintToSteamId(admin.SteamId, AdminUtils.AdminApi.Localizer["Limitations.MaxByDayLimit"].Value
                            .Replace("{date}", Utils.GetDateString(lastPunishments[0].CreatedAt + 60*60*24))
                        );
                        return new DBResult(null, 3, "limitations limit reached");;
                    }
                }
            }
            
            var existingComm = await GetActiveComms(comm.SteamId);
            if (existingComm != null && existingComm.Any(x => x.MuteType == comm.MuteType || x.MuteType == 2))
                return new DBResult(null, 1, "Already banned");
            
            var onCommPre = OnCommPre?.Invoke(comm, ref announce) ?? HookResult.Continue;
            if (onCommPre != HookResult.Continue)
            {
                return new DBResult(null, -2, "Stopped by event PRE");
            }

            var result = await DBComms.Add(comm);
            switch (result.QueryStatus)
            {
                case 0:
                    comm.Id = result.ElementId ?? 0;
                    Server.NextFrame(() => {
                        if (announce)
                            Announces.SilenceAdded(comm);
                        Server.NextFrame(() => {
                            SilencePlayerInGame(comm);
                        });
                    });
                    break;
                case 1:
                    AdminUtils.LogDebug("Silence already exists!");
                    break;
                case -1:
                    AdminUtils.LogDebug("Some error while silence");
                    break;
            }
            
            var onCommPost = OnCommPost?.Invoke(comm, ref announce) ?? HookResult.Continue;
            if (onCommPost != HookResult.Continue)
            {
                return new DBResult(null, -2, "Stopped by event POST");
            }
            
            return result;
        }
        catch (Exception e)
        {
            AdminUtils.LogError(e.ToString());
            throw;
        }
    }

    public async Task<DBResult> AddGag(PlayerComm comm, bool announce = true)
    {
        try
        {
            comm.MuteType = 1;
            AdminUtils.LogDebug($"Gaging player {comm.SteamId}!");
            var reservedReason = GagsConfig.Config.Reasons.FirstOrDefault(x => x.Title.ToLower() == comm.Reason.ToLower());
            if (reservedReason != null)
            {
                AdminUtils.LogDebug($"Do reservedReason transformations...");
                if (reservedReason.BanOnAllServers)
                    comm.ServerId = null;
                comm.Reason = reservedReason.Text;
            }

            var admin = comm.Admin!;
            var group = admin.Group;
            if (group != null)
            {
                var limitations = group.Limitations;
                var maxTime = limitations.FirstOrDefault(x => x.LimitationKey == "max_gag_time")?.LimitationValue;
                var minTime = limitations.FirstOrDefault(x => x.LimitationKey == "min_gag_time")?.LimitationValue;
                var minTimeInt = minTime == null ? 0 : int.Parse(minTime);
                var maxTimeInt = maxTime == null ? int.MaxValue : int.Parse(maxTime);
                var maxByDay = limitations.FirstOrDefault(x => x.LimitationKey == "max_gags_in_day")?.LimitationValue;
                var maxByDayInt = maxByDay == null ? int.MaxValue : int.Parse(maxByDay);
                if (comm.Duration/60 > maxTimeInt || minTimeInt > comm.Duration/60)
                {
                    Helper.PrintToSteamId(admin.SteamId, AdminUtils.AdminApi.Localizer["Limitations.TimeLimit"].Value
                        .Replace("{min}", minTimeInt.ToString())
                        .Replace("{max}", maxTimeInt.ToString())
                    );
                    return new DBResult(null, 3, "limitations limit reached");
                }
                if (maxByDay != null)
                {
                    var lastPunishments = (await DBComms.GetLastAdminComms(admin, 60 * 60 * 24)).Where(x => x.MuteType == 1).ToList();
                    if (lastPunishments.Count > maxByDayInt)
                    {
                        Helper.PrintToSteamId(admin.SteamId, AdminUtils.AdminApi.Localizer["Limitations.MaxByDayLimit"].Value
                            .Replace("{date}", Utils.GetDateString(lastPunishments[0].CreatedAt + 60*60*24))
                        );
                        return new DBResult(null, 3, "limitations limit reached");
                    }
                }
            }
            
            var existingComm = await GetActiveComms(comm.SteamId);
            if (existingComm != null && existingComm.Any(x => x.MuteType == comm.MuteType || x.MuteType == 2))
                return new DBResult(null, 1, "Already banned");
            
            var onCommPre = OnCommPre?.Invoke(comm, ref announce) ?? HookResult.Continue;
            if (onCommPre != HookResult.Continue)
            {
                return new DBResult(null, -2, "Stopped by event PRE");
            }
            var result = await DBComms.Add(comm);
            switch (result.QueryStatus)
            {
                case 0:
                    comm.Id = result.ElementId ?? 0;
                    Server.NextFrame(() => {
                        if (announce)
                            Announces.GagAdded(comm);
                        Server.NextFrame(() => {
                            GagPlayerInGame(comm);
                        });
                    });
                    break;
                case 1:
                    AdminUtils.LogDebug("Gag already exists!");
                    break;
                case -1:
                    AdminUtils.LogDebug("Some error while gag");
                    break;
            }
            
            var onCommPost = OnCommPost?.Invoke(comm, ref announce) ?? HookResult.Continue;
            if (onCommPost != HookResult.Continue)
            {
                return new DBResult(null, -2, "Stopped by event POST");
            }
            
            return result;
        }
        catch (Exception e)
        {
            AdminUtils.LogError(e.ToString());
            throw;
        }
    }

    public async Task<DBResult> UnGag(Admin admin, string steamId, string? reason, bool announce = true)
    {
        AdminUtils.LogDebug($"Ungag player {steamId}!");
        var comms = await GetActiveComms(steamId);
        if (!comms.HasGag())
        {
            AdminUtils.LogDebug("Gag not finded ✖!");
            return new DBResult(0, 1, "Gag not finded ✖!");
        }
        
        var onUnCommPre = OnUnCommPre?.Invoke(admin, ref steamId, ref reason, ref announce) ?? HookResult.Continue;
        if (onUnCommPre != HookResult.Continue)
        {
            return new DBResult(null, -2, "Stopped by event PRE");
        }

        var gag = comms.GetGag()!;
        var result = await DBComms.UnComm(admin, comms.GetGag()!, reason);
        switch (result.QueryStatus)
        {
            case 0:
                gag.UnbannedBy = admin.Id;
                gag.UnbanReason = reason;
                Server.NextFrame(() => {
                    UnGagPlayerInGame(gag);
                    if (announce)
                        Announces.UnGagged(gag);
                });
                break;
            case 2:
                AdminUtils.LogDebug("Not enough permissions for ungag this player");
                break;
            case -1:
                AdminUtils.LogDebug("Some error while ungag");
                break;
        }
        
        var onUnCommPost = OnUnCommPost?.Invoke(admin, ref steamId, ref reason, ref announce) ?? HookResult.Continue;
        if (onUnCommPost != HookResult.Continue)
        {
            return new DBResult(null, -2, "Stopped by event PRE");
        }
        
        return result;
    }

    public async Task<DBResult> AddMute(PlayerComm comm, bool announce = true)
    {
        try
        {
            comm.MuteType = 0;
            AdminUtils.LogDebug($"Muting player {comm.SteamId}!");
            var reservedReason = MutesConfig.Config.Reasons.FirstOrDefault(x => x.Title.ToLower() == comm.Reason.ToLower());
            if (reservedReason != null)
            {
                AdminUtils.LogDebug($"Do reservedReason transformations..." );
                if (reservedReason.BanOnAllServers)
                    comm.ServerId = null;
                comm.Reason = reservedReason.Text;
            }

            var admin = comm.Admin!;
            var group = admin.Group;
            if (group != null)
            {
                var limitations = group.Limitations;
                var maxTime = limitations.FirstOrDefault(x => x.LimitationKey == "max_mute_time")?.LimitationValue;
                var minTime = limitations.FirstOrDefault(x => x.LimitationKey == "min_mute_time")?.LimitationValue;
                var minTimeInt = minTime == null ? 0 : int.Parse(minTime);
                var maxTimeInt = maxTime == null ? int.MaxValue : int.Parse(maxTime);
                var maxByDay = limitations.FirstOrDefault(x => x.LimitationKey == "max_mutes_in_day")?.LimitationValue;
                var maxByDayInt = maxByDay == null ? int.MaxValue : int.Parse(maxByDay);
                if (comm.Duration/60 > maxTimeInt || minTimeInt > comm.Duration/60)
                {
                    Helper.PrintToSteamId(admin.SteamId, AdminUtils.AdminApi.Localizer["Limitations.TimeLimit"].Value
                        .Replace("{min}", minTimeInt.ToString())
                        .Replace("{max}", maxTimeInt.ToString())
                    );
                    return new DBResult(null, 3, "limitations limit reached");
                }
                if (maxByDay != null)
                {
                    var lastPunishments = (await DBComms.GetLastAdminComms(admin, 60 * 60 * 24)).Where(x => x.MuteType == 0).ToList();
                    if (lastPunishments.Count > maxByDayInt)
                    {
                        Helper.PrintToSteamId(admin.SteamId, AdminUtils.AdminApi.Localizer["Limitations.MaxByDayLimit"].Value
                            .Replace("{date}", Utils.GetDateString(lastPunishments[0].CreatedAt + 60*60*24))
                        );
                        return new DBResult(null, 3, "limitations limit reached");
                    }
                }
            }
            
            var existingComm = await GetActiveComms(comm.SteamId);
            if (existingComm != null && existingComm.Any(x => x.MuteType == comm.MuteType || x.MuteType == 2))
                return new DBResult(null, 1, "Already banned");
            
            var onCommPre = OnCommPre?.Invoke(comm, ref announce) ?? HookResult.Continue;
            if (onCommPre != HookResult.Continue)
            {
                return new DBResult(null, -2, "Stopped by event PRE");
            }
            
            var result = await DBComms.Add(comm);
            switch (result.QueryStatus)
            {
                case 0:
                    comm.Id = result.ElementId ?? 0;
                    Server.NextFrame(() => {
                        if (announce)
                            Announces.MuteAdded(comm);
                        Server.NextFrame(() => {
                            MutePlayerInGame(comm);
                        });
                    });
                    break;
                case 1:
                    AdminUtils.LogDebug("Mute already exists!");
                    break;
                case -1:
                    AdminUtils.LogDebug("Some error while mute");
                    break;
            }
            
            var onCommPost = OnCommPost?.Invoke(comm, ref announce) ?? HookResult.Continue;
            if (onCommPost != HookResult.Continue)
            {
                return new DBResult(null, -2, "Stopped by event POST");
            }
            
            return result;
        }
        catch (Exception e)
        {
            AdminUtils.LogError(e.ToString());
            throw;
        }
    }

    public async Task<DBResult> UnMute(Admin admin, string steamId, string? reason, bool announce = true)
    {
        AdminUtils.LogDebug($"Unmute player {steamId}!");
        var comms = await DBComms.GetActiveComms(steamId);
        if (!comms.HasMute())
        {
            AdminUtils.LogDebug("Mute not finded ✖!");
            return new DBResult(0, 1, "Mute not finded ✖!");
        }
        
        var onUnCommPre = OnUnCommPre?.Invoke(admin, ref steamId, ref reason, ref announce) ?? HookResult.Continue;
        if (onUnCommPre != HookResult.Continue)
        {
            return new DBResult(null, -2, "Stopped by event PRE");
        }
        
        var mute = comms.GetMute()!;
        var result = await DBComms.UnComm(admin, mute, reason);
        switch (result.QueryStatus)
        {
            case 0:
                mute.UnbannedBy = admin.Id;
                mute.UnbanReason = reason;
                Server.NextFrame(() => {
                    UnmutePlayerInGame(mute);
                    if (announce)
                        Announces.UnMuted(mute);
                });
                break;
            case 2:
                AdminUtils.LogDebug("Not enough permissions for unmute this player");
                break;
            case -1:
                AdminUtils.LogDebug("Some error while unmute");
                break;
        }
        
        var onUnCommPost = OnUnCommPost?.Invoke(admin, ref steamId, ref reason, ref announce) ?? HookResult.Continue;
        if (onUnCommPost != HookResult.Continue)
        {
            return new DBResult(null, -2, "Stopped by event PRE");
        }
        
        return result;
    }
    
    public void Kick(Admin admin, CCSPlayerController player, string reason)
    {
        AdminUtils.LogDebug($"Kicking player {player.PlayerName}...");
        var eventData = new EventData("kick_player_pre");
        eventData.Insert("admin", admin);
        eventData.Insert("player", player);
        eventData.Insert("reason", reason);

        var onKickEventPre = OnDynamicEvent?.Invoke(eventData) ?? HookResult.Continue;
        if (onKickEventPre != HookResult.Continue)
        {
            AdminUtils.LogDebug("Stopped by event PRE");
            return;
        }

        admin = eventData.Get<Admin>("admin");
        player = eventData.Get<CCSPlayerController>("player");
        reason = eventData.Get<string>("reason");
        
        DisconnectPlayer(player, reason, admin: admin);
        eventData.EventKey = "kick_player_post";
        _ = OnDynamicEvent?.Invoke(eventData) ?? HookResult.Continue;
    }

    public async Task<DBResult> CreateGroup(Group group)
    {
        var result = await DBGroups.AddGroup(group);
        await ReloadDataFromDBOnAllServers();
        return result;
    }

    public async Task<DBResult> UpdateGroup(Group group)
    {
        var result = await DBGroups.UpdateGroupInBase(group);
        await ReloadDataFromDBOnAllServers();
        return result;
    }

    public async Task<DBResult> DeleteGroup(Group group)
    {
        var result = await DBGroups.DeleteGroup(group);
        await ReloadDataFromDBOnAllServers();
        return result;
    }

    public async Task<List<Group>> GetAllGroups()
    {
        return await DBGroups.GetAllGroups();
    }

    public async Task<DBResult> CreateWarn(Warn warn)
    {
        var eData = new EventData("create_warn");
        eData.Insert("warn", warn);
        if (eData.Invoke() != HookResult.Continue)
        {
            return new DBResult(null, -2, "Stopped by event WARN");
        }
        warn = eData.Get<Warn>("warn");
        var result = await warn.InsertToBase();
        if (result.ElementId != null) 
            Warns.Add(warn);
        return result;
    }

    public async Task<DBResult> UpdateWarn(Warn warn)
    {
        var eData = new EventData("update_warn");
        eData.Insert("warn", warn);
        if (eData.Invoke() != HookResult.Continue)
        {
            return new DBResult(null, -2, "Stopped by event WARN");
        }
        warn = eData.Get<Warn>("warn");
        var exWarn = Warns.FirstOrDefault(x => x.Id == warn.Id);
        if (exWarn != null)
            exWarn = warn;
        return await warn.UpdateInBase();
    }

    public async Task<DBResult> DeleteWarn(Admin admin, Warn warn)
    {
        warn.DeletedBy = admin.Id;
        warn.DeletedAt = AdminUtils.CurrentTimestamp();
        var eData = new EventData("delete_warn");
        eData.Insert("warn", warn);
        if (eData.Invoke() != HookResult.Continue)
        {
            return new DBResult(null, -2, "Stopped by event WARN");
        }
        warn = eData.Get<Warn>("warn");
        return await warn.UpdateInBase();
    }

    public async Task<List<Warn>> GetAllWarns()
    {
        return await DBWarns.GetAll();
    }

    public async Task<List<Warn>> GetAllWarnsByAdmin(Admin admin)
    {
        return await DBWarns.GetAllActiveByAdmin(admin.Id);
    }
    public async Task<List<Warn>> GetAllWarnsForAdmin(Admin admin)
    {
        return await DBWarns.GetAllActiveForAdmin(admin.Id);
    }
}