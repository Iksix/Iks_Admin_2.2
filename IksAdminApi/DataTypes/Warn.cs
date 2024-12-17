namespace IksAdminApi;

public class Warn
{
    public int Id {get; set;}
    public int AdminId {get; set;}
    public int TargetId {get; set;}
    public int Duration {get; set;}
    public string Reason {get; set;}
    public int CreatedAt {get; set;} = AdminUtils.CurrentTimestamp();
    public int EndAt {get; set;}
    public int UpdatedAt {get; set;} = AdminUtils.CurrentTimestamp();
    public int? DeletedAt {get; set;} = null;
    public int? DeletedBy {get; set;} = null;

    public Admin? Admin {get {
        return AdminUtils.Admin(AdminId);
    }}
    public Admin? TargetAdmin {get {
        return AdminUtils.Admin(TargetId);
    }}
    public Admin? DeletedByAdmin {get {
        return DeletedBy == null ? null : AdminUtils.Admin((int)DeletedBy);
    }}

    public Warn(
        int id,
        int adminId,
        int targetId,
        int duration,
        string reason,
        int createdAt,
        int updatedAt,
        int endAt,
        int? deletedAt,
        int? deletedBy
    )
    {
        Id = id;
        AdminId = adminId;
        TargetId = targetId;
        Duration = duration;
        Reason = reason;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        EndAt = endAt;
        DeletedAt = deletedAt;
        DeletedBy = deletedBy;
    }
    public Warn(
        int AdminId,
        int TargetId,
        int Duration,
        string Reason
    ) {
        this.AdminId = AdminId;   
        this.TargetId = TargetId;  
        this.Duration = Duration;  
        this.Reason = Reason; 
        EndAt = Duration == 0 ? 0 : AdminUtils.CurrentTimestamp() + Duration;
    }
}