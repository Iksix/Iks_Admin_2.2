using IksAdminApi;
using Microsoft.Extensions.Localization;

namespace IksAdmin;

public static class MsgAnnounces
{
    private static AdminApi _api = Main.AdminApi;
    private static IStringLocalizer _localizer = _api.Localizer;
    public static void BanAdded(PlayerBan ban)
    {
        var str = ban.BanType == 0 ? _localizer["Announce.BanAdded"] : _localizer["Announce.BanAddedIp"];
        AdminUtils.PrintToServer(str.Value
            .Replace("{admin}", ban.Admin!.Name)
            .Replace("{name}", ban.NameString)
            .Replace("{reason}", ban.Reason)
            .Replace("{ip}", ban.IpString)
            .Replace("{duration}", AdminUtils.GetDurationString(ban.Duration)), tag: _localizer["Tag"]
        );
    }
    public static void Unbanned(PlayerBan ban)
    {
        AdminUtils.PrintToServer(_localizer["Announce.Unbanned"].Value
            .Replace("{admin}", ban.UnbannedByAdmin!.Name)
            .Replace("{name}", ban.NameString)
            .Replace("{reason}", ban.UnbanReason)
            .Replace("{duration}", AdminUtils.GetDurationString(ban.Duration)), tag: _localizer["Tag"]
        );
    }

    public static void GagAdded(PlayerComm gag)
    {
        AdminUtils.PrintToServer(_localizer["Announce.GagAdded"].Value
            .Replace("{admin}", gag.Admin!.Name)
            .Replace("{name}", gag.Name)
            .Replace("{reason}", gag.Reason)
            .Replace("{duration}", AdminUtils.GetDurationString(gag.Duration)), tag: _localizer["Tag"]
        );
    }
    public static void UnGagged(PlayerComm gag)
    {
        AdminUtils.PrintToServer(_localizer["Announce.UnGagged"].Value
            .Replace("{admin}", gag.UnbannedByAdmin!.Name)
            .Replace("{name}", gag.Name)
            .Replace("{reason}", gag.UnbanReason)
            .Replace("{duration}", AdminUtils.GetDurationString(gag.Duration)), tag: _localizer["Tag"]
        );
    }

    public static void MuteAdded(PlayerComm mute)
    {
        AdminUtils.PrintToServer(_localizer["Announce.MuteAdded"].Value
            .Replace("{admin}", mute.Admin!.Name)
            .Replace("{name}", mute.Name)
            .Replace("{reason}", mute.Reason)
            .Replace("{duration}", AdminUtils.GetDurationString(mute.Duration)), tag: _localizer["Tag"]
        );
    }
     public static void UnMuted(PlayerComm gag)
    {
        AdminUtils.PrintToServer(_localizer["Announce.UnMuted"].Value
            .Replace("{admin}", gag.UnbannedByAdmin!.Name)
            .Replace("{name}", gag.Name)
            .Replace("{reason}", gag.UnbanReason)
            .Replace("{duration}", AdminUtils.GetDurationString(gag.Duration)), tag: _localizer["Tag"]
        );
    }
    public static void SilenceAdded(PlayerComm comm)
    {
        AdminUtils.PrintToServer(_localizer["Announce.SilenceAdded"].Value
                .Replace("{admin}", comm.Admin!.Name)
                .Replace("{name}", comm.Name)
                .Replace("{reason}", comm.Reason)
                .Replace("{duration}", AdminUtils.GetDurationString(comm.Duration)), tag: _localizer["Tag"]
        );
    }
    public static void UnSilenced(PlayerComm comm)
    {
        AdminUtils.PrintToServer(_localizer["Announce.UnSilenced"].Value
                .Replace("{admin}", comm.UnbannedByAdmin!.Name)
                .Replace("{name}", comm.Name)
                .Replace("{reason}", comm.UnbanReason)
                .Replace("{duration}", AdminUtils.GetDurationString(comm.Duration)), tag: _localizer["Tag"]
        );
    }

    public static void Warn(Warn warn)
    {
        AdminUtils.PrintToServer(_localizer["Announce.Warn"].Value
                .Replace("{admin}", AdminUtils.Admin(warn.AdminId)!.Name)
                .Replace("{name}", AdminUtils.Admin(warn.TargetId)!.Name)
                .Replace("{reason}", warn.Reason)
                .Replace("{now}", AdminUtils.Admin(warn.TargetId)!.Warns.Count.ToString())
                .Replace("{max}", _api.Config.MaxWarns.ToString())
                .Replace("{duration}", (AdminUtils.GetDurationString(warn.Duration)).ToString()), tag: _localizer["Tag"]
        );
    }
}