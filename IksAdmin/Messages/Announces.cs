using IksAdminApi;
using Microsoft.Extensions.Localization;

namespace IksAdmin;

public static class Announces
{
    private static IStringLocalizer Localizer = Main.AdminApi.Localizer;
    public static void BanAdded(PlayerBan ban)
    {
        var str = ban.BanIp == 0 ? Localizer["Announce.BanAdded"] : Localizer["Announce.BanAddedIp"];
        AdminUtils.PrintToServer(str.Value
            .Replace("{admin}", ban.Admin!.Name)
            .Replace("{name}", ban.NameString)
            .Replace("{reason}", ban.Reason)
            .Replace("{ip}", ban.IpString)
            .Replace("{duration}", AdminUtils.GetDurationString(ban.Duration)), tag: Localizer["Tag"]
        );
    }
}