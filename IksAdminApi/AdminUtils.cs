using System.Text.RegularExpressions;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;

namespace IksAdminApi;


public static class AdminUtils 
{
    public delegate Admin? AdminFinderByController(CCSPlayerController player);
    public static AdminFinderByController FindAdminByControllerMethod = null!;
    public delegate Admin? AdminFinderById(int id);
    public static AdminFinderById FindAdminByIdMethod = null!;
    public static IIksAdminApi AdminApi = null!;
    public delegate Dictionary<string, Dictionary<string, string>> RightsGetter();
    public static RightsGetter GetPremissions = null!;
    public delegate IAdminConfig ConfigGetter();
    public static ConfigGetter GetConfigMethod = null!;
    public delegate Group? GetGroupFromIdMethod(int id);
    public static GetGroupFromIdMethod GetGroupFromIdFunc = null!;
    public delegate void DebugFunc(string message);
    public static DebugFunc Debug = null!;
    public static Group? GetGroup(int? id)
    {
        if (id == null) return null;
        return GetGroupFromIdFunc((int)id);
    }
    public static int CurrentTimestamp()
    {
        return (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

    public static DateTime UnixTimeStampToDateTime( int unixTimeStamp )
    {
        // Unix timestamp is seconds past epoch
        DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        dateTime = dateTime.AddSeconds( unixTimeStamp ).ToLocalTime();
        return dateTime;
    }

    public static string GetDateString( int unixTimeStamp )
    {
        var dateTime = UnixTimeStampToDateTime( unixTimeStamp );
        return dateTime.ToString( "dd.MM.yyyy HH:mm:ss" );
    }

    public static string GetDurationString( int seconds )
    {
        return $"{seconds} сек.";
    }

    public static CCSPlayerController? GetControllerBySteamId(string steamId)
    {
        return Utilities.GetPlayers().FirstOrDefault(x => x != null && x.IsValid && x.AuthorizedSteamID != null && x.Connected == PlayerConnectedState.PlayerConnected && x.AuthorizedSteamID.SteamId64.ToString() == steamId);
    }
    public static CCSPlayerController? GetControllerByUid(uint userId)
    {
        return Utilities.GetPlayers().FirstOrDefault(x => x != null && x.IsValid && x.Connected == PlayerConnectedState.PlayerConnected && x.UserId == userId);
    }
    public static CCSPlayerController? GetControllerByName(string name, bool ignoreRegistry = false)
    {
        return Utilities.GetPlayers().FirstOrDefault(x => x != null && x.IsValid && x.Connected == PlayerConnectedState.PlayerConnected && (ignoreRegistry ? x.PlayerName.ToLower().Contains(name) : x.PlayerName.Contains(name)));
    }
    public static CCSPlayerController? GetControllerByIp(string ip)
    {
        return Utilities.GetPlayers().FirstOrDefault(x => x != null && x.IsValid && x.AuthorizedSteamID != null && x.Connected == PlayerConnectedState.PlayerConnected && x.IpAddress == ip);
    }
    public static List<CCSPlayerController> GetOnlinePlayers(bool includeBots = false)
    {
        if (includeBots)
            return Utilities.GetPlayers().Where(x => x != null && x.IsValid && x.Connected == PlayerConnectedState.PlayerConnected).ToList();
        return Utilities.GetPlayers().Where(x => x != null && x.IsValid && !x.IsBot && x.AuthorizedSteamID != null && x.Connected == PlayerConnectedState.PlayerConnected).ToList();
    }
    public static Admin? Admin(this CCSPlayerController player)
    {
        return FindAdminByControllerMethod(player);
    }
    public static Admin? Admin(this PlayerInfo player)
    {
        return AdminApi.ServerAdmins.FirstOrDefault(x => x.SteamId == player.SteamId);
    }
    public static Admin? Admin(int id)
    {
        return FindAdminByIdMethod(id);
    }
    public static bool IsAdmin(this CCSPlayerController player)
    {
        return FindAdminByControllerMethod(player) != null;
    }
    public static IAdminConfig Config()
    {
        return GetConfigMethod();
    }
    public static void Print(this CCSPlayerController? player, string message, string tag = "")
    {
        if (message.Trim() == "") return;
        foreach (var str in message.Split("\n"))
        {
            if (player != null)
                player.PrintToChat($" {tag} {str}");
            else Console.WriteLine($" {tag} {str}");
        }
    }
    public static void PrintToServer(string message, string tag = "")
    {
        if (message.Trim() == "") return;
        foreach (var str in message.Split("\n"))
        {
            Server.PrintToChatAll($" {tag} {str}");
        }
    }
    public static void Reply(this CommandInfo info, string message, string tag = "")
    {
        if (message.Trim() == "") return;
        foreach (var str in message.Split("\n"))
        {
            info.ReplyToCommand($" {tag} {str}");
        }
    }
    /// <returns>Возвращает строку из текущих флагов по праву(ex: "admin_manage.add") (учитывая замену в кфг)</returns>
    public static string GetCurrentPermissionFlags(string key)
    {
        var permissions = GetPremissions();
        var firstKey = key.Split(".")[0];
        var lastKey = string.Join(".", key.Split(".").Skip(1));
        if (!permissions.TryGetValue(firstKey, out var permission))
        {
            throw new Exception("Trying to get permissions group that doesn't registred (HasPermissions method)");
        }
        if (!permission.TryGetValue(lastKey, out var flags))
        {
            throw new Exception("Trying to get permissions that doesn't registred (HasPermissions method)");
        }
        if (Config().PermissionReplacement.ContainsKey(key))
        {
            Debug($"Replace permission flags from config...");
            flags = Config().PermissionReplacement[key];
            Debug($"Permission flags replacement ✔ | flags: {flags}");
        }
        return flags;
    }
    /// <returns>Возвращает строку из всех флагов которые используются в группе прав (учитывая замену в кфг)</returns>
    public static string GetAllPermissionGroupFlags(string key) // ex: admin_manage
    {
        var registredPermissions = GetPremissions();
        if (!registredPermissions.TryGetValue(key, out var permissions))
        {
            throw new Exception("Trying to get permissions group that doesn't registred (HasPermissions method)");
        }
        var flags = "";
        foreach (var permission in permissions)
        {
            flags += GetCurrentPermissionFlags($"{key}.{permission.Key}");
        }
        return flags;
    }
    /// <summary>
    /// Проверяет есть ли у админа доступ к любому из прав группы(ex: "admin_manage")
    /// </summary>
    public static bool HasAnyGroupPermission(this CCSPlayerController player, string key)
    {
        return HasAnyGroupPermission(player.Admin(), key);
    }
    public static bool HasAnyGroupPermission(this Admin? admin, string key)
    {
        var allGroupFlags = GetAllPermissionGroupFlags(key);
        if (allGroupFlags.Contains("*")) return true;
        if (admin == null) return false;
        if (admin.CurrentFlags.ToCharArray().Any(allGroupFlags.Contains)) return true;
        return false;
    }
    public static bool HasPermissions(this CCSPlayerController? player, string key)
    {
        if (player == null)
        {
            return true;
        }
        var admin = player.Admin();
        return HasPermissions(admin, key);
    }
    public static bool HasPermissions(this Admin? admin, string key)
    {
        if (admin != null)
            Debug($"Checking permission: {admin.Name} | {key}" );
        else Debug($"Checking permission: {key}" );
        var flags = GetCurrentPermissionFlags(key);
        if (flags == "*")
        {
            Debug($"Has Access ✔");
            return true;
        }
        if (admin == null) {
            Debug($"Admin is null | No Access ✖");
            return false;
        }
        if (admin.CurrentFlags.Contains(flags) || admin.CurrentFlags.Contains("z") || admin.SteamId == "CONSOLE")
        {
            Debug($"Admin has access ✔");
            return true;
        } else {
            Debug($"Admin hasn't access ✖");
            return false;
        }
    }
    public static List<string> GetArgsFromCommandLine(string commandLine)
    {
        List<string> args = new List<string>();
        var regex = new Regex(@"(""((\\"")|([^""]))*"")|('((\\')|([^']))*')|(\S+)");
        var matches = regex.Matches(commandLine);
        foreach (Match match in matches)
        {
            var arg = match.Value;
            if (arg.StartsWith('"'))
            {
                arg = arg.Remove(0, 1);
                arg = arg.TrimEnd('"');
            }
            args.Add(arg);
        }
        args.RemoveAt(0);
        return args;
    }
}
