using ArWoh.API.Entities;
using ArWoh.API.Enums;
using ArWoh.API.Interface;
using Microsoft.AspNetCore.Mvc;
using Net.payOS;
using Net.payOS.Types;

namespace ArWoh.API.Service;

public class PaymentService : IPaymentService
{
    private readonly PayOS _payOS;
    private readonly ILoggerService _logger;
    private readonly IUnitOfWork _unitOfWork;

    private readonly ICartService _cartService;

    public PaymentService(ILoggerService logger, PayOS payOs, ICartService cartService, IUnitOfWork unitOfWork)
    {
        _logger = logger;
        _payOS = payOs;
        _cartService = cartService;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Xử lý Webhook từ PayOS
    /// </summary>
    public async Task<IActionResult> PaymentWebhook([FromBody] WebhookData webhookData)
    {
        try
        {
            _logger.Info($"Nhận webhook từ PayOS {webhookData}");

            // 1. Lấy thông tin đơn hàng từ webhook
            var orderCode = webhookData.orderCode; // ID của Payment trong hệ thống
            var statusCode = webhookData.code; // Mã trạng thái giao dịch từ PayOS
            var transactionId = webhookData.reference; // Mã giao dịch của PayOS

            // 2. Tìm Payment tương ứng trong database
            var payment = await _unitOfWork.Payments.FirstOrDefaultAsync(p => p.Id == orderCode);
            if (payment == null)
            {
                _logger.Warn($"Không tìm thấy Payment với orderCode: {orderCode}");
                return new NotFoundObjectResult("Payment not found");
            }

            // 3. Xử lý trạng thái thanh toán dựa trên statusCode từ PayOS
            switch (statusCode)
            {
                case "00": // Thanh toán thành công
                    payment.PaymentStatus = PaymentStatusEnum.COMPLETED;
                    payment.GatewayTransactionId = transactionId;
                    break;

                case "01": // Thanh toán thất bại
                    payment.PaymentStatus = PaymentStatusEnum.CANCELED;
                    break;

                case "02": // Người dùng huỷ thanh toán
                    payment.PaymentStatus = PaymentStatusEnum.CANCELED;
                    break;

                default:
                    _logger.Warn($"Trạng thái không xác định từ PayOS: {statusCode}");
                    return new BadRequestObjectResult("Unknown payment status");
            }

            await _unitOfWork.CompleteAsync();
            _logger.Success($"Cập nhật trạng thái thanh toán thành công: OrderCode {orderCode}, Status {statusCode}");

            return new OkObjectResult(new { success = true, message = "Webhook processed successfully" });
        }
        catch (Exception ex)
        {
            _logger.Error($"Lỗi khi xử lý webhook từ PayOS: {ex.Message}");
            return new StatusCodeResult(500);
        }
    }

    public async Task<string> ProcessPayment(int userId)
    {
        // 1. Lấy giỏ hàng của người dùng
        var cart = await _cartService.GetCartByUserId(userId);
        if (cart == null || !cart.CartItems.Any())
            throw new Exception("Giỏ hàng trống!");

        // 2. Tạo Payment trước
        var payment = new Payment
        {
            Amount = cart.TotalPrice,
            PaymentGateway = PaymentGatewayEnum.PAYOS,
            PaymentStatus = PaymentStatusEnum.PENDING
        };

        await _unitOfWork.Payments.AddAsync(payment);
        await _unitOfWork.CompleteAsync(); // Lưu Payment để có ID

        // 3. Tạo danh sách PaymentTransaction
        var transactions = new List<PaymentTransaction>();
        var itemList = new List<ItemData>();

        foreach (var item in cart.CartItems)
        {
            var transaction = new PaymentTransaction
            {
                CustomerId = userId,
                ImageId = item.ImageId,
                Amount = item.Price * item.Quantity,
                PaymentStatus = PaymentTransactionStatusEnum.PENDING
            };
            transactions.Add(transaction);

            itemList.Add(new ItemData(
                item.ImageTitle ?? "Ảnh không có tiêu đề",
                item.Quantity,
                (int)item.Price // PayOS xử lý đơn vị là VND * 100
            ));
        }

        await _unitOfWork.Transactions.AddRangeAsync(transactions);
        await _unitOfWork.CompleteAsync();

        // 4. Cập nhật PaymentTransactionId trong Payment
        payment.PaymentTransactionId = transactions.First().Id; // Lấy transaction đầu tiên
        await _unitOfWork.CompleteAsync(); // Lưu cập nhật

        // 5. Gọi PayOS API để tạo link thanh toán
        var paymentData = new PaymentData(
            payment.Id,
            (int)cart.TotalPrice,
            "image payment",
            itemList,
            returnUrl: "https://arwoh-fe.vercel.app/",
            cancelUrl: "https://arwoh.ae-tao-fullstack-api.site/"
        );

        var paymentResult = await _payOS.createPaymentLink(paymentData);
        if (paymentResult == null || string.IsNullOrEmpty(paymentResult.checkoutUrl))
            throw new Exception("Không thể tạo link thanh toán!");

        // 6. Cập nhật thông tin thanh toán trong Payment
        payment.GatewayTransactionId = paymentResult.orderCode.ToString();
        await _unitOfWork.CompleteAsync();

        return paymentResult.checkoutUrl;
    }
}