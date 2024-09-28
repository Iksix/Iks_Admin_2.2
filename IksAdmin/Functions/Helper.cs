using System.Text.Json;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using IksAdminApi;

namespace IksAdmin.Functions;

public static class Helper
{
    public static void SetSortMenus()
    {
        using var streamReader = new StreamReader($"{Main.AdminApi.ModuleDirectory}/sortmenus.json");
        string json = streamReader.ReadToEnd();
        var sortMenus = JsonSerializer.Deserialize<Dictionary<string, SortMenu[]>>(json, new JsonSerializerOptions() { ReadCommentHandling = JsonCommentHandling.Skip })!;
        Main.AdminApi.SortMenus = sortMenus;
        Main.AdminApi.Debug("Sort Menus setted!");
        foreach (var item in Main.AdminApi.SortMenus)
        {
            Main.AdminApi.Debug($@"Menu key: {item.Key}");
            Main.AdminApi.Debug($@"Menu options: ");
            Main.AdminApi.Debug($@"ID | ViewFlags | View");
            foreach (var option in item.Value)
            {
                Main.AdminApi.Debug($@"{option.Id} | {option.ViewFlags} | {option.View}");
            }
        }
    }
    public static void Print(CCSPlayerController? player, string message)
    {
        if (player == null)
        {
            Console.WriteLine(message);
            return;
        }
        Server.NextFrame(() => {
            player.Print(message, Main.AdminApi.Localizer["Tag"]);
        });
    }
    public static void Reply(CommandInfo info, string message)
    {
        Server.NextFrame(() => {
            info.Reply(message, Main.AdminApi.Localizer["Tag"]);
        });
    }
}