using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace IksAdmin.Functions;

public static class Database
{
    public static string ConnectionString { get; set; } = string.Empty;

    public static async Task Init()
    {
        try
        {
            using var conn = new MySqlConnection(ConnectionString);
            await conn.OpenAsync();
            await conn.QueryAsync(@"
            create table if not exists iks_servers(
                id int not null auto_increment primary key,
                server_key varchar(32) not null,
                ip varchar(32) not null,
                name varchar(64) not null,
                rcon varchar(128),
                created_at int not null,
                updated_at int not null,
                deleted_at int default null
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
            create table if not exists iks_groups(
                id int not null auto_increment primary key,
                name varchar(64) not null,
                flags varchar(32) not null,
                immunity int not null,
                created_at int not null,
                updated_at int not null,
                deleted_at int default null
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
            create table if not exists iks_admins(
                id int not null auto_increment primary key,
                steam_id varchar(17) not null,
                name varchar(64) not null,
                flags varchar(32),
                immunity int,
                group_id int,
                server_key varchar(255),
                created_at int not null,
                updated_at int not null,
                deleted_at int default null,
                foreign key (group_id) references iks_groups(id)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
            insert into iks_admins(steam_id, name, flags, immunity, created_at, updated_at)
            select 'CONSOLE', 'CONSOLE', '', 0, unix_timestamp(), unix_timestamp()
            where not exists (select 1 from iks_admins where steam_id = 'CONSOLE');

            create table if not exists iks_gags(
                id int not null auto_increment primary key,
                steam_id varchar(17) not null,
                ip varchar(32),
                name varchar(64) not null,
                duration int not null,
                server_id int,
                admin_id int not null,
                ubanned_by int,
                created_at int not null,
                end_at int not null,
                updated_at int not null,
                deleted_at int default null,
                foreign key (admin_id) references iks_admins(id),
                foreign key (ubanned_by) references iks_admins(id),
                foreign key (server_id) references iks_servers(id)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
            create table if not exists iks_mutes(
                id int not null auto_increment primary key,
                steam_id varchar(17) not null,
                ip varchar(32),
                name varchar(64) not null,
                duration int not null,
                server_id int,
                admin_id int not null,
                ubanned_by int,
                created_at int not null,
                end_at int not null,
                updated_at int not null,
                deleted_at int default null,
                foreign key (admin_id) references iks_admins(id),
                foreign key (ubanned_by) references iks_admins(id),
                foreign key (server_id) references iks_servers(id)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
            create table if not exists iks_bans(
                id int not null auto_increment primary key,
                steam_id varchar(17) not null,
                ip varchar(32),
                name varchar(64) not null,
                duration int not null,
                ban_ip tinyint not null default 0,
                server_id int,
                admin_id int not null,
                ubanned_by int,
                created_at int not null,
                end_at int not null,
                updated_at int not null,
                deleted_at int default null,
                foreign key (admin_id) references iks_admins(id),
                foreign key (ubanned_by) references iks_admins(id),
                foreign key (server_id) references iks_servers(id)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
            ");
        }
        catch (MySqlException e)
        {
            Main.AdminApi.LogError(e.ToString());
            throw;
        }
    }
}