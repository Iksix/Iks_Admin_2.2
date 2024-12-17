using Dapper;
using IksAdminApi;
using MySqlConnector;

namespace IksAdmin;

public static class DBGroups
{
    private static string GroupSelect = @"
        select
        id as id,
        name as name,
        flags as flags,
        immunity as immunity,
        comment as comment,
        created_at as createdAt,
        updated_at as updatedAt,
        deleted_at as deletedAt
        from iks_groups
    ";
    public static async Task<Group> AddGroup(Group group)
    {
        try
        {
            await using var conn = new MySqlConnection(DB.ConnectionString);
            await conn.OpenAsync();
            var existingGroup = await GetGroup(group.Name, ignoreDeleted: false);
            if (existingGroup != null)
            {
                var oldGroup = Main.AdminApi.Groups.FirstOrDefault(g => g.Id == existingGroup.Id);
                Main.AdminApi.Debug($"Group {group.Name} already exists...");
                Main.AdminApi.Debug($"Set new group {group.Name} id = {existingGroup.Id} ✔");
                group.Id = existingGroup.Id;
                Main.AdminApi.Debug($"Update group in base...");
                return await UpdateGroupInBase(group);
            }
            Main.AdminApi.Debug($"Add group to base...");
            return await AddGroupToBase(group);
        }
        catch (MySqlException e)
        {
            Main.AdminApi.LogError(e.ToString());
            throw;
        }
    }
    public static async Task<Group?> GetGroup(string groupName, bool ignoreDeleted = true)
    {
        try
        {
            await using var conn = new MySqlConnection(DB.ConnectionString);
            await conn.OpenAsync();
            var ignoreDeletedString = ignoreDeleted ? "and deleted_at is null" : "";
            var group = await conn.QueryFirstOrDefaultAsync<Group>($@"
                {GroupSelect}
                where name = @groupName
                {ignoreDeletedString}
            ", new { groupName });

            return group;
        }
        catch (MySqlException e)
        {
            Main.AdminApi.LogError(e.ToString());
            throw;
        }
    }

    public static async Task<List<Group>> GetAllGroups(bool ignoreDeleted = true)
    {
        try
        {
            await using var conn = new MySqlConnection(DB.ConnectionString);
            await conn.OpenAsync();
            var ignoreDeletedString = ignoreDeleted ? "where deleted_at is null" : "";
            var groups = (await conn.QueryAsync<Group>($@"
                {GroupSelect}
                {ignoreDeletedString}
            ")).ToList();

            return groups;
        }
        catch (MySqlException e)
        {
            Main.AdminApi.LogError(e.ToString());
            throw;
        }
    }

    public static async Task<Group> AddGroupToBase(Group group)
    {
        try
        {
            await using var conn = new MySqlConnection(DB.ConnectionString);
            await conn.OpenAsync();
            var id = await conn.QuerySingleAsync<int>(@"
                insert into iks_groups
                ( name, flags, immunity, comment, created_at, updated_at)
                values
                (@name, @flags, @immunity, @comment, unix_timestamp(), unix_timestamp());
                select last_insert_id();
            ", new {
                name = group.Name,
                flags = group.Flags,
                immunity = group.Immunity,
                comment = group.Comment
            });
            group.Id = id;
            Main.AdminApi.Debug($"Group added to base ✔");
            Main.AdminApi.Debug($"Group id = {group!.Id} ✔");
            Main.AdminApi.Groups.Add(group);
            return group;
        }
        catch (MySqlException e)
        {
            Main.AdminApi.LogError(e.ToString());
            throw;
        }
    }
    public static async Task<Group> UpdateGroupInBase(Group group)
    {
        try
        {
            await using var conn = new MySqlConnection(DB.ConnectionString);
            await conn.OpenAsync();
            await conn.QueryAsync(@"
                update iks_groups set 
                name = @name,
                flags = @flags,
                immunity = @immunity,
                comment = @comment,
                updated_at = unix_timestamp(),
                deleted_at = null
                where id = @id 
            ", new {
                id = group.Id,
                name = group.Name,
                flags = group.Flags,
                immunity = group.Immunity,
                comment = group.Comment
            });
            var pluginGroup = Main.AdminApi.Groups.FirstOrDefault(x => x.Id == group.Id);
            if (pluginGroup != null)
                pluginGroup = group;
            Main.AdminApi.Debug($"Group updated in base ✔");
            return group;
        }
        catch (MySqlException e)
        {
            Main.AdminApi.LogError(e.ToString());
            throw;
        }
    }
    public static async Task DeleteGroup(string name)
    {
        try
        {
            await using var conn = new MySqlConnection(DB.ConnectionString);
            await conn.OpenAsync();
            await conn.QueryAsync(@"
                update iks_groups set 
                deleted_at = current_timestamp()
                where name = @name
            ", new {
                name
            });
            Main.AdminApi.Debug($"Group deleted ✔");
        }
        catch (MySqlException e)
        {
            Main.AdminApi.LogError(e.ToString());
            throw;
        }
    }

    public static async Task RefreshGroups()
    {
        try
        {
            Main.AdminApi.Debug("Refresing groups...");
            var groups = await GetAllGroups();
            Main.AdminApi.Debug("1/2 Groups getted ✔");
            Main.AdminApi.Groups = groups;
            Main.AdminApi.Debug("2/2 Groups setted ✔");
            Main.AdminApi.Debug("Groups refreshed ✔");
            Main.AdminApi.Debug("---------------");
            Main.AdminApi.Debug("Groups:");
            Main.AdminApi.Debug("id | name | flags | immunity");
            foreach (var group in groups)
            {
                Main.AdminApi.Debug($"{group.Id} | {group.Name} | {group.Flags} | {group.Immunity}");
            }
            await RefreshLimitations();
        }
        catch (Exception e)
        {
            Main.AdminApi.LogError(e.ToString());
            throw;
        }
    }
    public static async Task RefreshLimitations()
    {
        try
        {
            Main.AdminApi.Debug("Refresing limitations...");
            var limitations = await GetAllLimitations();
            Main.AdminApi.Debug("1/2 limitations getted ✔");
            Main.AdminApi.GroupLimitations = limitations;
            Main.AdminApi.Debug("2/2 limitations setted ✔");
            Main.AdminApi.Debug("limitations refreshed ✔");
            Main.AdminApi.Debug("---------------");
        }
        catch (Exception e)
        {
            Main.AdminApi.LogError(e.ToString());
            throw;
        }
    }

    private static async Task<List<GroupLimitation>> GetAllLimitations()
    {
        try
        {
            await using var conn = new MySqlConnection(DB.ConnectionString);
            await conn.OpenAsync();
            var limitations = (await conn.QueryAsync<GroupLimitation>(@"
                select
                id as id,
                group_id as groupId,
                limitation_key as limitationKey,
                limitation_value as limitationValue,
                created_at as createdAt,
                updated_at as updatedAt,
                deleted_at as deletedAt
                from iks_groups_limitations
                where deleted_at is null
            ")).ToList();
            return limitations;
        }
        catch (MySqlException e)
        {
            Main.AdminApi.LogError(e.ToString());
            throw;
        }
    }
}