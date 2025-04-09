using ArWoh.API.Enums;

namespace ArWoh.API.DTOs.OrderDTOs;

public class CreateOrderDto
{
    public bool IsPhysicalPrint { get; set; }
    public string? ShippingAddress { get; set; }
    public decimal? ShippingFee { get; set; }
    public PaymentGatewayEnum PaymentGateway { get; set; }
    public string RedirectUrl { get; set; }
}