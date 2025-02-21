namespace ArWoh.API.Entities;

public class Transaction : BaseEntity
{
    public int CustomerId { get; set; }
    public int ImageId { get; set; }
    public decimal Amount { get; set; }
    public string PaymentStatus { get; set; } = "Pending"; // "Pending", "Completed", "Failed"
    public string VnPayTransactionId { get; set; }
    public bool IsPhysicalPrint { get; set; } // Đánh dấu nếu là ảnh in

    public User Customer { get; set; }
    public Image Image { get; set; }
}
