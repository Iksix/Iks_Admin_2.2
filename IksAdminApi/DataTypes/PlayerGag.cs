using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IksAdminApi;

public class PlayerGag
{
    public int Id {get; set;}
    public string SteamId {get; set;}
    public string? Ip {get; set;}
    public string? Name {get; set;}
    public string Reason {get; set;}
    public int Duration {get; set;}
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
    public PlayerGag(int id, string steamId, string? ip, string? name, int duration, string reason, int? serverId, int adminId, int? unbannedBy, string? unbanReason, int createdAt, int endAt, int updatedAt, int? deletedAt)
    {
        Id = id;
        SteamId = steamId;
        Ip = ip;
        Name = name;
        Duration = duration;
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
    public PlayerGag(string steamId, string? ip, string? name, string reason, int duration, int? serverId = null)
    {
        SteamId = steamId;
        Ip = ip;
        Name = name;
        Duration = duration;
        Reason = reason; 
        ServerId = serverId;
        if (AdminUtils.Config().MirrorsIp.Contains(Ip)) Ip = null;
    }

    public PlayerGag(PlayerInfo player, string reason, int duration, int? serverId = null)
    {
        SteamId = player.SteamId;
        Ip = player.Ip;
        Name = player.PlayerName;
        Duration = duration;
        Reason = reason; 
        ServerId = serverId;
        if (AdminUtils.Config().MirrorsIp.Contains(Ip)) Ip = null;
    }
}