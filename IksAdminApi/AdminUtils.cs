using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace IksAdminApi;


public static class AdminUtils 
{
    public delegate Admin? AdminFinder(CCSPlayerController player);
    public static AdminFinder FindAdminMethod = null!;
    public delegate Dictionary<string, string> RightsGetter();
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
        if (admin.CurrentFlags.Contains(flags) || admin.CurrentFlags.Contains("z"))
        {
            Debug($"Admin has access ✔");
            return true;
        } else {
            Debug($"Admin hasn't access ✖");
            return false;
        }
    }
    public static bool HasPermissions(this Admin admin, string key)
    {
        Debug($"Checking permission: {admin.Name} | {key}" );
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
