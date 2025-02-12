namespace ArWoh.API.Entities;

public class Transaction : BaseEntity
{
    public int UserId { get; set; }
    public int ImageId { get; set; }
    public decimal Amount { get; set; }
    public string PaymentStatus { get; set; } = "Pending"; // "Pending", "Completed", "Failed"
    public string VnPayTransactionId { get; set; }

    public User User { get; set; }
    public Image Image { get; set; }
}
