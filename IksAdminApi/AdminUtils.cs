using System.Text.RegularExpressions;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities;
using Microsoft.Extensions.Localization;

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

    public static PlayerComm? GetComm(this CCSPlayerController player)
    {
        return AdminApi.Comms.FirstOrDefault(x => x.SteamId == player.AuthorizedSteamID!.SteamId64.ToString());
    }
    public static string GetDurationString( int seconds )
    {
        return $"{seconds} сек.";
    }

    public static bool HasMute(this List<PlayerComm> comms)
    {
        return comms.Any(x => x.MuteType is 0);
    }
    public static bool HasGag(this List<PlayerComm> comms)
    {
        return comms.Any(x => x.MuteType is 1);
    }
    public static bool HasSilence(this List<PlayerComm> comms)
    {
        return comms.Any(x => x.MuteType is 2);
    }
    public static PlayerComm? GetGag(this List<PlayerComm> comms)
    {
        return comms.FirstOrDefault(x => x.MuteType is 1);
    }
    public static PlayerComm? GetMute(this List<PlayerComm> comms)
    {
        return comms.FirstOrDefault(x => x.MuteType is 0);
    }
    public static PlayerComm? GetSilence(this List<PlayerComm> comms)
    {
        return comms.FirstOrDefault(x => x.MuteType is 2);
    }
    public static Admin? Admin(this CCSPlayerController? player)
    {
        if (player == null) return AdminApi.ConsoleAdmin;
        return FindAdminByControllerMethod(player);
    }
    
    public static Admin? ServerAdmin(this PlayerInfo player)
    {
        return AdminApi.ServerAdmins.FirstOrDefault(x => x.SteamId == player.SteamId);
    }
    public static Admin? Admin(int id)
    {
        return FindAdminByIdMethod(id);
    }
    public static Admin? Admin(string steamId)
    {
        if (steamId.ToLower() == "console")
            return AdminApi.ConsoleAdmin;
        return AdminApi.AllAdmins.FirstOrDefault(x => x.SteamId == steamId);
    }
    public static Admin? ServerAdmin(string steamId)
    {
        if (steamId.ToLower() == "console")
            return AdminApi.ConsoleAdmin;
        return AdminApi.ServerAdmins.FirstOrDefault(x => x.SteamId == steamId);
    }
    public static bool IsAdmin(this CCSPlayerController player)
    {
        return FindAdminByControllerMethod(player) != null;
    }
    public static IAdminConfig Config()
    {
        return GetConfigMethod();
    }
    public static string AReplace(this LocalizedString localizer, string[] keys, object[] values)
    {
        var input = localizer.ToString()!;
        for (int i = 0; i < keys.Length; i++)
        {
            input = input.Replace("{" + keys[i] + "}", values[i].ToString());
        }
        return input;
    }
    public static string AReplace(this string input, string[] keys, object[] values)
    {
        for (int i = 0; i < keys.Length; i++)
        {
            input = input.Replace("{" + keys[i] + "}", values[i].ToString());
        }
        return input;
    }
    public static void Print(this CCSPlayerController? player, string message, string? tag = null)
    {
        if (message.Trim() == "") return;
        var eventData = new EventData("print_to_player");
        eventData.Insert("player", player);
        eventData.Insert("message", message);
        eventData.Insert("tag", tag);
        if (eventData.Invoke() != HookResult.Continue)
        {
            Debug("Print(...) stopped by event PRE ");
            return;
        }

        player = eventData.Get<CCSPlayerController?>("player");
        message = eventData.Get<string>("message");
        tag = eventData.Get<string>("tag");
        
        Server.NextFrame(() =>
        {
            if (player == null)
            {
                Console.WriteLine(message);
                return;
            }
            foreach (var str in message.Split("\n"))
            {
                player.PrintToChat($" {tag ?? AdminApi.Localizer["Tag"]} {str}");
            }

            eventData.Invoke("print_to_player_post");
        });
        
    }
    public static string? GetIp(this CCSPlayerController player)
    {
        var ip = player.IpAddress;
        if (ip == null) return null;
        return ip.Split(":")[0];
    }

    public static string GetSteamId(this CCSPlayerController? player)
    {
        if (player == null) return "CONSOLE";
        if (player.IsBot)
        {
            throw new Exception("Trying to get bot steam id");
        }
        var steamId = player.AuthorizedSteamID;
        return steamId!.SteamId64.ToString();
    }

    public static bool IsConsoleId(string steamId)
    {
        return steamId.ToLower() == "console";
    }

    public static void PrintToServer(string message, string tag = "")
    {
        if (message.Trim() == "") return;
        foreach (var str in message.Split("\n"))
        {
            Server.PrintToChatAll($" {tag} {str}");
        }
    }
    public static void Reply(this CommandInfo info, string message, string? tag = null)
    {
        if (message.Trim() == "") return;
        Server.NextFrame(() =>
        {
            foreach (var str in message.Split("\n"))
            {
                info.ReplyToCommand($" {tag ?? AdminApi.Localizer["Tag"]} {str}");
            }
        });
    }
    /// <returns>Возвращает строку из текущих флагов по праву(ex: "admin_manage.add") (учитывая замену в кфг)</returns>
    public static string GetCurrentPermissionFlags(string key)
    {
        Debug("GetCurrentPermissionFlags for key: `" + key + "`");
        if (key == ">*") {
            return GetAllPermissionFlags();
        }
        var permissions = GetPremissions();
        var firstKey = key.Split(".")[0];
        var lastKey = string.Join(".", key.Split(".").Skip(1));
        if (!permissions.TryGetValue(firstKey, out var permission))
        {
            throw new Exception("Trying to get permissions group that doesn't registred (HasPermissions method) | " + key);
        }
        if (!permission.TryGetValue(lastKey, out var flags))
        {
            throw new Exception("Trying to get permissions that doesn't registred (HasPermissions method) | " + key);
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
    /// <returns>Возвращает строку из всех флагов которые используются(учитывая замену в кфг)</returns>
    public static string GetAllPermissionFlags() // ex: admin_manage
    {
        var registredPermissions = GetPremissions();
        var flags = "";
        foreach (var permission in registredPermissions)
        {
            var group = permission.Key;
            foreach (var right in permission.Value)
            {
                flags += GetCurrentPermissionFlags($"{group}.{right.Key}");
            }
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
        if (admin.IsDisabled) return false;
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
        if (admin.IsDisabled) {
            Debug($"Admin is disabled | No Access ✖");
            return false;
        }
        if (admin.CurrentFlags.Contains(flags) || admin.CurrentFlags.Contains("z") || admin.SteamId == "CONSOLE"
            || (key == ">*" && admin.CurrentFlags.ToCharArray().Any(x => flags.Contains(x)))
            )
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
