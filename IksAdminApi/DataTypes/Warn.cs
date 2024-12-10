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
        int Id,
        int AdminId,
        int TargetId,
        int Duration,
        string Reason,
        int CreatedAt,
        int EndAt,
        int UpdatedAt,
        int? DeletedAt,
        int? DeletedBy
    ) {
        this.Id = Id;        
        this.AdminId = AdminId;   
        this.TargetId = TargetId;  
        this.Duration = Duration;  
        this.Reason = Reason; 
        this.CreatedAt = CreatedAt; 
        this.UpdatedAt = UpdatedAt; 
        this.EndAt = EndAt;
        this.DeletedAt = DeletedAt;
        this.DeletedBy = DeletedBy;
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