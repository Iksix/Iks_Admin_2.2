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