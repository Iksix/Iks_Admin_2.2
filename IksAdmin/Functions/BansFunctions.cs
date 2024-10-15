using IksAdminApi;

namespace IksAdmin.Functions;


public static class BansFunctions
{
    public static AdminApi AdminApi = Main.AdminApi!;
    public static async Task Ban(PlayerBan ban)
    {
        AdminApi.Debug("Add ban... " + ban.SteamId);
        var result = await AdminApi.AddBan(ban);
        AdminApi.Debug("Ban result: " + result);
        switch (result)
        {
            case 0:
                Helper.PrintToSteamId(ban.Admin!.SteamId, AdminApi.Localizer["ActionSuccess.BanSuccess"]);
                break;
            case 1:
                Helper.PrintToSteamId(ban.Admin!.SteamId, AdminApi.Localizer["ActionError.AlreadyBanned"]);
                break;
            case -1:
                Helper.PrintToSteamId(ban.Admin!.SteamId, AdminApi.Localizer["ActionError.Other"]);
                break;
        }
    }

    public static async Task Unban(Admin admin, string steamId, string reason)
    {
        AdminApi.Debug("Trying to unban... " + steamId);
        var result = await AdminApi.Unban(admin, steamId, reason);
        AdminApi.Debug("Unban result: " + result);
        switch (result)
        {
            case 0:
                Helper.PrintToSteamId(admin.SteamId, AdminApi.Localizer["ActionSuccess.UnbanSuccess"]);
                break;
            case 1:
                Helper.PrintToSteamId(admin.SteamId, AdminApi.Localizer["ActionError.PunishmentNotFound"]);
                break;
            case 2:
                Helper.PrintToSteamId(admin.SteamId, AdminApi.Localizer["ActionError.NotEnoughPermissionsForUnban"]);
                break;
            case -1:
                Helper.PrintToSteamId(admin.SteamId, AdminApi.Localizer["ActionError.Other"]);
                break;
        }
    }
    public static async Task UnbanIp(Admin admin, string ip, string reason)
    {
        AdminApi.Debug("Trying to unban ip... " + ip);
        var result = await AdminApi.Unban(admin, ip, reason);
        AdminApi.Debug("Unban ip result: " + result);
        switch (result)
        {
            case 0:
                Helper.PrintToSteamId(admin.SteamId, AdminApi.Localizer["ActionSuccess.UnbanSuccess"]);
                break;
            case 1:
                Helper.PrintToSteamId(admin.SteamId, AdminApi.Localizer["ActionError.PunishmentNotFound"]);
                break;
            case 2:
                Helper.PrintToSteamId(admin.SteamId, AdminApi.Localizer["ActionError.NotEnoughPermissionsForUnban"]);
                break;
            case -1:
                Helper.PrintToSteamId(admin.SteamId, AdminApi.Localizer["ActionError.Other"]);
                break;
        }
    }
}