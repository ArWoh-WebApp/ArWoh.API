﻿using ArWoh.API.DTOs.OrderDTOs;
using ArWoh.API.DTOs.PaymentDTOs;
using ArWoh.API.Enums;

namespace ArWoh.API.Interface;

public interface IOrderService
{
    /// <summary>
    ///     lấy doanh thu của 1 photographer
    /// </summary>
    Task<RevenueDto> GetPhotographerRevenue(int photographerId);

    /// <summary>
    ///     Tạo đơn hàng từ giỏ hàng
    /// </summary>
    Task<OrderDto> CreateOrderFromCart(int userId, CreateOrderDto createOrderDto);

    /// <summary>
    ///     Cập nhật trạng thái đơn hàng
    /// </summary>
    Task<OrderDto> UpdateOrderStatus(int orderId, OrderStatusEnum status);

    /// <summary>
    ///     Lấy thông tin đơn hàng
    /// </summary>
    Task<OrderDto> GetOrderById(int orderId);
}