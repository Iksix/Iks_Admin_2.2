using CounterStrikeSharp.API.Core;
using IksAdminApi;
namespace IksAdmin;
public class PluginConfig : BasePluginConfig, IAdminConfig
{
    public string ServerKey { get; set; } = "1";
    public string ServerIp {get; set;} = "0.0.0.0:27015"; // Указываете IP сервера
    public string ServerName {get; set;} = "Server name"; // Название сервера, если пусто то АВТО
    public string RconPassword {get; set;} = "12345";
    // DATABASE ===
    public string Host { get; set; } = "host";
    public string Database { get; set; } = "Database";
    public string User { get; set; } = "User";
    public string Password { get; set; } = "Password";
    public string Port { get; set; } = "3306";
    // ===
    public string WebApiKey {get; set;} = ""; // Указываете API ключ для получения имени в оффлайн бане
    public bool AdvancedKick {get; set;} = true;
    public int AdvancedKickTime {get; set;} = 5;
    public bool DebugMode { get; set; } = true;
    public int MenuType { get; set; } = 2; // -1 = Default(Player select) [MM] | 0 = ChatMenu | 1 = ConsoleMenu | 2 = HtmlMenu | 3 = ButtonMenu [MM]
    public Dictionary<string, string> PermissionReplacement { get; set; } = new Dictionary<string, string>()
    {
        {"admins_manage_add", "z"} // Пример замены права управления админами на флаг z (Ну он и так z по дефолту, ну так что бы знали)
    };
    public string[] IgnoreCommandsRegistering {get; set;} = ["example_command"]; // Эти команды не будут инициализированны при добавлении через метод админки (пишем без префикса css_)
    public string[] MirrorsIp {get; set;} = ["0.0.0.0"]; // Эти айпи не возможно будет добавить в наказания (будет null) (рассчитано что тут будут айпи зеркал)
    
}