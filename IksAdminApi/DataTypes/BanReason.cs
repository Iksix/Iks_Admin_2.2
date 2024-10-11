using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace IksAdminApi;

public class BanReason
{
    public string Title {get; set;} // Причина отображаемая в меню
    public string Text {get; set;} // Причина отображаемая при бане
    public int MinTime {get; set;} = 0;
    public int MaxTime {get; set;} = 0;
    public int? Duration {get; set;} = null; // Если null то админ выбирает время
    public bool BanOnAllServers {get; set;} = false; // Банить ли по этой причине на всех серверах
    public BanReason(string title, string? text = null, int minTime = 0, int maxTime = 0, int? duration = null, bool banOnAllServers = false)
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