namespace IksAdminApi;

public class MuteReason : Reason
{
    public MuteReason(string title, string? text = null, int minTime = 0, int maxTime = 0, int? duration = null, bool banOnAllServers = false)
    {
        Title = title;
        if (text == null)
            Text = title;
        else Text = text;
        MinTime = minTime;
        MaxTime = maxTime;
        Duration = duration;
        BanOnAllServers = banOnAllServers;
    }
}