using ArWoh.API.Enums;

namespace ArWoh.API.Entities;

public class Transaction : BaseEntity
{
    public int CustomerId { get; set; }
    public int ImageId { get; set; }
    public decimal Amount { get; set; }
    public PaymentStatusEnum PaymentStatus { get; set; } = PaymentStatusEnum.PENDING;
    public bool IsPhysicalPrint { get; set; } // Đánh dấu nếu là ảnh in

    public User Customer { get; set; }
    public Image Image { get; set; }

    public Payment Payment { get; set; } // Thêm quan hệ với Payment
}
