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
    public static void Unbanned(PlayerBan ban)
    {
        AdminUtils.PrintToServer(Localizer["Announce.Unbanned"].Value
            .Replace("{admin}", ban.UnbannedByAdmin!.Name)
            .Replace("{name}", ban.NameString)
            .Replace("{reason}", ban.UnbanReason)
            .Replace("{duration}", AdminUtils.GetDurationString(ban.Duration)), tag: Localizer["Tag"]
        );
    }

    public static void GagAdded(PlayerGag gag)
    {
        AdminUtils.PrintToServer(Localizer["Announce.GagAdded"].Value
            .Replace("{admin}", gag.Admin!.Name)
            .Replace("{name}", gag.Name)
            .Replace("{reason}", gag.Reason)
            .Replace("{duration}", AdminUtils.GetDurationString(gag.Duration)), tag: Localizer["Tag"]
        );
    }
    public static void Ungagged(PlayerGag gag)
    {
        AdminUtils.PrintToServer(Localizer["Announce.Ungagged"].Value
            .Replace("{admin}", gag.UnbannedByAdmin!.Name)
            .Replace("{name}", gag.Name)
            .Replace("{reason}", gag.UnbanReason)
            .Replace("{duration}", AdminUtils.GetDurationString(gag.Duration)), tag: Localizer["Tag"]
        );
    }

    public static void MuteAdded(PlayerMute mute)
    {
        AdminUtils.PrintToServer(Localizer["Announce.MuteAdded"].Value
            .Replace("{admin}", mute.Admin!.Name)
            .Replace("{name}", mute.Name)
            .Replace("{reason}", mute.Reason)
            .Replace("{duration}", AdminUtils.GetDurationString(mute.Duration)), tag: Localizer["Tag"]
        );
    }
     public static void Unmuted(PlayerMute gag)
    {
        AdminUtils.PrintToServer(Localizer["Announce.Unmuted"].Value
            .Replace("{admin}", gag.UnbannedByAdmin!.Name)
            .Replace("{name}", gag.Name)
            .Replace("{reason}", gag.UnbanReason)
            .Replace("{duration}", AdminUtils.GetDurationString(gag.Duration)), tag: Localizer["Tag"]
        );
    }
}