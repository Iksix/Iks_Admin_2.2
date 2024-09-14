create table if not exists iks_servers(
    id int not null auto_increment primary key,
    server_key varchar(32) not null,
    ip varchar(32) not null,
    name varchar(64) not null,
    rcon varchar(128) default null,
    created_at int not null,
    updated_at int not null,
    deleted_at int default null
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
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
    is_disabled int(1) not null default 0,
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
    reason varchar(128) not null,
    server_id int default null,
    admin_id int not null,
    ubanned_by int default null,
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
    reason varchar(128) not null,
    server_id int default null,
    admin_id int not null,
    ubanned_by int default null,
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
    reason varchar(128) not null,
    ban_ip tinyint not null default 0,
    server_id int default null,
    admin_id int not null,
    ubanned_by int default null,
    created_at int not null,
    end_at int not null,
    updated_at int not null,
    deleted_at int default null,
    foreign key (admin_id) references iks_admins(id),
    foreign key (ubanned_by) references iks_admins(id),
    foreign key (server_id) references iks_servers(id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
create table if not exists iks_admin_warns(
    id int not null auto_increment primary key,
    admin_id int not null,
    target_id int not null,
    duration int not null,
    reason varchar(128) not null,
    created_at int not null,
    end_at int not null,
    updated_at int not null,
    deleted_at int default null,
    removed_by int default null,
    foreign key (admin_id) references iks_admins(id),
    foreign key (target_id) references iks_servers(id),
    foreign key (removed_by) references iks_admins(id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;