using ArWoh.API.Enums;

namespace ArWoh.API.DTOs.ShipOrderDTOs;

// DTOs/ShippingDTOs/ShippableImageDto.cs
public class ShippableImageDto
{
    public int Id { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string? Url { get; set; }
    public int TransactionId { get; set; }
}

// DTOs/ShippingDTOs/CreateShippingOrderDto.cs
public class CreateShippingOrderDto
{
    public int TransactionId { get; set; }
    public string ShippingAddress { get; set; }
}

// DTOs/ShippingDTOs/ShippingOrderDto.cs
public class ShippingOrderDto
{
    public int Id { get; set; }
    public int TransactionId { get; set; }
    public int ImageId { get; set; }
    public string? ImageTitle { get; set; }
    public string? ImageUrl { get; set; }
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerEmail { get; set; }
    public string ShippingAddress { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal OrderAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public ShippingStatusEnum Status { get; set; }
    public string? ConfirmNote { get; set; }
    public string? PackagingNote { get; set; }
    public string? ShippingNote { get; set; }
    public string? DeliveryNote { get; set; }
    public string? DeliveryProofImageUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

// DTOs/ShippingDTOs/UpdateShippingStatusDto.cs
public class UpdateShippingStatusDto
{
    public int OrderId { get; set; }
    public ShippingStatusEnum Status { get; set; }
    public string? Note { get; set; }
    public IFormFile? DeliveryProofImage { get; set; }
}