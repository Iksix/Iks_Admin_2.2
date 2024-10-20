using CounterStrikeSharp.API.Core;

namespace IksAdminApi;

public class Admin 
{
    public delegate string GetCurrentFlagsMethod(Admin admin);
    public static GetCurrentFlagsMethod GetCurrentFlagsFunc = null!;
    public delegate int GetCurrentImmunityMethod(Admin admin);
    public static GetCurrentImmunityMethod GetCurrentImmunityFunc = null!;
    public int Id {get; set;}
    public string SteamId {get; set;} = "";
    public string Name {get; set;}
    public string? Flags {get; set;}
    public int? Immunity {get; set;}
    public int? GroupId {get; set;} = null;
    public string? ServerKey {get; set;}
    public string? Discord {get; set;}
    public string? Vk {get; set;}
    public int Disabled {get; set;}
    public int CreatedAt {get; set;} = AdminUtils.CurrentTimestamp();
    public int UpdatedAt {get; set;} = AdminUtils.CurrentTimestamp();
    public int? DeletedAt {get; set;} = null;
    public int? EndAt {get; set;}
    public bool Online {get {
        return AdminUtils.GetControllerBySteamId(SteamId) != null;
    }}
    public string CurrentFlags { get {
        return GetCurrentFlagsFunc(this);
    }}
    public int CurrentImmunity { get {
        return GetCurrentImmunityFunc(this);
    }}
    public Group? Group { get {
        return AdminUtils.GetGroup(GroupId);
    }}
    public bool IsDisabled {get {
        return Disabled == 1;
    }}
    public List<string> ServerKeys { get  {
        var keys = new List<string>();
        if (ServerKey != null && ServerKey.Length > 0)
        {
            foreach (var key in ServerKey.Split(";"))
            {
                keys.Add(key);
            }
        }
        return keys;
    } }
    public CCSPlayerController? Controller { get => AdminUtils.GetControllerBySteamId(SteamId); } 
    public bool isConsole { get => Id == 1;}


    // Limitations ===
    public int MinBanTime {get {
        if (Group == null) return 0;
        var limit = Group.Limitations.FirstOrDefault(x => x.LimitationKey == "min_ban_time");
        if (limit == null) return 0;
        else return int.Parse(limit.LimitationValue);
    }}
    public int MaxBanTime {get {
        if (Group == null) return 0;
        var limit = Group.Limitations.FirstOrDefault(x => x.LimitationKey == "max_ban_time");
        if (limit == null) return 0;
        else return int.Parse(limit.LimitationValue);
    }}
    public int MinGagTime {get {
        if (Group == null) return 0;
        var limit = Group.Limitations.FirstOrDefault(x => x.LimitationKey == "min_gag_time");
        if (limit == null) return 0;
        else return int.Parse(limit.LimitationValue);
    }}
    public int MaxGagTime {get {
        if (Group == null) return 0;
        var limit = Group.Limitations.FirstOrDefault(x => x.LimitationKey == "max_gag_time");
        if (limit == null) return 0;
        else return int.Parse(limit.LimitationValue);
    }}
    public int MinMuteTime {get {
        if (Group == null) return 0;
        var limit = Group.Limitations.FirstOrDefault(x => x.LimitationKey == "min_mute_time");
        if (limit == null) return 0;
        else return int.Parse(limit.LimitationValue);
    }}
    public int MaxMuteTime {get {
        if (Group == null) return 0;
        var limit = Group.Limitations.FirstOrDefault(x => x.LimitationKey == "max_mute_time");
        if (limit == null) return 0;
        else return int.Parse(limit.LimitationValue);
    }}
    public int MaxBansInDay {get {
        if (Group == null) return 0;
        var limit = Group.Limitations.FirstOrDefault(x => x.LimitationKey == "max_bans_in_day");
        if (limit == null) return 0;
        else return int.Parse(limit.LimitationValue);
    }}
    public int MaxGagsInDay {get {
        if (Group == null) return 0;
        var limit = Group.Limitations.FirstOrDefault(x => x.LimitationKey == "max_gags_in_day");
        if (limit == null) return 0;
        else return int.Parse(limit.LimitationValue);
    }}
    public int MaxMutesInDay {get {
        if (Group == null) return 0;
        var limit = Group.Limitations.FirstOrDefault(x => x.LimitationKey == "max_mutes_in_day");
        if (limit == null) return 0;
        else return int.Parse(limit.LimitationValue);
    }}
    public int MaxBansInRound {get {
        if (Group == null) return 0;
        var limit = Group.Limitations.FirstOrDefault(x => x.LimitationKey == "max_bans_in_round");
        if (limit == null) return 0;
        else return int.Parse(limit.LimitationValue);
    }}
    public int MaxGagsInRound {get {
        if (Group == null) return 0;
        var limit = Group.Limitations.FirstOrDefault(x => x.LimitationKey == "max_gags_in_round");
        if (limit == null) return 0;
        else return int.Parse(limit.LimitationValue);
    }}
    public int MaxMutesInRound {get {
        if (Group == null) return 0;
        var limit = Group.Limitations.FirstOrDefault(x => x.LimitationKey == "max_mutes_in_round");
        if (limit == null) return 0;
        else return int.Parse(limit.LimitationValue);
    }}
    /// <summary>
    /// For getting from db
    /// </summary>
    public Admin(int id, string steamId, string name, string? flags, int? immunity, int? groupId, string? serverKey, string? discord, string? vk, int isDisabled, int? endAt, int createdAt, int updatedAt, int? deletedAt)
    {
        Id = id;
        SteamId = steamId;
        Name = name;
        Flags = flags;
        Immunity = immunity;
        GroupId = groupId;
        Disabled = isDisabled;
        ServerKey = serverKey;  
        Discord = discord;
        Vk = vk;
        EndAt = endAt;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        DeletedAt = deletedAt;
    }
    /// <summary>
    /// For creating new admin
    /// </summary>
    public Admin(string steamId, string name, string? flags = null, int? immunity = null, int? groupId = null, string? serverKey = null, string? discord = null, string? vk = null, int? endAt = null)
    {
        SteamId = steamId;
        Name = name;
        Flags = flags;
        Immunity = immunity;
        GroupId = groupId;
        ServerKey = serverKey;
        Discord = discord;
        Vk = vk;
        EndAt = endAt;
        CreatedAt = AdminUtils.CurrentTimestamp();
        UpdatedAt = AdminUtils.CurrentTimestamp();
    }
}