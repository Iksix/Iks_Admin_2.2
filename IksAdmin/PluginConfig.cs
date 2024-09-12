using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CounterStrikeSharp.API.Core;
using IksAdminApi;
namespace IksAdmin;
public class PluginConfig : BasePluginConfig, IAdminConfig
{
    public bool DebugMode { get; set; } = true;
    public int MenuType { get; set; } = 3; // -1 = Default(Player select) | 0 = ChatMenu | 1 = ConsoleMenu | 2 = HtmlMenu | 3 = ButtonMenu
    public Dictionary<string, string> PermissionReplacement { get; set; } = new Dictionary<string, string>()
    {
        {"core.admin_control", "z"} // Пример замены права управления админами на флаг z (Ну он и так z по дефолту, ну так что бы знали)
    };
}