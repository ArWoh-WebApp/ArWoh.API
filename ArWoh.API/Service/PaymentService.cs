using ArWoh.API.DTOs.EmailDTOs;
using ArWoh.API.DTOs.OrderDTOs;
using ArWoh.API.DTOs.PaymentDTOs;
using ArWoh.API.Entities;
using ArWoh.API.Enums;
using ArWoh.API.Interface;
using Microsoft.EntityFrameworkCore;
using Net.payOS;
using Net.payOS.Types;
using Newtonsoft.Json;

namespace ArWoh.API.Service;

public class PaymentService : IPaymentService
{
    private readonly IEmailService _emailService;
    private readonly ILoggerService _logger;
    private readonly IOrderService _orderService;
    private readonly PayOS _payOs;
    private readonly IUnitOfWork _unitOfWork;

    public PaymentService(ILoggerService logger, PayOS payOs, IUnitOfWork unitOfWork, IOrderService orderService,
        IEmailService emailService)
    {
        _logger = logger;
        _payOs = payOs;
        _unitOfWork = unitOfWork;
        _orderService = orderService;
        _emailService = emailService;
    }

    // Thêm tham số để lọc hoặc sắp xếp
    public async Task<List<PaymentInfoDto>> GetAllPayments(PaymentStatusEnum? status = null, DateTime? fromDate = null,
        DateTime? toDate = null)
    {
        try
        {
            // Bắt đầu với truy vấn cơ bản
            var query = _unitOfWork.Payments.GetQueryable();

            // Áp dụng các điều kiện lọc nếu được cung cấp
            if (status.HasValue) query = query.Where(p => p.Status == status.Value);

            if (fromDate.HasValue) query = query.Where(p => p.CreatedAt >= fromDate.Value);

            if (toDate.HasValue) query = query.Where(p => p.CreatedAt <= toDate.Value);

            // Sắp xếp theo thời gian tạo giảm dần (mới nhất trước)
            query = query.OrderByDescending(p => p.CreatedAt);

            // Thực thi truy vấn
            var payments = await query.ToListAsync();

            // Chuyển đổi entities thành DTOs
            var paymentDtos = payments.Select(p => new PaymentInfoDto
            {
                Id = p.Id,
                OrderId = p.OrderId,
                Amount = p.Amount,
                PaymentGateway = p.PaymentGateway.ToString(),
                Status = p.Status.ToString(),
                GatewayTransactionId = p.GatewayTransactionId,
                PaymentUrl = p.PaymentUrl,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            }).ToList();

            return paymentDtos;
        }
        catch (Exception e)
        {
            _logger.Error($"Error getting payments: {e.Message}");
            throw;
        }
    }

    public async Task<string> ProcessPayment(int userId, CreateOrderDto createOrderDto)
    {
        try
        {
            // Tạo order từ giỏ hàng
            var order = await _orderService.CreateOrderFromCart(userId, createOrderDto);

            // Tạo payment record
            var payment = new Payment
            {
                OrderId = order.Id,
                Amount = order.TotalAmount,
                PaymentGateway = PaymentGatewayEnum.PAYOS,
                Status = PaymentStatusEnum.PENDING,
                RedirectUrl = "https://arwoh.vercel.app/payment-success"
            };

            await _unitOfWork.Payments.AddAsync(payment);
            await _unitOfWork.CompleteAsync();

            // Chuẩn bị danh sách item cho PayOS
            var itemList = new List<ItemData>();
            foreach (var detail in order.OrderDetails)
                itemList.Add(new ItemData(
                    detail.ImageTitle ?? "Ảnh không có tiêu đề",
                    detail.Quantity,
                    (int)detail.Price
                ));

            // dùng ID đơn hàng kết hợp với timestamp ngắn
            var orderCode = order.Id * 1000 + DateTime.Now.Second + DateTime.Now.Minute * 60;

            // Tạo dữ liệu thanh toán
            var paymentData = new PaymentData(
                orderCode, // Sử dụng orderCode mới thay vì payment.Id
                (int)order.TotalAmount,
                $"Thanh toán đơn hàng #{order.Id}",
                itemList,
                "https://arwoh.vercel.app/payment-success",
                payment.RedirectUrl
            );

            // Gọi PayOS API để tạo link thanh toán
            var paymentResult = await _payOs.createPaymentLink(paymentData);

            // Cập nhật thông tin thanh toán
            payment.GatewayTransactionId = paymentResult.orderCode.ToString();
            payment.PaymentUrl = paymentResult.checkoutUrl;
            payment.GatewayResponse = JsonConvert.SerializeObject(paymentResult);
            await _unitOfWork.CompleteAsync();

            _logger.Info($"Payment link created for order {order.Id}: {paymentResult.checkoutUrl}");

            return paymentResult.checkoutUrl;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to process payment: {ex.Message}");
            throw new Exception("Không thể tạo link thanh toán. Vui lòng thử lại sau.", ex);
        }
    }

