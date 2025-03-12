using System.ComponentModel.DataAnnotations.Schema;
using ArWoh.API.Enums;

namespace ArWoh.API.Entities;

public class PaymentTransaction : BaseEntity
{
    public int CustomerId { get; set; }
    public int ImageId { get; set; }
    public decimal Amount { get; set; }
    public PaymentTransactionStatusEnum PaymentStatus { get; set; } = PaymentTransactionStatusEnum.PENDING;
    public bool IsPhysicalPrint { get; set; } 

    [ForeignKey("CustomerId")] public User Customer { get; set; }

    [ForeignKey("ImageId")] public Image Image { get; set; }
}