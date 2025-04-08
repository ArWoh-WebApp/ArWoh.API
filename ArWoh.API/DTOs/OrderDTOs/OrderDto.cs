namespace ArWoh.API.DTOs.OrderDTOs;

public class OrderDto
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; }
    public bool IsPhysicalPrint { get; set; }
    public string? ShippingAddress { get; set; }
    public string? ShippingStatus { get; set; }
    public decimal? ShippingFee { get; set; }
    
    // Thông tin ghi chú vận chuyển
    public string? ConfirmNote { get; set; }
    public string? PackagingNote { get; set; }
    public string? ShippingNote { get; set; }
    public string? DeliveryNote { get; set; }
    public string? DeliveryProofImageUrl { get; set; }

    // Các thông tin liên kết
    public List<OrderDetailDto> OrderDetails { get; set; } = new List<OrderDetailDto>();
    public PaymentInfoDto PaymentInfo { get; set; }
    public UserBasicInfoDto Customer { get; set; }
    
    // Thông tin bổ sung
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}