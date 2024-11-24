namespace IksAdminApi;
public interface IAdminConfig 
{
    public int ServerId { get; set; }
    public string ServerIp { get; set; } // ip:port
    public string ServerName { get; set; }
    public string RconPassword {get; set;}
    // DATABASE ===
    public string Host { get; set; } 
    public string Database { get; set; } 
    public string User { get; set; } 
    public string Password { get; set; }
    public string Port { get; set; }
    // OPTIONS ==
    public string WebApiKey {get; set;}
    public bool AdvancedKick {get; set;}
    public int AdvancedKickTime {get; set;}
    public int MenuType { get; set; }
    public Dictionary<string, string> PermissionReplacement {get; set;}
    public bool DebugMode { get; set; }
    public string[] IgnoreCommandsRegistering {get; set;}
    public string[] MirrorsIp {get; set;}

    public int LastPunishmentTime {get; set;}
}