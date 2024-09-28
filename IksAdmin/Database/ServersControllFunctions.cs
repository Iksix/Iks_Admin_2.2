using Dapper;
using IksAdminApi;
using MySqlConnector;

namespace IksAdmin.Functions;

public static class ServersControllFunctions
{
    public static async Task Add(ServerModel server)
    {
        try
        {
            Main.AdminApi.Debug("Add server to base...");
            await using var conn = new MySqlConnection(Database.ConnectionString);
            await conn.OpenAsync();
            var existingServer = await Get(server.ServerKey);
            if (existingServer != null)
            {
                Main.AdminApi.Debug("Server exists with id " + existingServer.Id);
                server.Id = existingServer.Id;
                await Update(server);
                return;
            }
            await conn.QueryAsync(@"
                insert into iks_servers(server_key, ip, name, rcon, created_at, updated_at)
                values(@serverKey, @ip, @name, @rcon, current_timestamp(), current_timestamp())
            ", new {
                serverKey = server.ServerKey,
                ip = server.Ip,
                name = server.Name,
                rcon = server.Rcon
            });
            Main.AdminApi.Debug("Server added to db ✔");
        }
        catch (MySqlException e)
        {
            Main.AdminApi.LogError(e.ToString());
            throw;
        }
    }

    private static async Task Update(ServerModel server)
    {
        try
        {
            Main.AdminApi.Debug("Server update...");
            await using var conn = new MySqlConnection(Database.ConnectionString);
            await conn.OpenAsync();
            await conn.QueryAsync(@"
                update iks_servers set
                server_key = @serverKey,
                ip = @ip,
                name = @name,
                rcon = @rcon,
                updated_at = current_timestamp(),
                deleted_at = null
                where id = @id
            ", 
            new {
                serverKey = server.ServerKey,
                ip = server.Ip,
                name = server.Name,
                rcon = server.Rcon,
                id = server.Id
            }
            );
            Main.AdminApi.Debug($"Server updated ✔");
        }
        catch (MySqlException e)
        {
            Main.AdminApi.LogError(e.ToString());
            throw;
        }
    }

    public static async Task<ServerModel?> Get(string serverKey)
    {
        try
        {
            await using var conn = new MySqlConnection(Database.ConnectionString);
            await conn.OpenAsync();

            var server = await conn.QueryFirstOrDefaultAsync<ServerModel>(@"
                select
                id as id,
                server_key as serverKey,
                ip as ip,
                name as name,
                created_at as createdAt,
                updated_at as updatedAt,
                deleted_at as deletedAt
                from iks_servers
                where server_key = @serverKey
                and deleted_at is null
            ");
            return server;
        }
        catch (MySqlException e)
        {
            Main.AdminApi.LogError(e.ToString());
            throw;
        }
    }
    public static async Task<List<ServerModel>> GetAll()
    {
        try
        {
            await using var conn = new MySqlConnection(Database.ConnectionString);
            await conn.OpenAsync();

            var servers = (await conn.QueryAsync<ServerModel>(@"
                select
                id as id,
                server_key as serverKey,
                ip as ip,
                name as name,
                created_at as createdAt,
                updated_at as updatedAt,
                deleted_at as deletedAt
                from iks_servers
                where deleted_at is null
            ")).ToList();

            return servers;
        }
        catch (MySqlException e)
        {
            Main.AdminApi.LogError(e.ToString());
            throw;
        }
    }
}