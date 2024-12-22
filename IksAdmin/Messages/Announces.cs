using IksAdminApi;
using Microsoft.Extensions.Localization;

namespace IksAdmin;

public static class Announces
{
    private static IStringLocalizer Localizer = Main.AdminApi.Localizer;
    public static void BanAdded(PlayerBan ban)
    {
        var str = ban.BanType == 0 ? Localizer["Announce.BanAdded"] : Localizer["Announce.BanAddedIp"];
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

    public static void GagAdded(PlayerComm gag)
    {
        AdminUtils.PrintToServer(Localizer["Announce.GagAdded"].Value
            .Replace("{admin}", gag.Admin!.Name)
            .Replace("{name}", gag.Name)
            .Replace("{reason}", gag.Reason)
            .Replace("{duration}", AdminUtils.GetDurationString(gag.Duration)), tag: Localizer["Tag"]
        );
    }
    public static void UnGagged(PlayerComm gag)
    {
        AdminUtils.PrintToServer(Localizer["Announce.UnGagged"].Value
            .Replace("{admin}", gag.UnbannedByAdmin!.Name)
            .Replace("{name}", gag.Name)
            .Replace("{reason}", gag.UnbanReason)
            .Replace("{duration}", AdminUtils.GetDurationString(gag.Duration)), tag: Localizer["Tag"]
        );
    }

    public static void MuteAdded(PlayerComm mute)
    {
        AdminUtils.PrintToServer(Localizer["Announce.MuteAdded"].Value
            .Replace("{admin}", mute.Admin!.Name)
            .Replace("{name}", mute.Name)
            .Replace("{reason}", mute.Reason)
            .Replace("{duration}", AdminUtils.GetDurationString(mute.Duration)), tag: Localizer["Tag"]
        );
    }
     public static void UnMuted(PlayerComm gag)
    {
        AdminUtils.PrintToServer(Localizer["Announce.UnMuted"].Value
            .Replace("{admin}", gag.UnbannedByAdmin!.Name)
            .Replace("{name}", gag.Name)
            .Replace("{reason}", gag.UnbanReason)
            .Replace("{duration}", AdminUtils.GetDurationString(gag.Duration)), tag: Localizer["Tag"]
        );
    }
    public static void SilenceAdded(PlayerComm comm)
    {
        AdminUtils.PrintToServer(Localizer["Announce.SilenceAdded"].Value
                .Replace("{admin}", comm.Admin!.Name)
                .Replace("{name}", comm.Name)
                .Replace("{reason}", comm.Reason)
                .Replace("{duration}", AdminUtils.GetDurationString(comm.Duration)), tag: Localizer["Tag"]
        );
    }
    public static void UnSilenced(PlayerComm comm)
    {
        AdminUtils.PrintToServer(Localizer["Announce.UnSilenced"].Value
                .Replace("{admin}", comm.UnbannedByAdmin!.Name)
                .Replace("{name}", comm.Name)
                .Replace("{reason}", comm.UnbanReason)
                .Replace("{duration}", AdminUtils.GetDurationString(comm.Duration)), tag: Localizer["Tag"]
        );
    }
}