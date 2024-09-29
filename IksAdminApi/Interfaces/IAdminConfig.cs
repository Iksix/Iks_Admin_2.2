namespace IksAdminApi;
public interface IAdminConfig 
{
    public string ServerKey { get; set; }
    public string ServerIp { get; set; }
    public string ServerName { get; set; }
    public string RconPassword {get; set;}
    // DATABASE ===
    public string Host { get; set; } 
    public string Database { get; set; } 
    public string User { get; set; } 
    public string Password { get; set; }
    public string Port { get; set; }
    // OPTIONS ==
    public int MenuType { get; set; }
    public Dictionary<string, string> PermissionReplacement {get; set;}
    public bool DebugMode { get; set; }
    
}