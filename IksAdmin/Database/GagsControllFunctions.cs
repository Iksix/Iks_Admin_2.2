using Dapper;
using IksAdminApi;
using MySqlConnector;

namespace IksAdmin;

public static class GagsControllFunctions
{
    private static readonly string SelectGag = @"
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
    from iks_gags
    ";
    public static async Task<PlayerGag?> GetActiveGag(string steamId)
    {
        try
        {
            await using var conn = new MySqlConnection(Database.ConnectionString);
            await conn.OpenAsync();
            var gags = await conn.QueryFirstOrDefaultAsync<PlayerGag>(SelectGag + @"
                where deleted_at is null
                and steam_id = @steamId
                and unbanned_by is null
                and end_at > unix_timestamp()
                and (server_id is null or server_id = @serverId)
            ", new {steamId, serverId = Main.AdminApi.ThisServer.Id, timestamp = AdminUtils.CurrentTimestamp()});
            return gags;
        }
        catch (Exception e)
        {
            Main.AdminApi.LogError(e.ToString());
            throw;
        }
    }
    public static async Task<List<PlayerGag>> GetAllGags(string steamId)
    {
        try
        {
            await using var conn = new MySqlConnection(Database.ConnectionString);
            await conn.OpenAsync();
            var gags = (await conn.QueryAsync<PlayerGag>($@"
                {SelectGag}
                where deleted_at is null
                and steam_id = @steamId
                and (server_id is null or server_id = @serverId)
            ", new {steamId, serverId = Main.AdminApi.ThisServer.Id})).ToList();
            return gags;
        }
        catch (Exception e)
        {
            Main.AdminApi.LogError(e.ToString());
            throw;
        }
    }
    public static async Task<List<PlayerGag>> GetAllGags()
    {
        try
        {
            await using var conn = new MySqlConnection(Database.ConnectionString);
            await conn.OpenAsync();
            var gags = (await conn.QueryAsync<PlayerGag>($@"
                {SelectGag}
                where deleted_at is null
                and (server_id is null or server_id = @serverId)
            ", new {serverId = Main.AdminApi.ThisServer.Id})).ToList();
            return gags;
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
    public static async Task<int> Add(PlayerGag punishment)
    {
        try
        {
            await using var conn = new MySqlConnection(Database.ConnectionString);
            await conn.OpenAsync();
            PlayerGag? existingGag = await GetActiveGag(punishment.SteamId);
            if (existingGag != null)
                return 1;
            await conn.QueryAsync(@"
                insert into iks_gags
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
    /// return statuses: 0 - unbanned, 1 - ban not finded, 2 - admin can't do this, -1 - other
    /// </summary>
    public static async Task<int> Ungag(Admin admin, PlayerGag gag, string? reason)
    {
        try
        {
            await using var conn = new MySqlConnection(Database.ConnectionString);
            await conn.OpenAsync();

            if (!CanUngag(admin, gag)) return 2;

            await conn.QueryAsync(@"
                update iks_gags set 
                unbanned_by = @adminId, 
                unban_reason = @reason
                where id = @id
            ", new {
                adminId = admin.Id,
                id = gag.Id,
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

    private static bool CanUngag(Admin admin, PlayerGag gag)
    {
        var bannedBy = gag.Admin;
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