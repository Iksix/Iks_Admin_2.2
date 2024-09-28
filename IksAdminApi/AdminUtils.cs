using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace IksAdminApi;


public static class AdminUtils 
{
    public delegate Admin? AdminFinder(CCSPlayerController player);
    public static AdminFinder FindAdminMethod = null!;
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

    public static CCSPlayerController? GetControllerBySteamId(string steamId)
    {
        return Utilities.GetPlayers().FirstOrDefault(x => x != null && x.IsValid && x.AuthorizedSteamID != null && x.Connected == PlayerConnectedState.PlayerConnected && x.AuthorizedSteamID.SteamId64.ToString() == steamId);
    }
    public static List<CCSPlayerController> GetOnlinePlayers(bool includeBots = false)
    {
        if (includeBots)
            return Utilities.GetPlayers().Where(x => x != null && x.IsValid && x.Connected == PlayerConnectedState.PlayerConnected).ToList();
        return Utilities.GetPlayers().Where(x => x != null && x.IsValid && !x.IsBot && x.AuthorizedSteamID != null && x.Connected == PlayerConnectedState.PlayerConnected).ToList();
    }
    public static Admin? Admin(this CCSPlayerController player)
    {
        return FindAdminMethod(player);
    }
    public static bool IsAdmin(this CCSPlayerController player)
    {
        return FindAdminMethod(player) != null;
    }
    public static IAdminConfig Config()
    {
        return GetConfigMethod();
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
    public static bool HasPermissions(this CCSPlayerController player, string key)
    {
        Debug($"Checking permission: {player.PlayerName} | {key}" );
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
        if (admin.CurrentFlags.Contains(flags) || admin.CurrentFlags.Contains("z"))
        {
            Debug($"Admin has access ✔");
            return true;
        } else {
            Debug($"Admin hasn't access ✖");
            return false;
        }
    }
}
