using CounterStrikeSharp.API.Core;

namespace IksAdminApi.DataTypes;

public class PlayerInfo
{
    public int UserId {get; set;}
    public int Slot {get; set;}
    public string Ip {get; set;}
    public string? SteamId {get; set;}
    public string PlayerName {get; set;}
    public CCSPlayerController? Controller {get {
        if (SteamId == null) return null;
        return AdminUtils.GetControllerBySteamId(SteamId);
    }}
    public bool IsOnline {get {
        if (SteamId == null) return false;
        return AdminUtils.GetControllerBySteamId(SteamId) != null;
    }}

    public PlayerInfo(CCSPlayerController player)
    {
        UserId = (int)player.UserId!;
        Slot = player.Slot;
        Ip = player.IpAddress!;
        if (player.AuthorizedSteamID == null)
        {
            SteamId = null;
        } else {
            SteamId = player.AuthorizedSteamID!.SteamId64.ToString();
        }
        PlayerName = player.PlayerName;
    }
    public PlayerInfo()
    {
        UserId = 0;
        Slot = 0;
        Ip = "";
        SteamId = "";
        PlayerName = "";
    }

}