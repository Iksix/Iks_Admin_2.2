using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IksAdminApi;

public class GroupLimitation
{
    public int Id {get; set;}
    public int GroupId {get; set;}
    public string LimitationKey {get; set;}
    public string LimitationValue {get; set;}
    public int CreatedAt {get; set;}
    public int UpdatedAt {get; set;}
    public int? DeletedAt {get; set;} = null;
    public Group? Group {get => AdminUtils.GetGroup(GroupId);}
    public int GetInt {get {
        if (int.TryParse(LimitationValue, out int result))
            return result;
        throw new Exception("Value not an int");
    }}
    public string GetString {get {
        return LimitationValue;
    }}
    /// <summary>
    /// Getting from base
    /// </summary>
    public GroupLimitation(int id, int groupId, string limitationKey, string limitationValue, int createdAt, int updatedAt, int? deletedAt)
    {
        Id = id;
        GroupId = groupId;
        LimitationKey = limitationKey;
        LimitationValue = limitationValue;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        DeletedAt = deletedAt;
    }
    /// <summary>
    /// Create new
    /// </summary>
    public GroupLimitation(int groupId, string limitationKey, string limitationValue)
    {
        GroupId = groupId;
        LimitationKey = limitationKey;
        LimitationValue = limitationValue;
        CreatedAt = AdminUtils.CurrentTimestamp();
        UpdatedAt = AdminUtils.CurrentTimestamp();
    }
}