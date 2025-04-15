using ArWoh.API.Enums;

namespace ArWoh.API.DTOs.ShippingDTOs;

public class ShippingOrderFilterDto
{
    public ShippingStatusEnum? ShippingStatus { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
}