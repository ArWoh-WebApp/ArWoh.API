using ArWoh.API.DTOs.ShipOrderDTOs;

namespace ArWoh.API.Interface;

public interface IShippingOrderService
{
    Task<IEnumerable<ShippableImageDto>> GetShippableImagesByUserId(int userId);
    Task<ShippingOrderDto> CreateShippingOrder(CreateShippingOrderDto createDto);
    Task<IEnumerable<ShippingOrderDto>> GetUserShippingOrders(int userId);
    Task<ShippingOrderDto> GetShippingOrderById(int orderId);
    Task<IEnumerable<ShippingOrderDto>> GetAllShippingOrders();
    Task<ShippingOrderDto> UpdateShippingOrderStatus(UpdateShippingStatusDto updateDto);
}