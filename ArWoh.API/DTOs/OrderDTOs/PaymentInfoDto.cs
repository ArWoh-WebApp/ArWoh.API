namespace ArWoh.API.DTOs.OrderDTOs;

// DTO cho thông tin thanh toán
public class PaymentInfoDto
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public decimal Amount { get; set; }
    public string PaymentGateway { get; set; }
    public string Status { get; set; }
    public string? GatewayTransactionId { get; set; }
    public string? PaymentUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
