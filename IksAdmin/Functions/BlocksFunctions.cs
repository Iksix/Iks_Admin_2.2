using CounterStrikeSharp.API.Modules.Commands;
using IksAdminApi;

namespace IksAdmin.Functions;


public static class BlocksFunctions
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
                Helper.PrintToSteamId(ban.Admin!.SteamId, AdminApi.Localizer["ActionError.AlreadyBanned"]);
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
                Helper.PrintToSteamId(admin.SteamId, AdminApi.Localizer["ActionSuccess.BanSuccess"]);
                break;
            case 1:
                Helper.PrintToSteamId(admin.SteamId, AdminApi.Localizer["ActionError.AlreadyBanned"]);
                break;
            case 2:
                Helper.PrintToSteamId(admin.SteamId, AdminApi.Localizer["ActionError.AlreadyBanned"]);
                break;
            case -1:
                Helper.PrintToSteamId(admin.SteamId, AdminApi.Localizer["ActionError.AlreadyBanned"]);
                break;
        }
    }
}