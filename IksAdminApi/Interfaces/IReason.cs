using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IksAdminApi;

public interface IReason
{
    public string Title {get; set;}
    public string Text {get; set;}
    public int MinTime {get; set;}
    public int MaxTime {get; set;}
    public int? Duration {get; set;}
    public bool BanOnAllServers {get; set;}
}