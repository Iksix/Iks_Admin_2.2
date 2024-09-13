using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CounterStrikeSharp.API.Core;
using Dapper;
using IksAdminApi;
using MySqlConnector;

namespace IksAdmin.Functions;

public static class AdminsControllFunctions
{
    public static async Task<Admin> AddAdmin(Admin admin)
    {
        try
        {
            await using var conn = new MySqlConnection(Database.ConnectionString);
            await conn.OpenAsync();
            var existingAdmin = await GetAdmin(admin.SteamId, ignoreDeleted: false);
            if (existingAdmin != null)
            {
                Main.AdminApi.Debug($"Admin {admin.SteamId} already exists...");
                Main.AdminApi.Debug($"Set new admin {admin.SteamId} id = {existingAdmin.Id} ✔");
                admin.Id = existingAdmin.Id;
                Main.AdminApi.Debug($"Update admin in base...");
                return await UpdateAdminInBase(admin);
            }
            Main.AdminApi.Debug($"Add admin to base...");
            return await AddAdminToBase(admin);
        }
        catch (MySqlException e)
        {
            Main.AdminApi.LogError(e.ToString());
            throw;
        }
    }
    public static async Task AddServerKeyToAdmin(string steamId, string serverKey)
    {
        try
        {
            await using var conn = new MySqlConnection(Database.ConnectionString);
            await conn.OpenAsync();
            var existingAdmin = await GetAdmin(steamId);
            if (existingAdmin == null)
            {
                Main.AdminApi.LogError($"Admin {steamId} not finded ✖");
                return;
            }
            Main.AdminApi.Debug($"Admin {steamId} finded ✔");
            Main.AdminApi.Debug($"Adding server key...");
            var serverKeys = existingAdmin.ServerKeys.ToList();
            if (!serverKeys.Contains(serverKey))
            {
                serverKeys.Add(serverKey);
                Main.AdminApi.Debug($"Server key added ✔");
            } else {
                Main.AdminApi.Debug($"Server key already exists ✖");
                return;
            }
            existingAdmin.ServerKey = string.Join(";", serverKeys);
            await UpdateAdminInBase(existingAdmin);
        }
        catch (MySqlException e)
        {
            Main.AdminApi.LogError(e.ToString());
            throw;
        }
    }
    public static async Task<Admin?> GetAdmin(string steamId, string? serverKey = null, bool ignoreDeleted = true)
    {
        try
        {
            if (serverKey == null) 
            {
                serverKey = Main.AdminApi.Config.ServerKey;
            }
            await using var conn = new MySqlConnection(Database.ConnectionString);
            await conn.OpenAsync();
            var ignoreDeletedString = ignoreDeleted ? "and deleted_at is null" : "";
            var admins = (await conn.QueryAsync<Admin>($@"
                select
                id as id,
                steam_id as steamId,
                name as name,
                flags as flags,
                immunity as immunity,
                group_id as groupId,
                server_key as serverKey,
                created_at as createdAt,
                updated_at as updatedAt,
                deleted_at as deletedAt
                from iks_admins
                where steam_id = @steamId
                {ignoreDeletedString}
            ")).ToList();

            return admins.FirstOrDefault(x => x.ServerKey.Trim() == "" && x.ServerKeys.Contains(serverKey));
        }
        catch (MySqlException e)
        {
            Main.AdminApi.LogError(e.ToString());
            throw;
        }
    }

    public static async Task<List<Admin>> GetAllAdmins(string? serverKey = null, bool ignoreDeleted = true)
    {
        try
        {
            if (serverKey == null) 
            {
                serverKey = Main.AdminApi.Config.ServerKey;
            }
            await using var conn = new MySqlConnection(Database.ConnectionString);
            await conn.OpenAsync();
            var ignoreDeletedString = ignoreDeleted ? "where deleted_at is null" : "";
            var admins = (await conn.QueryAsync<Admin>($@"
                select
                id as id,
                steam_id as steamId,
                name as name,
                flags as flags,
                immunity as immunity,
                group_id as groupId,
                server_key as serverKey,
                created_at as createdAt,
                updated_at as updatedAt,
                deleted_at as deletedAt
                from iks_admins
                {ignoreDeletedString}
            ")).ToList();

            return admins;
        }
        catch (MySqlException e)
        {
            Main.AdminApi.LogError(e.ToString());
            throw;
        }
    }

    public static async Task<Admin> AddAdminToBase(Admin admin)
    {
        try
        {
            await using var conn = new MySqlConnection(Database.ConnectionString);
            await conn.OpenAsync();
            var newAdmin = await conn.QuerySingleAsync<Admin>(@"
                insert into iks_admins
                (steam_id, name, flags, immunity, group_id, server_key, created_at, updated_at)
                values
                (@steamId, @name, @flags, @immunity, @groupId, @serverKey, unix_timestamp(), unix_timestamp())
            ", new {
                steamId = admin.SteamId,
                name = admin.Name,
                flags = admin.Flags,
                immunity = admin.Immunity,
                groupId = admin.GroupId,
                serverKey = admin.ServerKey
            });
            Main.AdminApi.Debug($"Admin added to base ✔");
            return newAdmin;
        }
        catch (MySqlException e)
        {
            Main.AdminApi.LogError(e.ToString());
            throw;
        }
    }
    public static async Task<Admin> UpdateAdminInBase(Admin admin)
    {
        try
        {
            await using var conn = new MySqlConnection(Database.ConnectionString);
            await conn.OpenAsync();
            var updatedAdmin = await conn.QuerySingleAsync<Admin>(@"
                update iks_admins set 
                steam_id = @steamId,
                name = @name,
                flags = @flags,
                immunity = @immunity,
                group_id = @groupId,
                server_key = @serverKey,
                updated_at = unix_timestamp(),
                deleted_at = null
                where id = @id 
            ", new {
                id = admin.Id,
                steamId = admin.SteamId,
                name = admin.Name,
                flags = admin.Flags,
                immunity = admin.Immunity,
                groupId = admin.GroupId,
                serverKey = admin.ServerKey
            });
            Main.AdminApi.Debug($"Admin updated in base ✔");
            return updatedAdmin;
        }
        catch (MySqlException e)
        {
            Main.AdminApi.LogError(e.ToString());
            throw;
        }
    }

    public static async Task RefreshAdmins()
    {
        try
        {
            Main.AdminApi.Debug("Refresing admins...");
            var admins = await GetAllAdmins();
            Main.AdminApi.Debug("1/4 Admins getted ✔");
            Main.AdminApi.ConsoleAdmin = admins.First(x => x.SteamId.ToLower() == "console");
            Main.AdminApi.Debug("2/4 Console admin setted ✔");
            admins = admins.Where(x => x.SteamId.ToLower() != "console").ToList();
            Main.AdminApi.AllAdmins = admins;
            Main.AdminApi.Debug("3/4 All admins setted ✔");
            var serverAdmins = admins.Where(x => x.ServerKey == null || x.ServerKeys.Contains(Main.AdminApi.Config.ServerKey)).ToList();
            Main.AdminApi.ServerAdmins = serverAdmins;
            Main.AdminApi.Debug("4/4 Server admins setted ✔");
            Main.AdminApi.Debug("Admins refreshed ✔");
            Main.AdminApi.Debug("---------------");
            Main.AdminApi.Debug("Server admins:");
            foreach (var admin in serverAdmins)
            {
                Main.AdminApi.Debug($"{admin.SteamId} | {admin.Name}");
            }
        }
        catch (Exception e)
        {
            Main.AdminApi.LogError(e.ToString());
            throw;
        }
    }
}