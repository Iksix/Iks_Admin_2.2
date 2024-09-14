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
    public Admin ConsoleAdmin {get; set;}
    public List<Admin> ServerAdmins { get; set; }
    public List<Admin> AllAdmins { get; set; }
    public List<Group> Groups {get; set;}
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
    public string ServerKey { get; set; } 
    // DATABASE ===
    public string Host { get; set; } 
    public string Database { get; set; } 
    public string User { get; set; } 
    public string Password { get; set; }
    public string Port { get; set; }
    // ===
    public int MenuType { get; set; }
    public Dictionary<string, string> PermissionReplacement {get; set;}
    public bool DebugMode { get; set; }
}
public interface IDynamicMenu
{
    public string Id {get; set;}
    public string Title {get; set;}
    public MenuColors TitleColor {get; set;}
    public MenuType Type {get; set;}
    public Action<CCSPlayerController>? BackAction {get; set;}
    public PostSelectAction PostSelectAction {get; set;}
    public void Open(CCSPlayerController player, bool useSortMenu = true);
    public void AddMenuOption(string id, string title, Action<CCSPlayerController, IDynamicMenuOption> onExecute, MenuColors? color = null, bool disabled = false, string viewFlags = "*");
}
public interface IDynamicMenuOption
{
    public string Id {get; set;}
    public string Title {get; set;}
    public MenuColors Color {get; set;}
    public string ViewFlags {get; set;}
    public Action<CCSPlayerController, IDynamicMenuOption> OnExecute {get; set;}
}

public class Admin 
{
    public int Id {get; set;}
    public string SteamId {get; set;} = "";
    public string Name {get; set;}
    public string? Flags {get; set;}
    public int? Immunity {get; set;}
    public int? GroupId {get; set;} = null;
    public string? ServerKey {get; set;}
    public int Disabled {get; set;}
    public int CreatedAt {get; set;}
    public int UpdatedAt {get; set;}
    public int? DeletedAt {get; set;} = null;
    public bool Online {get {
        return AdminUtils.GetControllerBySteamId(SteamId) != null;
    }}
    public string CurrentFlags { get {
        return Flags ?? "";
    }}
    public bool IsDisabled {get {
        return Disabled == 1;
    }}
    public List<string> ServerKeys { get  {
        var keys = new List<string>();
        if (ServerKey != null && ServerKey.Length > 0)
        {
            foreach (var key in ServerKey.Split(";"))
            {
                keys.Add(key);
            }
        }
        return keys;
    } }
    public CCSPlayerController? Controller { get => AdminUtils.GetControllerBySteamId(SteamId); } 

    /// <summary>
    /// For getting from db
    /// </summary>
    public Admin(int id, string steamId, string name, string? flags, int? immunity, int? groupId, string serverKey, int isDisabled, int createdAt, int updatedAt, int? deletedAt)
    {
        Id = id;
        SteamId = steamId;
        Name = name;
        Flags = flags;
        Immunity = immunity;
        GroupId = groupId;
        Disabled = isDisabled;
        ServerKey = serverKey;  
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        DeletedAt = deletedAt;
    }
    /// <summary>
    /// For creating new admin
    /// </summary>
    public Admin(string steamId, string name, string? flags = null, int? immunity = null, int? groupId = null, string? serverKey = null)
    {
        SteamId = steamId;
        Name = name;
        Flags = flags;
        Immunity = immunity;
        GroupId = groupId;
        ServerKey = serverKey;
        CreatedAt = AdminUtils.CurrentTimestamp();
        UpdatedAt = AdminUtils.CurrentTimestamp();
    }
}

public class Group {
    public int Id {get; set;}
    public string Name {get; set;}
    public string Flags {get; set;}
    public int Immunity {get; set;}
    public int CreatedAt {get; set;}
    public int UpdatedAt {get; set;}
    public int? DeletedAt {get; set;} = null;

