using ArWoh.API.DTOs.ShippingDTOs;

namespace ArWoh.API.Interface;

public interface IShippingService
{
    Task<IEnumerable<ShippableImageDto>> GetShippableImagesByUserId(int userId);
}