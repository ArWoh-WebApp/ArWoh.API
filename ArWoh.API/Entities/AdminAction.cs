namespace ArWoh.API.Entities;

public class AdminAction : BaseEntity
{
    public int AdminId { get; set; }
    public int? UserId { get; set; }
    public int? ImageId { get; set; }
    public string ActionType { get; set; } // "Ban User", "Remove Image", "Flag Content"
    public string Reason { get; set; }

    public User Admin { get; set; }
}