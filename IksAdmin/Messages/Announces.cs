using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        .Replace("{ip}", ban.IpString)
        .Replace("{duration}", AdminUtils.GetDurationString(ban.Duration))
        );
    }
}