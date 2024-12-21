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
    public static void Add(CCSPlayerController? caller, CommandInfo info, string steamId, string name, int time, int? serverId, string? groupName = null, string? flags = null, int? immunity = null, string? discord = null, string? vk = null)
    {
        int? groupId = null;
        if (groupName != null) {
            var group = Main.AdminApi.Groups.FirstOrDefault(x => x.Name == groupName);
            if (group == null) {
                Helper.Reply(info, "Group not founded ✖");
                return;
            }
            groupId = group!.Id;
        }
        Helper.Reply(info, "Adding admin...");
        Task.Run(async () => {
            var admin = new Admin(
                steamId,
                name,
                flags,
                immunity,
                groupId,
                discord,
                vk,
                endAt: time == 0 ? null : AdminUtils.CurrentTimestamp() + time
            );
            var existingAdmin = await DBAdmins.GetAdmin(steamId, serverId, ignoreDeleted: false);
            if (existingAdmin != null)
            {
                if (existingAdmin.DeletedAt != null) {
                    Helper.Reply(info, $"Finded DELETED admin with id: {admin.Id} | Admin will be updated with new stats ✔");
                }
                admin.Id = existingAdmin.Id;
                await DBAdmins.UpdateAdminInBase(admin);
                
                return;
            }
            
            var newAdmin = await DBAdmins.AddAdminToBase(admin);
            Helper.Reply(info, "Admin added ✔");
            
            try
            {
                if (serverId != null)
                {
                    await DBAdmins.AddServerIdToAdmin(newAdmin.Id, (int)serverId);
                } else {
                    await DBAdmins.AddServerIdToAdmin(newAdmin.Id, Main.AdminApi.ThisServer.Id);
                }
                await Main.AdminApi.RefreshAdmins();
            }
            catch (System.Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            
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
            await DBAdmins.UpdateAdminInBase(admin);
            await Main.AdminApi!.SendRconToAllServers("css_am_reload_admins");
        });
    }

    public static void AddServerId(CCSPlayerController? caller, CommandInfo info, Admin admin, int? serverId)
    {
        Task.Run(async () => {
            if (serverId != null)
            {
                await DBAdmins.AddServerIdToAdmin(admin.Id, (int)serverId);
            } else {
                await DBAdmins.AddServerIdToAdmin(admin.Id, Main.AdminApi.ThisServer.Id);
            }
            Server.NextFrame(() => {
                Helper.Reply(info, "Server Id added to admin ✔");
            });
        });
    }
}