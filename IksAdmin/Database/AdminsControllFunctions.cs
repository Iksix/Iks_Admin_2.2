using Dapper;
using IksAdminApi;
using MySqlConnector;

namespace IksAdmin;

public static class AdminsControllFunctions
{
    private static string AdminSelect = @"
        select
        id as id,
        steam_id as steamId,
        name as name,
        flags as flags,
        immunity as immunity,
        group_id as groupId,
        discord as discord,
        vk as vk,
        is_disabled as isDisabled,
        end_at as endAt,
        created_at as createdAt,
        updated_at as updatedAt,
        deleted_at as deletedAt
        from iks_admins
    ";

    public static async Task SetAdminsToServer()
    {
        try
        {
            await using var conn = new MySqlConnection(Database.ConnectionString);
            await conn.OpenAsync();
            var adminsToServer = (await conn.QueryAsync<AdminToServer>(@"
            select
            admin_id as adminId,
            server_id as serverId
            from iks_admin_to_server
            ")).ToList();
            Main.AdminApi.AdminsToServer = adminsToServer;
        }
        catch (MySqlException e)
        {
            Main.AdminApi.LogError(e.ToString());
            throw;
        }
    }
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
    public static async Task AddServerIdToAdmin(int adminId, int serverId)
    {
        try
        {
            await using var conn = new MySqlConnection(Database.ConnectionString);
            await conn.OpenAsync();
            var existingAdmin = await GetAdminById(adminId);
            if (existingAdmin == null)
            {
                Main.AdminApi.LogError($"Admin {adminId} not finded ✖");
                return;
            }
            Main.AdminApi.Debug($"Admin {existingAdmin.Name} finded ✔");
            Main.AdminApi.Debug($"Adding server id...");
            if (Main.AdminApi.AdminsToServer.Any(x => x.AdminId == adminId && x.ServerId == serverId))
            {
                Main.AdminApi.LogError($"Server ID already added");
                return;
            }
            await conn.QueryAsync(@"
            insert into iks_admin_to_server(admin_id, server_id)
            values
            (@adminId, @serverId)
            ", new {adminId, serverId});
        }
        catch (MySqlException e)
        {
            Main.AdminApi.LogError(e.ToString());
            throw;
        }
    }
    public static async Task<Admin?> GetAdmin(string steamId, int? serverId = null, bool ignoreDeleted = true)
    {
        try
        {
            if (serverId == null) 
            {
                serverId = Main.AdminApi.ThisServer.Id;
            }
            await using var conn = new MySqlConnection(Database.ConnectionString);
            await conn.OpenAsync();
            var ignoreDeletedString = ignoreDeleted ? "and deleted_at is null" : "";
            var admins = (await conn.QueryAsync<Admin>($@"
                {AdminSelect}
                where steam_id = @steamId
                {ignoreDeletedString}
            ", new { steamId })).ToList();

            return admins.FirstOrDefault(x => x.Servers.Contains((int)serverId));
        }
        catch (MySqlException e)
        {
            Main.AdminApi.LogError(e.ToString());
            throw;
        }
    }
    public static async Task<Admin?> GetAdminById(int id, int? serverId = null, bool ignoreDeleted = true)
    {
        try
        {
            if (serverId == null) 
            {
                serverId = Main.AdminApi.ThisServer.Id;
            }
            await using var conn = new MySqlConnection(Database.ConnectionString);
            await conn.OpenAsync();
            var ignoreDeletedString = ignoreDeleted ? "and deleted_at is null" : "";
            var admins = (await conn.QueryAsync<Admin>($@"
                {AdminSelect}
                where id = @id
                {ignoreDeletedString}
            ", new { id })).ToList();

            return admins.FirstOrDefault(x => x.Servers.Contains((int)serverId));
        }
        catch (MySqlException e)
        {
            Main.AdminApi.LogError(e.ToString());
            throw;
        }
    }

    public static async Task<List<Admin>> GetAllAdmins(int? serverKey = null, bool ignoreDeleted = true)
    {
        try
        {
            if (serverKey == null) 
            {
                serverKey = Main.AdminApi.Config.ServerId;
            }
            await using var conn = new MySqlConnection(Database.ConnectionString);
            await conn.OpenAsync();
            var ignoreDeletedString = ignoreDeleted ? "where deleted_at is null" : "";
            var admins = (await conn.QueryAsync<Admin>($@"
                {AdminSelect}
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
            await conn.QueryAsync(@"
                insert into iks_admins
                (steam_id, name, flags, immunity, group_id, server_key, discord, vk, end_at, created_at, updated_at)
                values
                (@steamId, @name, @flags, @immunity, @groupId, @serverKey, @discord, @vk, @endAt, unix_timestamp(), unix_timestamp());
            ", new {
                steamId = admin.SteamId,
                name = admin.Name,
                flags = admin.Flags,
                immunity = admin.Immunity,
                groupId = admin.GroupId,
                discord = admin.Discord,
                vk = admin.Vk,
                endAt = admin.EndAt
            });
            Main.AdminApi.Debug($"Admin added to base ✔");
            var newAdmin = await GetAdmin(admin.SteamId);
            return newAdmin!;
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
            await conn.QueryAsync(@"
                update iks_admins set 
                steam_id = @steamId,
                name = @name,
                flags = @flags,
                immunity = @immunity,
                group_id = @groupId,
                discord = @discord,
                vk = @vk,
                is_disabled = @disabled,
                end_at = @endAt,
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
                disabled = admin.Disabled,
                discord = admin.Discord,
                vk = admin.Vk,
                endAt = admin.EndAt
            });
            Main.AdminApi.Debug($"Admin updated in base ✔");
            var updatedAdmin = await GetAdmin(admin.SteamId);
            return updatedAdmin!;
        }
        catch (MySqlException e)
        {
            Main.AdminApi.LogError(e.ToString());
            throw;
        }
    }

    public static async Task DeleteAdmin(int id)
    {
        try
        {
            await using var conn = new MySqlConnection(Database.ConnectionString);
            await conn.OpenAsync();
            await conn.QueryAsync(@"
                update iks_admins set 
                deleted_at = current_timestamp()
                where id = @id
            ", new {
                id
            });
            Main.AdminApi.Debug($"Group deleted ✔");
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
            await GroupsControllFunctions.RefreshGroups();
            Main.AdminApi.Debug("Refreshing admins to server...");
            await SetAdminsToServer();
            Main.AdminApi.Debug("1/5 Admin to server setted ✔");
            Main.AdminApi.Debug("Refreshing admins...");
            var admins = await GetAllAdmins();
            Main.AdminApi.Debug("2/5 Admins getted ✔");
            Main.AdminApi.ConsoleAdmin = admins.First(x => x.SteamId.ToLower() == "console");
            Main.AdminApi.Debug("3/5 Console admin setted ✔");
            admins = admins.Where(x => x.SteamId.ToLower() != "console").ToList();
            Main.AdminApi.AllAdmins = await GetAllAdmins(ignoreDeleted: false);
            Main.AdminApi.Debug("4/5 All admins setted ✔");
            var serverAdmins = admins.Where(x => x.Servers.Contains(AdminUtils.AdminApi.ThisServer.Id)).ToList();
            Main.AdminApi.ServerAdmins = serverAdmins;
            Main.AdminApi.Debug("5/5 Server admins setted ✔");
            Main.AdminApi.Debug("Admins refreshed ✔");
            Main.AdminApi.Debug("---------------");
            Main.AdminApi.Debug("Server admins:");
            Main.AdminApi.Debug($"id | name | steamId | flags | immunity | groupId | serverKey | discord | vk | isDisabled");
            foreach (var admin in serverAdmins)
            {
                Main.AdminApi.Debug($"{admin.Id} | {admin.Name} | {admin.SteamId} | {admin.Flags} | {admin.Immunity} | {admin.GroupId} | {admin.Servers} | {admin.Discord} | {admin.Vk} | {admin.IsDisabled}");
            }
            await Main.AdminApi.SendRconToAllServers("css_am_reload_admins", true);
        }
        catch (Exception e)
        {
            Main.AdminApi.LogError(e.ToString());
            throw;
        }
    }
}