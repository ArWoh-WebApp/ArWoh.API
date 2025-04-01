using System.ComponentModel.DataAnnotations.Schema;
using ArWoh.API.Enums;

namespace ArWoh.API.Entities;

public class ShippingOrder : BaseEntity
{
    public int TransactionId { get; set; }
    public string ShippingAddress { get; set; }
    public decimal ShippingFee { get; set; }
    public ShippingStatusEnum Status { get; set; } = ShippingStatusEnum.Pending;
    public string? ConfirmNote { get; set; }
    public string? PackagingNote { get; set; }
    public string? ShippingNote { get; set; }
    public string? DeliveryNote { get; set; }
    public string? DeliveryProofImageUrl { get; set; }

    [ForeignKey("TransactionId")] public PaymentTransaction Transaction { get; set; }
}