using ArWoh.API.DTOs.ShippingDTOs;
using ArWoh.API.Enums;

namespace ArWoh.API.Interface;

public interface IShippingService
{
    Task<ShippingOrderDto> CreateShippingOrder(ShippingOrderCreateDto orderDto, int userId);
    Task<IEnumerable<ShippingOrderDto>> GetUserShippingOrders(int userId);
    Task<IEnumerable<ShippableImageDto>> GetShippableImagesByUserId(int userId);
    Task<ShippingOrderDto> GetShippingOrderById(int orderId, int userId);

    Task<IEnumerable<ShippingOrderDto>> GetAllShippingOrders();

    Task<ShippingOrderDto> UpdateShippingOrderStatus(int orderId, ShippingStatusEnum newStatus,
        string note);

    Task<ShippingOrderDto> UploadDeliveryProofImage(int orderId, IFormFile image);
}