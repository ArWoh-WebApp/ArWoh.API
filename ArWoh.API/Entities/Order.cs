using System.ComponentModel.DataAnnotations.Schema;
using ArWoh.API.Enums;

namespace ArWoh.API.Entities;

public class Order : BaseEntity
{
    public int CustomerId { get; set; }
    public decimal TotalAmount { get; set; }
    public OrderStatusEnum Status { get; set; } = OrderStatusEnum.Pending;
    public bool IsPhysicalPrint { get; set; }
    public string? ShippingAddress { get; set; }
    public ShippingStatusEnum? ShippingStatus { get; set; }
    public decimal? ShippingFee { get; set; }

    // Thông tin vận chuyển bổ sung
    public string? ConfirmNote { get; set; }
    public string? PackagingNote { get; set; }
    public string? ShippingNote { get; set; }
    public string? DeliveryNote { get; set; }
    public string? DeliveryProofImageUrl { get; set; }

    [ForeignKey("CustomerId")] public User Customer { get; set; }

    public ICollection<OrderDetail> OrderDetails { get; set; }
    public ICollection<Payment> Payments { get; set; }
}