    /// <summary>
    /// For getting from db
    /// </summary>
    public Group(int id, string name, string flags, int immunity, int createdAt, int updatedAt, int? deletedAt)
    {
        Id = id;
        Name = name;
        Flags = flags;
        Immunity = immunity;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        DeletedAt = deletedAt;
    }
    /// <summary>
    /// For creating new group
    /// </summary>
    public Group(string name, string flags, int immunity)
    {
        Name = name;
        Flags = flags;
        Immunity = immunity;
        CreatedAt = AdminUtils.CurrentTimestamp();
        UpdatedAt = AdminUtils.CurrentTimestamp();
    }
}

public static class AdminUtils 
{
    public delegate Admin? AdminFinder(CCSPlayerController player);
    public static AdminFinder FindAdminMethod = null!;
    public delegate Dictionary<string, string> RightsGetter();
    public static RightsGetter GetPremissions = null!;
    public delegate IAdminConfig ConfigGetter();
    public static ConfigGetter GetConfigMethod = null!;
    public delegate void DebugFunc(string message);
    public static DebugFunc Debug = null!;
    public static int CurrentTimestamp()
    {
        return (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

    public static CCSPlayerController? GetControllerBySteamId(string steamId)
    {
        return Utilities.GetPlayers().FirstOrDefault(x => x != null && x.IsValid && x.AuthorizedSteamID != null && x.Connected == PlayerConnectedState.PlayerConnected && x.AuthorizedSteamID.SteamId64.ToString() == steamId);
    }
    public static List<CCSPlayerController> GetOnlinePlayers()
    {
        return Utilities.GetPlayers().Where(x => x != null && x.IsValid && x.AuthorizedSteamID != null && x.Connected == PlayerConnectedState.PlayerConnected).ToList();
    }
    public static Admin? Admin(this CCSPlayerController player)
    {
        return FindAdminMethod(player);
    }
    public static IAdminConfig Config()
    {
        return GetConfigMethod();
    }
    public static bool HasPermissions(this CCSPlayerController player, string key)
    {
        Debug($"Checking permission: {player.PlayerName} | {key}" );
        var permissions = GetPremissions();
        if (!permissions.TryGetValue(key, out var flags))
        {
            throw new Exception("Trying to check permissions that doesn't registred (HasPermissions method)");
        }
        Debug($"Permission registred ✔ | flags: {flags}");
        if (Config().PermissionReplacement.ContainsKey(key))
        {
            Debug($"Replace permission flags from config...");
            flags = Config().PermissionReplacement[key];
            Debug($"Permission flags replacement ✔ | flags: {flags}");
        }
        if (flags == "*")
        {
            Debug($"Has Access ✔");
            return true;
        }
        Debug($"Getting admin...");
        var admin = player.Admin();
        if (admin == null) {
            Debug($"Admin is null | No Access ✖");
            return false;
        }
        if (admin.Flags.Contains(flags) || admin.Flags.Contains("z"))
        {
            Debug($"Admin has access ✔");
            return true;
        } else {
            Debug($"Admin hasn't access ✖");
            return false;
        }
    }
}

public enum MenuColors
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

public class PlayerInfo
{
    public int UserId {get; set;}
    public int Slot {get; set;}
    public string Ip {get; set;}
    public string SteamId {get; set;}
    public string PlayerName {get; set;}
    public CCSPlayerController? Controller {get {
        return AdminUtils.GetControllerBySteamId(SteamId);
    }}
    public bool IsOnline {get {
        return AdminUtils.GetControllerBySteamId(SteamId) != null;
    }}

    public PlayerInfo(CCSPlayerController player)
    {
        UserId = (int)player.UserId!;
        Slot = player.Slot;
        Ip = player.IpAddress!;
        if (player.AuthorizedSteamID == null)
        {
            SteamId = "NOTAUTH";
        } else {
            SteamId = player.AuthorizedSteamID!.SteamId64.ToString();
        }
        PlayerName = player.PlayerName;
    }
    public PlayerInfo()
    {
        UserId = 0;
        Slot = 0;
        Ip = "";
        SteamId = "";
        PlayerName = "";
    }

}