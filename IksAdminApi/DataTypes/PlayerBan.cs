using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IksAdminApi;

public class PlayerBan
{
    public int Id {get; set;}
    public string? SteamId {get; set;}
    public string? Ip {get; set;}
    public string? Name {get; set;}
    public string Reason {get; set;}
    public int Duration {get; set;}
    public int BanIp {get; set;} = 0;
    public int? ServerId {get; set;} = null;
    public int AdminId {get; set;}
    public int EndAt {get; set;}
    public int? UnbannedBy {get; set;}
    public string? UnbanReason {get; set;}
    public int CreatedAt {get; set;} = AdminUtils.CurrentTimestamp();
    public int UpdatedAt {get; set;} = AdminUtils.CurrentTimestamp();
    public int? DeletedAt {get; set;} = null;

    public Admin? Admin {get {
        return AdminUtils.FindAdminByIdMethod(AdminId);
    }}
    public Admin? UnbannedByAdmin {get {
        if (UnbannedBy == null) return null;
        return AdminUtils.FindAdminByIdMethod((int)UnbannedBy);
    }}
    public ServerModel? Server {get {
        if (ServerId == null) return null;
        return AdminUtils.AdminApi.AllServers.FirstOrDefault(x => x.Id == ServerId);
    }}
    // used for getting from db
    public PlayerBan(int id, string? steamId, string? ip, string? name, int duration, string reason, int banIp, int? serverId, int adminId, int? unbannedBy, string? unbanReason, int createdAt, int endAt, int updatedAt, int? deletedAt)
    {
        Id = id;
        SteamId = steamId;
        Ip = ip;
        Name = name;
        Duration = duration;
        BanIp = banIp;
        Reason = reason; 
        ServerId = serverId;
        AdminId = adminId;
        UnbannedBy = unbannedBy;
        CreatedAt = createdAt;
        EndAt = endAt;
        UpdatedAt = updatedAt;
        DeletedAt = deletedAt;
        UnbanReason = unbanReason;
    }
    // creating ===
    public PlayerBan(string? steamId, string? ip, string? name, string reason, int duration, int? serverId = null, bool banIp = false)
    {
        SteamId = steamId;
        Ip = ip;
        Name = name;
        Duration = duration;
        Reason = reason; 
        ServerId = serverId;
        if (banIp) BanIp = 1;
        if (AdminUtils.Config().MirrorsIp.Contains(Ip)) Ip = null;
    }

    public PlayerBan(PlayerInfo player, string reason, int duration, int? serverId = null, bool banIp = false)
    {
        SteamId = player.SteamId;
        Ip = player.Ip;
        Name = player.PlayerName;
        Duration = duration;
        Reason = reason; 
        ServerId = serverId;
        if (banIp) BanIp = 1;
        if (AdminUtils.Config().MirrorsIp.Contains(Ip)) Ip = null;
    }
}