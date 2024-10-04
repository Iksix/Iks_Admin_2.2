namespace IksAdminApi;

public class Group {
    public int Id {get; set;}
    public string Name {get; set;}
    public string Flags {get; set;}
    public int Immunity {get; set;}
    public string? Comment {get; set;}
    public int CreatedAt {get; set;} = AdminUtils.CurrentTimestamp();
    public int UpdatedAt {get; set;} = AdminUtils.CurrentTimestamp();
    public int? DeletedAt {get; set;} = null;

    /// <summary>
    /// For getting from db
    /// </summary>
    public Group(int id, string name, string flags, int immunity, string? comment, int createdAt, int updatedAt, int? deletedAt)
    {
        Id = id;
        Name = name;
        Flags = flags;
        Immunity = immunity;
        Comment = comment;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        DeletedAt = deletedAt;
    }
    /// <summary>
    /// For creating new group
    /// </summary>
    public Group(string name, string flags, int immunity, string? comment = null)
    {
        Name = name;
        Flags = flags;
        Immunity = immunity;
        Comment = comment;
    }
    
}