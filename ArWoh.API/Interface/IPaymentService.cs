using ArWoh.API.DTOs.OrderDTOs;
using ArWoh.API.DTOs.PaymentDTOs;
using Net.payOS.Types;

namespace ArWoh.API.Interface;

public interface IPaymentService
{
    /// <summary>
    ///     Tạo đơn hàng từ giỏ hàng và tạo link thanh toán
    /// </summary>
    Task<string> ProcessPayment(int userId, CreateOrderDto createOrderDto);

    Task ProcessWebhook(WebhookType webhookData);
    Task<PaymentStatusDto> GetPaymentStatus(int paymentId);
    Task<bool> CancelPayment(int paymentId, string reason);
    Task<List<PaymentStatusDto>> GetPaymentsByOrderId(int orderId);
}