    public async Task ProcessWebhook(WebhookType webhookData)
    {
        try
        {
            // Xác thực webhook data
            var data = _payOs.verifyPaymentWebhookData(webhookData);

            // Tìm payment record thông qua GatewayTransactionId
            var payment = await _unitOfWork.Payments.FirstOrDefaultAsync(p =>
                p.GatewayTransactionId == data.orderCode.ToString());

            if (payment == null)
            {
                _logger.Error($"Payment not found for orderCode: {data.orderCode}");
                return;
            }

            // Cập nhật trạng thái payment
            payment.Status = PaymentStatusEnum.COMPLETED;
            payment.GatewayResponse = JsonConvert.SerializeObject(data);
            _unitOfWork.Payments.Update(payment);

            // Cập nhật trạng thái order
            await _orderService.UpdateOrderStatus(payment.OrderId, OrderStatusEnum.Completed);

            // Get order with customer details for email
            var order = await _unitOfWork.Orders.GetByIdAsync(
                payment.OrderId,
                o => o.Customer
            );

            if (order != null && order.Customer != null)
            {
                // Create email request with customer info
                var emailRequest = new EmailRequestDTO
                {
                    UserEmail = order.Customer.Email,
                    UserName = order.Customer.Username ?? "Valued Customer"
                };

                // Send email with purchased images
                await _emailService.SendPurchasedImagesEmailAsync(emailRequest, payment.OrderId);
            }
            else
            {
                _logger.Error($"Could not send email for order {payment.OrderId} - missing customer data");
            }

            _logger.Info($"Payment successful for order {payment.OrderId}");
        }
        catch (Exception ex)
        {
            _logger.Error($"Error processing webhook: {ex.Message}");
        }
    }

    public async Task<PaymentStatusDto> GetPaymentStatus(int paymentId)
    {
        var payment = await _unitOfWork.Payments.GetByIdAsync(paymentId);
        if (payment == null)
            throw new Exception("Payment not found");

        // Nếu payment chưa hoàn thành, kiểm tra với PayOS
        if (payment.Status == PaymentStatusEnum.PENDING)
            try
            {
                var paymentInfo = await _payOs.getPaymentLinkInformation(long.Parse(payment.GatewayTransactionId));

                // Cập nhật trạng thái nếu đã thay đổi
                if (paymentInfo.status == "PAID" && payment.Status != PaymentStatusEnum.COMPLETED)
                {
                    payment.Status = PaymentStatusEnum.COMPLETED;
                    _unitOfWork.Payments.Update(payment);

                    await _orderService.UpdateOrderStatus(payment.OrderId, OrderStatusEnum.Completed);

                    // Get order with customer details for email
                    var order = await _unitOfWork.Orders.GetByIdAsync(
                        payment.OrderId,
                        o => o.Customer
                    );

                    if (order != null && order.Customer != null)
                    {
                        // Create email request with customer info
                        var emailRequest = new EmailRequestDTO
                        {
                            UserEmail = order.Customer.Email,
                            UserName = order.Customer.Username ?? "Valued Customer"
                        };

                        // Send email with purchased images
                        await _emailService.SendPurchasedImagesEmailAsync(emailRequest, payment.OrderId);
                    }
                    else
                    {
                        _logger.Error($"Could not send email for order {payment.OrderId} - missing customer data");
                    }

                    await _unitOfWork.CompleteAsync();
                }
                else if (paymentInfo.status == "CANCELLED" && payment.Status != PaymentStatusEnum.CANCELLED)
                {
                    payment.Status = PaymentStatusEnum.CANCELLED;
                    _unitOfWork.Payments.Update(payment);

                    await _orderService.UpdateOrderStatus(payment.OrderId, OrderStatusEnum.Cancelled);
                    await _unitOfWork.CompleteAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error checking payment status: {ex.Message}");
            }

        return new PaymentStatusDto
        {
            PaymentId = payment.Id,
            OrderId = payment.OrderId,
            Status = payment.Status.ToString(),
            PaymentUrl = payment.PaymentUrl,
            Amount = payment.Amount,
            CreatedAt = payment.CreatedAt,
            UpdatedAt = payment.UpdatedAt
        };
    }

    public async Task<bool> CancelPayment(int paymentId, string reason)
    {
        var payment = await _unitOfWork.Payments.GetByIdAsync(paymentId);
        if (payment == null)
            throw new Exception("Payment not found");

        if (payment.Status != PaymentStatusEnum.PENDING)
            throw new Exception("Only pending payments can be cancelled");

        try
        {
            var result = await _payOs.cancelPaymentLink(long.Parse(payment.GatewayTransactionId), reason);

            // Cập nhật trạng thái payment
            payment.Status = PaymentStatusEnum.CANCELLED;
            payment.GatewayResponse = JsonConvert.SerializeObject(result);
            _unitOfWork.Payments.Update(payment);

            // Cập nhật trạng thái đơn hàng
            await _orderService.UpdateOrderStatus(payment.OrderId, OrderStatusEnum.Cancelled);
            await _unitOfWork.CompleteAsync();

            _logger.Info($"Payment {paymentId} cancelled successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to cancel payment: {ex.Message}");
            throw new Exception("Cannot cancel payment", ex);
        }
    }

    public async Task<List<PaymentStatusDto>> GetPaymentsByOrderId(int orderId)
    {
        var payments = await _unitOfWork.Payments.FindAsync(p => p.OrderId == orderId);

        return payments.Select(p => new PaymentStatusDto
        {
            PaymentId = p.Id,
            OrderId = p.OrderId,
            Status = p.Status.ToString(),
            PaymentUrl = p.PaymentUrl,
            Amount = p.Amount,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt
        }).ToList();
    }
}