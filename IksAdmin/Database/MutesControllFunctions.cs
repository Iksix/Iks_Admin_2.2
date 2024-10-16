using Dapper;
using IksAdminApi;
using MySqlConnector;

namespace IksAdmin;

public static class MutesControllFunctions
{
    private static readonly string SelectMute = @"
    select
    id as id,
    steam_id as steamId,
    ip as ip,
    name as name,
    duration as duration,
    reason as reason,
    server_id as serverId,
    admin_id as adminId,
    unbanned_by as unbannedBy,
    unban_reason as unbanReason,
    created_at as createdAt,
    end_at as endAt,
    updated_at as updatedAt,
    deleted_at as deletedAt
    from iks_mutes
    ";
    public static async Task<PlayerMute?> GetActiveMute(string steamId)
    {
        try
        {
            await using var conn = new MySqlConnection(Database.ConnectionString);
            await conn.OpenAsync();
            var mutes = await conn.QueryFirstOrDefaultAsync<PlayerMute>(SelectMute + @"
                where deleted_at is null
                and steam_id = @steamId
                and unbanned_by is null
                and end_at > unix_timestamp()
                and (server_id is null or server_id = @serverId)
            ", new {steamId, serverId = Main.AdminApi.ThisServer.Id, timestamp = AdminUtils.CurrentTimestamp()});
            return mutes;
        }
        catch (Exception e)
        {
            Main.AdminApi.LogError(e.ToString());
            throw;
        }
    }
    public static async Task<List<PlayerMute>> GetAllMutes(string steamId)
    {
        try
        {
            await using var conn = new MySqlConnection(Database.ConnectionString);
            await conn.OpenAsync();
            var mutes = (await conn.QueryAsync<PlayerMute>($@"
                {SelectMute}
                where deleted_at is null
                and steam_id = @steamId
                and (server_id is null or server_id = @serverId)
            ", new {steamId, serverId = Main.AdminApi.ThisServer.Id})).ToList();
            return mutes;
        }
        catch (Exception e)
        {
            Main.AdminApi.LogError(e.ToString());
            throw;
        }
    }
    public static async Task<List<PlayerMute>> GetLastAdminMutes(Admin admin, int time)
    {
        try
        {
            await using var conn = new MySqlConnection(Database.ConnectionString);
            await conn.OpenAsync();
            var mutes = (await conn.QueryAsync<PlayerMute>($@"
                {SelectMute}
                where deleted_at is null
                and admin_id = @steamId
                and (server_id is null or server_id = @serverId)
                and created_at > unix_timestamp() - @time
            ", new {time, admin_id = admin.Id, serverId = Main.AdminApi.ThisServer.Id})).ToList();
            return mutes;
        }
        catch (Exception e)
        {
            Main.AdminApi.LogError(e.ToString());
            throw;
        }
    }
    public static async Task<List<PlayerMute>> GetAllMutes()
    {
        try
        {
            await using var conn = new MySqlConnection(Database.ConnectionString);
            await conn.OpenAsync();
            var mutes = (await conn.QueryAsync<PlayerMute>($@"
                {SelectMute}
                where deleted_at is null
                and (server_id is null or server_id = @serverId)
            ", new {serverId = Main.AdminApi.ThisServer.Id})).ToList();
            return mutes;
        }
        catch (Exception e)
        {
            Main.AdminApi.LogError(e.ToString());
            throw;
        }
    }
    /// <summary>
    /// return statuses: 0 - banned, 1 - already banned, -1 - other
    /// </summary>
    public static async Task<int> Add(PlayerMute punishment)
    {
        try
        {
            await using var conn = new MySqlConnection(Database.ConnectionString);
            await conn.OpenAsync();
            PlayerMute? existingMute = await GetActiveMute(punishment.SteamId);
            if (existingMute != null)
                return 1;
            await conn.QueryAsync(@"
                insert into iks_mutes
                (steam_id, ip, name, duration, reason, server_id, admin_id, unbanned_by, unban_reason, created_at, end_at, updated_at, deleted_at)
                values
                (@steamId, @ip, @name, @duration, @reason, @serverId, @adminId, @unbannedBy, @unbanReason, @createdAt, @endAt, @updatedAt, @deletedAt)
            ", new {
                steamId = punishment.SteamId,
                ip = punishment.Ip,
                name = punishment.Name,
                duration = punishment.Duration,
                reason = punishment.Reason,
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
        catch (Exception e)
        {
            Main.AdminApi.LogError(e.ToString());
            return -1;
        }
    }
    /// <summary>
    /// return statuses: 0 - unbanned, 1 - ban not finded, -1 - other
    /// </summary>
    public static async Task<int> Unmute(Admin admin, PlayerMute mute, string? reason)
    {
        try
        {
            await using var conn = new MySqlConnection(Database.ConnectionString);
            await conn.OpenAsync();

            if (!CanUnmute(admin, mute)) return 2;

            await conn.QueryAsync(@"
                update iks_mutes set 
                unbanned_by = @adminId, 
                unban_reason = @reason
                where id = @id
            ", new {
                adminId = admin.Id,
                id = mute.Id,
                reason
            });
            return 0;
        }
        catch (Exception e)
        {
            Main.AdminApi.LogError(e.ToString());
            return -1;
        }
    }

    private static bool CanUnmute(Admin admin, PlayerMute mute)
    {
        var bannedBy = mute.Admin;
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