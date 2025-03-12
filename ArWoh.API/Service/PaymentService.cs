using ArWoh.API.Entities;
using ArWoh.API.Enums;
using ArWoh.API.Interface;
using Microsoft.AspNetCore.Mvc;
using Net.payOS;
using Net.payOS.Types;

namespace ArWoh.API.Service;

public class PaymentService : IPaymentService
{
    private readonly PayOS _payOs;
    private readonly ILoggerService _logger;
    private readonly IUnitOfWork _unitOfWork;

    private readonly ICartService _cartService;

    public PaymentService(ILoggerService logger, PayOS payOs, ICartService cartService, IUnitOfWork unitOfWork)
    {
        _logger = logger;
        _payOs = payOs;
        _cartService = cartService;
        _unitOfWork = unitOfWork;
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

        var paymentResult = await _payOs.createPaymentLink(paymentData);
        if (paymentResult == null || string.IsNullOrEmpty(paymentResult.checkoutUrl))
            throw new Exception("Không thể tạo link thanh toán!");

        // 6. Cập nhật thông tin thanh toán trong Payment
        payment.GatewayTransactionId = paymentResult.orderCode.ToString();
        await _unitOfWork.CompleteAsync();

        return paymentResult.checkoutUrl;
    }

    public async Task<IEnumerable<PaymentTransaction>> GetUserTransactions(int userId)
    {
        try
        {
            if (userId <= 0)
                throw new ArgumentException("Invalid user ID");

            var transactions = await _unitOfWork.Transactions.FindAsync(t => t.CustomerId == userId);

            if (transactions == null || !transactions.Any())
                throw new KeyNotFoundException("No transactions found for this user");

            return transactions;
        }
        catch (Exception ex)
        {
            // Log lỗi tại đây nếu có hệ thống logging
            throw new Exception($"Error retrieving user transactions: {ex.Message}", ex);
        }
    }

    public async Task<IEnumerable<PaymentTransaction>> GetAllTransactions()
    {
        try
        {
            var transactions = await _unitOfWork.Transactions.GetAllAsync();

            if (transactions == null || !transactions.Any())
                throw new KeyNotFoundException("No transactions found");

            return transactions;
        }
        catch (Exception ex)
        {
            throw new Exception($"Error retrieving all transactions: {ex.Message}", ex);
        }
    }

    public async Task<decimal> GetPhotographerRevenue(int photographerId)
    {
        try
        {
            if (photographerId <= 0)
                throw new ArgumentException("Invalid photographer ID");

            var images = await _unitOfWork.Images.FindAsync(i => i.PhotographerId == photographerId);
            if (images == null || !images.Any())
                throw new KeyNotFoundException("No images found for this photographer");

            var imageIds = images.Select(i => i.Id).ToList();
            var transactions = await _unitOfWork.Transactions.FindAsync(t => imageIds.Contains(t.ImageId));

            if (transactions == null || !transactions.Any())
                return 0; // Không có giao dịch nào, doanh thu là 0

            return transactions.Sum(t => t.Amount);
        }
        catch (Exception ex)
        {
            throw new Exception($"Error retrieving photographer revenue: {ex.Message}", ex);
        }
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

            // (Thêm skibidi dab dab) Tìm UserId thông qua PaymentTransaction
            var paymentTransaction = await _unitOfWork.Transactions.FirstOrDefaultAsync(t => t.Id == payment.PaymentTransactionId);
            if (paymentTransaction == null)
            {
                _logger.Warn($"Không tìm thấy PaymentTransaction với ID: {payment.PaymentTransactionId}");
                return new NotFoundObjectResult("PaymentTransaction not found");
            }
            int userId = paymentTransaction.CustomerId;

            // 3. Xử lý trạng thái thanh toán dựa trên statusCode từ PayOS
            switch (statusCode)
            {
                case "00": // Thanh toán thành công
                    payment.PaymentStatus = PaymentStatusEnum.COMPLETED;
                    payment.GatewayTransactionId = transactionId;

                    //(Thêm skibidi dab dab)
                    _logger.Info($"Resetting cart for user {userId} after payment.");
                    await _cartService.ResetCartAfterPayment(userId);

                    _logger.Success($"Cart reset successfully for user {userId}.");
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
}