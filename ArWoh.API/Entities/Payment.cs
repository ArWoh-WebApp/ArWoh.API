using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArWoh.API.Enums;

namespace ArWoh.API.Entities;

public class  Payment : BaseEntity
{
    [Required] public int OrderId { get; set; }

    [Required] public decimal Amount { get; set; }

    [Required] public PaymentGatewayEnum PaymentGateway { get; set; } // "PAYOS" hoặc "VNPAY"

    [Required] public PaymentStatusEnum Status { get; set; } = PaymentStatusEnum.PENDING;

    public string? GatewayTransactionId { get; set; } // ID từ cổng thanh toán
    public string? GatewayResponse { get; set; } // Response JSON từ cổng thanh toán
    public string? PaymentUrl { get; set; } // URL để chuyển hướng thanh toán
    public string? RedirectUrl { get; set; } // URL callback sau khi thanh toán

    [ForeignKey("OrderId")] public Order Order { get; set; }
}