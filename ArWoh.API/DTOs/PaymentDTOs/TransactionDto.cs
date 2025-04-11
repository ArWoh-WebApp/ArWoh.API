namespace ArWoh.API.DTOs.PaymentDTOs;

public class TransactionDto
{
    public int TransactionId { get; set; }
    public int OrderId { get; set; }
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public string PaymentGateway { get; set; }
    public string PaymentStatus { get; set; }
    public string GatewayTransactionId { get; set; }
    public string OrderStatus { get; set; }
    public int ItemCount { get; set; }
}