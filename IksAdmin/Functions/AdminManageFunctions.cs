using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using IksAdminApi;

namespace IksAdmin.Functions;

public static class AdminManageFunctions
{
    public static void Add(CCSPlayerController? caller, CommandInfo info, string steamId, string name, int time, string? serverKey, string? groupName = null, string? flags = null, int? immunity = null, string? discord = null, string? vk = null)
    {
        Helper.Reply(info, "Adding admin...");
        Task.Run(async () => {
            var existingAdmin = await AdminsControllFunctions.GetAdmin(steamId);
            if (existingAdmin != null)
            {
                Helper.Reply(info, "Admin already exists ✖");
                return;
            }
            var admin = new Admin(
                steamId,
                name,
                flags,
                immunity,
                Main.AdminApi.Groups.FirstOrDefault(x => x.Name == groupName)?.Id,
                serverKey,
                discord,
                vk,
                endAt: time == 0 ? null : AdminUtils.CurrentTimestamp() + time
            );
            await AdminsControllFunctions.AddAdminToBase(admin);
            Helper.Reply(info, "Admin added ✔");
            await Main.AdminApi.RefreshAdmins();
        });
    }

    public static void AddFlag(CCSPlayerController? caller, CommandInfo info, Admin admin, string flags)
    {
        if (admin.Flags == null)
        {
            admin.Flags = flags;
        } else{
            admin.Flags += flags;
        }
        Helper.Reply(info, "Flags setted to admin ✔");
        Task.Run(async () => {
            await AdminsControllFunctions.UpdateAdminInBase(admin);
            await Main.AdminApi!.SendRconToAllServers("css_am_reload_admins");
        });
    }
}