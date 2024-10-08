using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Commands;
using IksAdminApi;

namespace IksAdmin.Functions;


public static class BlocksFunctions
{
    public static AdminApi AdminApi = Main.AdminApi!;
    public static async Task Ban(PlayerBan ban, CommandInfo info)
    {
        AdminApi.Debug("Add ban... " + ban.SteamId);
        var result = await AdminApi.AddBan(ban);
        AdminApi.Debug("Ban result: " + result);
        switch (result)
        {
            case 0:
                Server.NextFrame(() => {
                    Helper.Reply(info, AdminApi.Localizer["ActionSuccess.BanSuccess"]);
                });
                break;
            case 1:
                Server.NextFrame(() => {
                    Helper.Reply(info, AdminApi.Localizer["ActionError.AlreadyBanned"]);
                });
                break;
        }
        
    }
}