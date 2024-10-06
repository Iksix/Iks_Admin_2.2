using Dapper;
using IksAdminApi;
using MySqlConnector;

namespace IksAdmin;

public static class BansControllFunctions
{
    private static readonly string SelectBans = @"
    select
    id as id,
    steam_id as steamId,
    ip as ip,
    name as name,
    duration as duration,
    reason as reason,
    ban_ip as banIp,
    server_id as serverId,
    admin_id as adminId,
    unbanned_by as unbannedBy,
    unban_reason as unbanReason,
    created_at as createdAt,
    end_at as endAt,
    updated_at as updatedAt,
    deleted_at as deletedAt
    from iks_bans
    ";
    public static async Task<PlayerBan?> GetActiveBan(string steamId)
    {
        try
        {
            await using var conn = new MySqlConnection(Database.ConnectionString);
            await conn.OpenAsync();
            var ban = await conn.QueryFirstOrDefaultAsync<PlayerBan>($@"
                {SelectBans}
                where deleted_at is null
                and steam_id = @steamId
                and unbanned_by is null
                and end_at > unix_timestamp()
                and (server_id is null or server_id = @serverId)
            ", new {steamId, serverId = Main.AdminApi.ThisServer.Id});
            return ban;
        }
        catch (MySqlException e)
        {
            Main.AdminApi.LogError(e.ToString());
            throw;
        }
    }
    public static async Task<PlayerBan?> GetActiveBanIp(string ip)
    {
        try
        {
            await using var conn = new MySqlConnection(Database.ConnectionString);
            await conn.OpenAsync();
            var ban = await conn.QueryFirstOrDefaultAsync<PlayerBan>($@"
                {SelectBans}
                where deleted_at is null
                and ip = @ip and ban_ip = 1
                and unbanned_by is null
                and end_at > unix_timestamp() or end_at is null
                and server_id is null or server_id = @serverId
            ", new {ip, serverId = Main.AdminApi.ThisServer.Id});
            return ban;
        }
        catch (MySqlException e)
        {
            Main.AdminApi.LogError(e.ToString());
            throw;
        }
    }
    public static async Task<List<PlayerBan>> GetAllBans(string steamId)
    {
        try
        {
            await using var conn = new MySqlConnection(Database.ConnectionString);
            await conn.OpenAsync();
            var bans = (await conn.QueryAsync<PlayerBan>($@"
                {SelectBans}
                where deleted_at is null
                and steam_id = @steamId
                and server_id is null or server_id = @serverId
            ", new {steamId, serverId = Main.AdminApi.ThisServer.Id})).ToList();
            return bans;
        }
        catch (MySqlException e)
        {
            Main.AdminApi.LogError(e.ToString());
            throw;
        }
    }
    public static async Task<List<PlayerBan>> GetAllBans()
    {
        try
        {
            await using var conn = new MySqlConnection(Database.ConnectionString);
            await conn.OpenAsync();
            var bans = (await conn.QueryAsync<PlayerBan>($@"
                {SelectBans}
                where deleted_at is null
                and server_id is null or server_id = @serverId
            ", new {serverId = Main.AdminApi.ThisServer.Id})).ToList();
            return bans;
        }
        catch (MySqlException e)
        {
            Main.AdminApi.LogError(e.ToString());
            throw;
        }
    }
    /// <summary>
    /// return statuses: 0 - banned, 1 - already banned, -1 - other
    /// </summary>
    public static async Task<int> Add(PlayerBan punishment)
    {
        try
        {
            await using var conn = new MySqlConnection(Database.ConnectionString);
            await conn.OpenAsync();
            PlayerBan? existingBan = null;
            if (punishment.BanIp == 1 && punishment.Ip != null)
                existingBan = await GetActiveBanIp(punishment.Ip);
            else if (punishment.BanIp == 0 && punishment.SteamId != null) existingBan = await GetActiveBan(punishment.SteamId);
            if (existingBan != null)
                return 1;
            
            await conn.QueryAsync(@"
                insert into iks_bans
                (steam_id, ip, name, duration, reason, ban_ip, server_id, admin_id, unbanned_by, unban_reason, created_at, end_at, updated_at, deleted_at)
                values
                (@steamId, @ip, @name, @duration, @reason, @banIp, @serverId, @adminId, @unbannedBy, @unbanReason, @createdAt, @endAt, @updatedAt, @deletedAt)
            ", new {
                steamId = punishment.SteamId,
                ip = punishment.Ip,
                name = punishment.Name,
                duration = punishment.Duration,
                reason = punishment.Reason,
                banIp = punishment.BanIp,
                serverId = punishment.ServerId,
                adminId = punishment.AdminId,
                unbannedBy = punishment.UnbannedBy,
                unbanReason = punishment.UnbanReason,
                createdAt = punishment.CreatedAt,
                endAt = punishment.EndAt,
                updatedAt = punishment.UpdatedAt,
                deletedAt = punishment.DeletedAt
            });

            return 0;
        }
        catch (MySqlException e)
        {
            Main.AdminApi.LogError(e.ToString());
            return -1;
        }
    }
    /// <summary>
    /// return statuses: 0 - unbanned, 1 - ban not finded, -1 - other
    /// </summary>
    public static async Task<int> Unban(Admin admin, string steamId, string? reason)
    {
        try
        {
            await using var conn = new MySqlConnection(Database.ConnectionString);
            await conn.OpenAsync();
            PlayerBan? existingBan = await GetActiveBan(steamId);

            if (existingBan == null)
                return 1;

            if (!CanUnban(admin, existingBan)) return 2;

            await conn.QueryAsync(@"
                update iks_bans set 
                unbanned_by = @adminId, 
                unban_reason = @reason
                where id = @banId
            ", new {
                adminId = admin.Id,
                banId = existingBan.Id,
                reason
            });
            return 0;
        }
        catch (MySqlException e)
        {
            Main.AdminApi.LogError(e.ToString());
            return -1;
        }
    }
    /// <summary>
    /// return statuses: 0 - unbanned, 1 - ban not finded, -1 - other
    /// </summary>
    public static async Task<int> UnbanIp(Admin admin, string ip, string? reason)
    {
        try
        {
            await using var conn = new MySqlConnection(Database.ConnectionString);
            await conn.OpenAsync();
            PlayerBan? existingBan = await GetActiveBanIp(ip);

            if (existingBan == null) return 1;

            if (!CanUnban(admin, existingBan)) return 2;

            await conn.QueryAsync(@"
                update iks_bans set 
                unbanned_by = @adminId, 
                unban_reason = @reason
                where id = @banId
            ", new {
                adminId = admin.Id,
                banId = existingBan.Id,
                reason
            });
            return 0;
        }
        catch (MySqlException e)
        {
            Main.AdminApi.LogError(e.ToString());
            return -1;
        }
    }

    private static bool CanUnban(Admin admin, PlayerBan existingBan)
    {
        var bannedBy = existingBan.Admin;
        if (bannedBy == null) return true;
        if (bannedBy.SteamId == admin.SteamId) return true;
        if (bannedBy.SteamId != "CONSOLE")
        {
            if (admin.HasPermissions("blocks_manage.remove_all")) return true;
        } else {
            if (admin.HasPermissions("blocks_manage.remove_console")) return true;
            return false;
        }
        if (admin.HasPermissions("blocks_manage.remove_immunity") && bannedBy.CurrentImmunity < admin.CurrentImmunity) return true;
        if (admin.HasPermissions("other.equals_immunity_action") && admin.HasPermissions("blocks_manage.remove_immunity") && bannedBy.CurrentImmunity <= admin.CurrentImmunity) return true;
        return false;
    }
}