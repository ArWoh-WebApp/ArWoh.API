using ArWoh.API.DTOs.OrderDTOs;

namespace ArWoh.API.Interface;

public interface IPaymentService
{
    /// <summary>
    /// Tạo đơn hàng từ giỏ hàng và tạo link thanh toán
    /// </summary>
    Task<string> ProcessPayment(int userId, CreateOrderDto createOrderDto);
    
    
    // ProcessWebhook(webhookData) - Xử lý dữ liệu nhận từ webhook, cập nhật trạng thái thanh toán
    // GetPaymentStatus(paymentId) - Lấy và cập nhật trạng thái thanh toán hiện tại
    // CancelPayment(paymentId, reason) - Hủy thanh toán
    // GetPaymentsByOrderId(orderId) - Lấy lịch sử thanh toán của đơn hàng
}