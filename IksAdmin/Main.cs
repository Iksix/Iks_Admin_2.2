using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using MenuManager;
using IksAdminApi;
using CounterStrikeSharp.API.Modules.Menu;
using IksAdmin.Menu;
using Microsoft.Extensions.Localization;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using Microsoft.Extensions.Logging;
using CounterStrikeSharp.API;

namespace IksAdmin;

public class Main : BasePlugin, IPluginConfig<PluginConfig>
{
    public override string ModuleName => "IksAdmin";
    public override string ModuleVersion => "2.1.1";
    public override string ModuleAuthor => "iks [Discord: iks__]";

    public PluginConfig Config { get; set; } = null!;
    public static IMenuApi MenuApi = null!;
    private static readonly PluginCapability<IMenuApi?> MenuCapability = new("menu:nfcore");   
    public static AdminApi AdminApi = null!;
    private readonly PluginCapability<IIksAdminApi> _pluginCapability  = new("iksadmin:core");
    public void OnConfigParsed(PluginConfig config)
    {
        Config = config;
    }
    public override void Load(bool hotReload)
    {
        AdminApi = new AdminApi(this, Config, Localizer);
    }
    public override void OnAllPluginsLoaded(bool hotReload)
    {
        MenuApi = MenuCapability.Get()!;
    }

    [ConsoleCommand("css_menu")]
    public void OnMenuCmd(CCSPlayerController controller, CommandInfo info)
    {
        var menu = AdminApi.CreateMenu("Test menu", (MenuType)Config.MenuType);

        menu.Open(controller);
    }
}

public class AdminApi : IIksAdminApi
{
    public IAdminConfig Config { get; set; } 
    public BasePlugin Plugin { get; set; } 
    public IStringLocalizer Localizer { get; set; } 
    public AdminApi(BasePlugin plugin, IAdminConfig config, IStringLocalizer localizer)
    {
        Plugin = plugin;
        Config = config;
        Localizer = localizer;
    }
    public void CloseMenu(CCSPlayerController player)
    {
        throw new NotImplementedException();
    }
    public IDynamicMenu CreateMenu(string title, MenuType type = MenuType.ButtonMenu, PostSelectAction postSelectAction = PostSelectAction.Nothing, Action<CCSPlayerController>? backAction = null)
    {
        return new DynamicMenu(title, type, postSelectAction, backAction);
    }

    public void Debug(string message)
    {
        if (!Config.DebugMode) return;
        Server.NextFrame(() => {
            Plugin.Logger.LogDebug(message);
        });
    }
}