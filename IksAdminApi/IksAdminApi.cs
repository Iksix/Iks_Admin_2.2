namespace IksAdminApi;

using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Menu;
using Microsoft.Extensions.Localization;

public interface IIksAdminApi
{
    // GLOBALS ===
    public List<PlayerMute> Mutes {get; set;}
    public List<PlayerGag> Gags {get; set;}

    public IAdminConfig Config { get; set; }
    public IStringLocalizer Localizer { get; set; }
    public BasePlugin Plugin { get; set; } 
    public string ModuleDirectory { get; set; }
    public Dictionary<string, SortMenu[]> SortMenus { get; set; }
    public Admin ConsoleAdmin {get; set;}
    public List<Admin> ServerAdmins { get; set; }
    public List<Admin> AllAdmins { get; set; }
    public List<ServerModel> AllServers { get; set; }
    public ServerModel ThisServer { get; set; }
    public List<Group> Groups {get; set;}
    public List<GroupLimitation> GroupLimitations {get; set;}
    public Dictionary<string, Dictionary<string, string>> RegistredPermissions { get; set; }
    public string DbConnectionString {get; set;}
    public Dictionary<CCSPlayerController, Action<string>> NextPlayerMessage {get;}
    public Task SendRconToAllServers(string command, bool ignoreSelf = false);
    public Task SendRconToServer(ServerModel server, string command);
    public ServerModel? GetServerByKey(string serverKey);
    public ServerModel? GetServerByIp(string ip);
    public Dictionary<string, List<CommandModel>> RegistredCommands {get; set;}
    public List<AdminModule> LoadedModules {get; set;}
    // MENU ===
    public IDynamicMenu CreateMenu(string id, string title, MenuType? type = null, MenuColors titleColor = MenuColors.Default, PostSelectAction postSelectAction = PostSelectAction.Nothing, Action<CCSPlayerController>? backAction = null, IDynamicMenu? backMenu = null);
    public void CloseMenu(CCSPlayerController player);
    // FUNC ===
    public void GagPlayerInGame(PlayerGag gag);
    public bool IsPlayerGagged(string steamId);
    public Task ReloadInfractions(string steamId, string? ip = null, bool instantlyKick = false);
    public Task<PlayerSummaries?> GetPlayerSummaries(ulong steamId);
    public void DoActionWithIdentity(CCSPlayerController? actioneer, string identity, Action<CCSPlayerController> action, string[]? blockedArgs = null);
    public void DisconnectPlayer(CCSPlayerController player, string reason, bool instantly = false);
    public bool CanDoActionWithPlayer(Admin admin, string targetId);
    public void SetCommandInititalizer(string moduleName);
    public void ClearCommandInitializer();
    public void Debug(string message);
    public void LogError(string message);
    public void RegisterPermission(string key, string defaultFlags);
    public string GetCurrentPermissionFlags(string key);
    public string GetCurrentPermissionFlags(string[] keys);
    public Task RefreshAdmins();
    public void HookNextPlayerMessage(CCSPlayerController player, Action<string> action);
    public void RemoveNextPlayerMessageHook(CCSPlayerController player);
    public void AddNewCommand(
        string command,
        string description,
        string permission,
        string usage,
        Action<CCSPlayerController, List<string>, CommandInfo> onExecute,
        CommandUsage commandUsage = CommandUsage.CLIENT_AND_SERVER,
        string? tag = null,
        string? hasNotPermissionsMessage = null,
        int minArgs = 0
    );
    // DATABASE/PUNISHMENTS FUNC ===
    /// <summary>
    /// return statuses: 0 - banned, 1 - already banned, 2 - stopped by limitations, -1 - other
    /// </summary>
    public Task<int> AddBan(PlayerBan ban, bool announce = true);
    /// <summary>
    /// return statuses: 0 - unbanned, 1 - ban not finded, 2 - admin haven't permission, -1 - other
    /// </summary>
    public Task<int> Unban(Admin admin, string steamId, string? reason, bool announce = true);
    /// <summary>
    /// return statuses: 0 - unbanned, 1 - ban not finded, 2 - admin haven't permission, -1 - other
    /// </summary>
    public Task<int> UnbanIp(Admin admin, string steamId, string? reason, bool announce = true);
    public Task<PlayerBan?> GetActiveBan(string steamId);
    public Task<List<PlayerBan>> GetAllBans(string steamId);
    public Task<PlayerBan?> GetActiveBanIp(string ip);
    public Task<List<PlayerBan>> GetAllIpBans(string ip);

    /// <summary>
    /// return statuses: 0 - gaged, 1 - already banned, 2 - stopped by limitations, -1 - other
    /// </summary>
    public Task<int> AddGag(PlayerGag ban, bool announce = true);
    /// <summary>
    /// return statuses: 0 - ungaged, 1 - ban not finded, 2 - admin haven't permission, -1 - other
    /// </summary>
    public Task<int> Ungag(Admin admin, string steamId, string? reason, bool announce = true);
    /// <summary>
    /// return statuses: 0 - gaged, 1 - already banned, 2 - stopped by limitations, -1 - other
    /// </summary>
    public Task<int> AddMute(PlayerMute ban, bool announce = true);
    /// <summary>
    /// return statuses: 0 - ungaged, 1 - ban not finded, 2 - admin haven't permission, -1 - other
    /// </summary>
    public Task<int> Unmute(Admin admin, string steamId, string? reason, bool announce = true);
    public Task<PlayerMute?> GetActiveMute(string steamId);
    public Task<List<PlayerMute>> GetAllMutes(string steamId);
    public Task<PlayerGag?> GetActiveGag(string steamId);
    public Task<List<PlayerGag>> GetAllGags(string steamId);
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
    public event Action OnReady;
    public void EOnModuleLoaded(AdminModule module);
    public void EOnModuleUnload(AdminModule module);
    public event Action<AdminModule> OnModuleUnload;
    public event Action<AdminModule> OnModuleLoaded;
}
