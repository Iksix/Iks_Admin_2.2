using IksAdminApi;

namespace IksAdmin.Functions;


public static class MutesFunctions
{
    public static AdminApi AdminApi = Main.AdminApi!;

    public static async Task Mute(PlayerComm mute)
    {
        AdminApi.Debug("Add mute... " + mute.SteamId);
        var result = await AdminApi.AddMute(mute);
        AdminApi.Debug("Mute result: " + result);
        switch (result.QueryStatus)
        {
            case 0:
                Helper.PrintToSteamId(mute.Admin!.SteamId, AdminApi.Localizer["ActionSuccess.MuteSuccess"]);
                break;
            case 1:
                Helper.PrintToSteamId(mute.Admin!.SteamId, AdminApi.Localizer["ActionError.AlreadyBanned"]);
                break;
            case 2:
                Helper.PrintToSteamId(mute.Admin!.SteamId, AdminApi.Localizer["ActionError.NotEnoughPermissionsForUnban"]);
                break;
            case -1:
                Helper.PrintToSteamId(mute.Admin!.SteamId, AdminApi.Localizer["ActionError.Other"]);
                break;
        }
    }
    public static async Task Unmute(Admin admin, string steamId, string reason)
    {
        AdminApi.Debug("Trying to unmute... " + steamId);
        var existingComm = (await AdminApi.GetActiveComms(steamId)).GetMute();
        if (existingComm == null)
        {
            Helper.PrintToSteamId(admin.SteamId, AdminApi.Localizer["ActionError.PunishmentNotFound"]);
            return;
        }
        existingComm.UnbanReason = reason;
        var result = await AdminApi.UnComm(admin, existingComm);
        AdminApi.Debug("Unmute result: " + result);
        switch (result.QueryStatus)
        {
            case 0:
                Helper.PrintToSteamId(admin.SteamId, AdminApi.Localizer["ActionSuccess.UnmuteSuccess"]);
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