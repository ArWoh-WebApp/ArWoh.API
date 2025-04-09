namespace ArWoh.API.DTOs.PaymentDTOs;

public class PaymentStatusDto
{
    public int PaymentId { get; set; }
    public int OrderId { get; set; }
    public string Status { get; set; }
    public string PaymentUrl { get; set; }
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}