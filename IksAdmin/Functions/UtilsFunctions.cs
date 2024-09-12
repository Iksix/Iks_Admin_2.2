using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CounterStrikeSharp.API.Core;
using IksAdminApi;

namespace IksAdmin.Functions;

public static class UtilsFunctions
{
    public static Admin? FindAdminMethod(CCSPlayerController player)
    {
        return Main.AdminApi.ServerAdmins.FirstOrDefault(x => x.SteamId == player.AuthorizedSteamID!.SteamId64.ToString());
    }

    public static Dictionary<string, string> GetPermissions()
    {
        return Main.AdminApi.RegistredPermissions;
    }

    public static IAdminConfig GetConfigMethod()
    {
        return Main.AdminApi.Config;
    }

    public static void SetDebugMethod(string message)
    {
        Main.AdminApi.Debug(message);
    }
}