using ArWoh.API.DTOs.OrderDTOs;

namespace ArWoh.API.Interface;

public interface IOrderService
{
    Task<OrderDto> CreateOrderFromCart(CreateOrderDto createOrderDto);
}