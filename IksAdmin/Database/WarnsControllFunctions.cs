using Dapper;
using IksAdminApi;
using MySqlConnector;

namespace IksAdmin;

public static class WarnsControllFunctions
{
    private static string WarnSelect = @"
    select 
    id as Id,
    admin_id as AdminId,
    target_id as TargetId,
    duration as Duration,
    reason as Reason,
    created_at as CreatedAt,
    end_at as UpdatedAt,
    updated_at as EndAt,
    deleted_at as DeletedAt,
    deleted_by as DeletedBy
    from iks_admins_warns
    ";

    public static async Task<List<Warn>> GetAllActive() {
        try
        {
            await using var conn = new MySqlConnection(Database.ConnectionString);
            await conn.OpenAsync();

            var warns = (await conn.QueryAsync<Warn>($@"
            {WarnSelect}
            where 
            (deleted_at = null or deleted_by = null)
            and (end_at > unix_timestamp() or end_at = 0)
            ")).ToList();
            return warns;
        }
        catch (MySqlException e)
        {
            Main.AdminApi.LogError(e.ToString());
            throw;
        }
    }
    public static async Task<Warn> InsertToBase(this Warn warn) {
        try
        {
            await using var conn = new MySqlConnection(Database.ConnectionString);
            await conn.OpenAsync();

            int id = await conn.QuerySingleAsync<int>(@"
            insert into iks_admins_warns
            (admin_id, target_id, duration, reason, created_at, end_at, updated_at)
            values
            (@adminId, @targetId, @duration, @reason, @createdAt, @endAt, @updatedAt);
            select last_insert_id();
            ", new {
                adminId = warn.AdminId,
                targetId = warn.TargetId,
                duration = warn.Duration,
                reason = warn.Reason,
                createdAt = warn.CreatedAt,
                endAt = warn.EndAt,
                updatedAt = warn.UpdatedAt,
            });

            warn.Id = id;

            return warn;
        }
        catch (MySqlException e)
        {
            Main.AdminApi.LogError(e.ToString());
            throw;
        }
    }
    public static async Task Update(this Warn warn) {
        try
        {
            await using var conn = new MySqlConnection(Database.ConnectionString);
            await conn.OpenAsync();
            warn.UpdatedAt = AdminUtils.CurrentTimestamp();
            int id = await conn.QuerySingleAsync<int>(@"
            update iks_admins_warns set
            admin_id = @adminId,
            target_id = @targetId,
            duration = @duration,
            reason = @reason,
            end_at = @endAt,
            updated_at = @updatedAt,
            deleted_at = @deletedAt,
            deleted_by = @deletedBy
            where id = @id
            ", new {
                id = warn.Id,
                adminId = warn.AdminId,
                targetId = warn.TargetId,
                duration = warn.Duration,
                reason = warn.Reason,
                createdAt = warn.CreatedAt,
                endAt = warn.EndAt,
                updatedAt = AdminUtils.CurrentTimestamp(),
                deletedBy = warn.DeletedBy,
                deleted_at = warn.DeletedAt
            });
        }
        catch (MySqlException e)
        {
            Main.AdminApi.LogError(e.ToString());
            throw;
        }
    }
    public static async Task<List<Warn>> GetAllActiveForAdmin(int id) {
        try
        {
            await using var conn = new MySqlConnection(Database.ConnectionString);
            await conn.OpenAsync();

            var warns = (await conn.QueryAsync<Warn>($@"
            {WarnSelect}
            where 
            target_id = @id and
            (deleted_at = null)
            and (end_at > unix_timestamp() or end_at = 0)
            ", new {id})).ToList();
            return warns;
        }
        catch (MySqlException e)
        {
            Main.AdminApi.LogError(e.ToString());
            throw;
        }
    }
    public static async Task<List<Warn>> GetAllActiveByAdmin(int id) {
        try
        {
            await using var conn = new MySqlConnection(Database.ConnectionString);
            await conn.OpenAsync();

            var warns = (await conn.QueryAsync<Warn>($@"
            {WarnSelect}
            where 
            admin_id = @id and
            (deleted_at = null)
            and (end_at > unix_timestamp() or end_at = 0)
            ", new {id})).ToList();
            return warns;
        }
        catch (MySqlException e)
        {
            Main.AdminApi.LogError(e.ToString());
            throw;
        }
    }
}