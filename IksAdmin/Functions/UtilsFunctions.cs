using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CounterStrikeSharp.API.Core;
using IksAdminApi;
using IksAdminApi.DataTypes;

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

    public static string GetCurrentFlagsFunc(Admin admin)
    {
        if (admin.GroupId == null)
            return admin.Flags ?? "";
        var group = Main.AdminApi.Groups.FirstOrDefault(x => x.Id == admin.GroupId);
        if (group == null) {
            return admin.Flags ?? "";
        } else return group.Flags;
    }
    public static int GetCurrentImmunityFunc(Admin admin)
    {
        if (admin.GroupId == null)
            return admin.Immunity ?? 0;
        var group = Main.AdminApi.Groups.FirstOrDefault(x => x.Id == admin.GroupId);
        if (group == null) {
            return admin.Immunity ?? 0;
        } else return group.Immunity;
    }

    public static Group? GetGroupFromIdFunc(int id)
    {
        return Main.AdminApi.Groups.FirstOrDefault(x => x.Id == id);
    }
}