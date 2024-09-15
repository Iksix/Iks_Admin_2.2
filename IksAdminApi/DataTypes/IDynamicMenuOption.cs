using CounterStrikeSharp.API.Core;
namespace IksAdminApi.DataTypes;

public interface IDynamicMenuOption
{
    public string Id {get; set;}
    public string Title {get; set;}
    public MenuColors Color {get; set;}
    public string ViewFlags {get; set;}
    public Action<CCSPlayerController, IDynamicMenuOption> OnExecute {get; set;}
}

