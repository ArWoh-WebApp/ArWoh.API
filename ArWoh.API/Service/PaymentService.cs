using ArWoh.API.DTOs.PaymentDTOs;
using ArWoh.API.Entities;
using ArWoh.API.Enums;
using ArWoh.API.Interface;
using Microsoft.AspNetCore.Mvc;
using Net.payOS;
using Net.payOS.Types;

namespace ArWoh.API.Service;

public class PaymentService : IPaymentService
{
    private readonly ICartService _cartService;
    private readonly ILoggerService _logger;
    private readonly PayOS _payOs;
    private readonly IUnitOfWork _unitOfWork;

    public PaymentService(ILoggerService logger, PayOS payOs, ICartService cartService, IUnitOfWork unitOfWork)
    {
        _logger = logger;
        _payOs = payOs;
        _cartService = cartService;
        _unitOfWork = unitOfWork;
    }


    public async Task<IEnumerable<PaymentTransaction>> GetUserTransactions(int userId)
    {
        try
        {
            if (userId <= 0)
                throw new ArgumentException("Invalid user ID");

            var transactions = await _unitOfWork.PaymentTransactions.FindAsync(t => t.CustomerId == userId);

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
            var transactions = await _unitOfWork.PaymentTransactions.GetAllAsync();

            if (transactions == null || !transactions.Any())
                throw new KeyNotFoundException("No transactions found");

            return transactions;
        }
        catch (Exception ex)
        {
            throw new Exception($"Error retrieving all transactions: {ex.Message}", ex);
        }
    }

    public async Task<RevenueDto> GetPhotographerRevenue(int photographerId)
    {
        _logger.Info($"Starting GetPhotographerRevenue for photographerId: {photographerId}");

        try
        {
            if (photographerId <= 0)
            {
                _logger.Warn($"Invalid photographer ID: {photographerId}");
                throw new ArgumentException("Invalid photographer ID");
            }

            // Get images by photographer
            _logger.Info($"Fetching images for photographerId: {photographerId}");
            var images = await _unitOfWork.Images.FindAsync(i => i.PhotographerId == photographerId);

            if (images == null || !images.Any())
            {
                _logger.Warn($"No images found for photographerId: {photographerId}");
                throw new KeyNotFoundException("No images found for this photographer");
            }

            _logger.Info($"Found {images.Count()} images for photographerId: {photographerId}");
            var imageIds = images.Select(i => i.Id).ToList();
            _logger.Info($"Image IDs: {string.Join(", ", imageIds)}");

            // Get transactions
            _logger.Info($"Fetching transactions for imageIds: {string.Join(", ", imageIds)}");
            var transactions = await _unitOfWork.PaymentTransactions.FindAsync(t => imageIds.Contains(t.ImageId));
            _logger.Info($"Found {transactions.Count()} total transactions");

            // Filter for completed transactions
            var completedTransactions = transactions
                .Where(t => (int)t.PaymentStatus == (int)PaymentTransactionStatusEnum.COMPLETED).ToList();
            _logger.Info($"Found {completedTransactions.Count} completed transactions");

            foreach (var tx in completedTransactions)
                _logger.Info(
                    $"Transaction ID: {tx.Id}, ImageId: {tx.ImageId}, Amount: {tx.Amount}, Status: {tx.PaymentStatus}");

            var result = new RevenueDto
            {
                TotalRevenue = completedTransactions.Sum(t => t.Amount),
                TotalImagesSold = completedTransactions.Select(t => t.ImageId).Distinct().Count()
            };

            _logger.Info($"Calculated TotalRevenue: {result.TotalRevenue}, TotalImagesSold: {result.TotalImagesSold}");

            // Dictionary for image lookup
            var imageDict = images.ToDictionary(i => i.Id);

            // Group transactions by image
            var groupedTransactions = completedTransactions
                .GroupBy(t => t.ImageId)
                .ToDictionary(g => g.Key, g => g.ToList());

            _logger.Info($"Grouped transactions by image: {groupedTransactions.Count} images have transactions");

            // Process each image with transactions
            foreach (var imageId in imageIds)
            {
                _logger.Info($"Processing ImageId: {imageId}");

                if (groupedTransactions.TryGetValue(imageId, out var imageTrans) && imageTrans.Any())
                {
                    var image = imageDict[imageId];
                    var detail = new ImageSalesDetail
                    {
                        ImageId = imageId,
                        ImageTitle = image.Title ?? "Untitled",
                        ImageUrl = image.Url ?? "",
                        SalesCount = imageTrans.Count,
                        TotalAmount = imageTrans.Sum(t => t.Amount)
                    };

                    _logger.Info(
                        $"Added detail for ImageId: {imageId}, Title: {detail.ImageTitle}, SalesCount: {detail.SalesCount}, TotalAmount: {detail.TotalAmount}");
                    result.ImageSales.Add(detail);
                }
                else
                {
                    _logger.Info($"No completed transactions found for ImageId: {imageId}");
                }
            }

            // Sort by sales count
            result.ImageSales = result.ImageSales.OrderByDescending(i => i.SalesCount).ToList();
            _logger.Success($"Successfully retrieved revenue details for photographerId: {photographerId}");

            return result;
        }
        catch (Exception ex)
        {
            _logger.Error($"Error in GetPhotographerRevenue: {ex.Message}, Stack: {ex.StackTrace}");
            throw new Exception($"Error retrieving photographer revenue details: {ex.Message}", ex);
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

        await _unitOfWork.PaymentTransactions.AddRangeAsync(transactions);
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
            returnUrl: "http://localhost:9090/api/payment/webhook",
            cancelUrl: "http://localhost:9090/api/payment/webhook"
        );

        var paymentResult = await _payOs.createPaymentLink(paymentData);
        if (paymentResult == null || string.IsNullOrEmpty(paymentResult.checkoutUrl))
            throw new Exception("Không thể tạo link thanh toán!");

        // 6. Cập nhật thông tin thanh toán trong Payment
        payment.GatewayTransactionId = paymentResult.orderCode.ToString();
        await _unitOfWork.CompleteAsync();

        return paymentResult.checkoutUrl;
    }

    public async Task<IActionResult> PaymentWebhook([FromBody] WebhookData webhookData)
    {
        try
        {
            _logger.Info($"Nhận webhook từ PayOS {webhookData}");

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

            var paymentTransaction = await _unitOfWork.PaymentTransactions.FirstOrDefaultAsync(t => t.Id == payment.PaymentTransactionId);
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

                    // Update PaymentTransaction status
                    if (payment.PaymentTransactionId.HasValue)
                    {
                        // Get the main transaction
                        var mainTransaction =
                            await _unitOfWork.PaymentTransactions.GetByIdAsync(payment.PaymentTransactionId.Value);
                        if (mainTransaction != null)
                        {
                            mainTransaction.PaymentStatus = PaymentTransactionStatusEnum.COMPLETED;
                            mainTransaction.UpdatedAt = DateTime.UtcNow;

                            // Find and update all transactions related to the same order
                            var relatedTransactions = await _unitOfWork.PaymentTransactions.FindAsync(
                                t => t.CustomerId == mainTransaction.CustomerId &&
                                     t.CreatedAt >= mainTransaction.CreatedAt.AddMinutes(-1) &&
                                     t.CreatedAt <= mainTransaction.CreatedAt.AddMinutes(1) &&
                                     t.PaymentStatus == PaymentTransactionStatusEnum.PENDING);

                            foreach (var transaction in relatedTransactions)
                            {
                                transaction.PaymentStatus = PaymentTransactionStatusEnum.COMPLETED;
                                transaction.UpdatedAt = DateTime.UtcNow;
                            }

                            _logger.Info(
                                $"Updated {relatedTransactions.Count() + 1} payment transactions to COMPLETED status");

                            // Clear the user's cart after successful payment
                            await ClearUserCart(mainTransaction.CustomerId);
                        }
                        else
                        {
                            _logger.Warn(
                                $"Không tìm thấy PaymentTransaction với ID: {payment.PaymentTransactionId.Value}");
                        }
                    }

                    break;

                case "01": // Thanh toán thất bại
                    payment.PaymentStatus = PaymentStatusEnum.CANCELED;
                    // Update related transactions to FAILED
                    await UpdateRelatedTransactions(payment, PaymentTransactionStatusEnum.FAILED);
                    break;

                case "02": // Người dùng huỷ thanh toán
                    payment.PaymentStatus = PaymentStatusEnum.CANCELED;
                    // Update related transactions to FAILED
                    await UpdateRelatedTransactions(payment, PaymentTransactionStatusEnum.FAILED);
                    break;

                default:
                    _logger.Warn($"Trạng thái không xác định từ PayOS: {statusCode}");
                    return new BadRequestObjectResult("Unknown payment status");
            }

            await _unitOfWork.CompleteAsync();
            _logger.Success(
                $"Cập nhật trạng thái thanh toán và giao dịch thành công: OrderCode {orderCode}, Status {statusCode}");

            return new OkObjectResult(new { success = true, message = "Webhook processed successfully" });
        }
        catch (Exception ex)
        {
            _logger.Error($"Lỗi khi xử lý webhook từ PayOS: {ex.Message}");
            return new StatusCodeResult(500);
        }
    }

    // Helper method to update related transactions
    private async Task UpdateRelatedTransactions(Payment payment, PaymentTransactionStatusEnum status)
    {
        if (payment.PaymentTransactionId.HasValue)
        {
            var mainTransaction =
                await _unitOfWork.PaymentTransactions.GetByIdAsync(payment.PaymentTransactionId.Value);
            if (mainTransaction != null)
            {
                mainTransaction.PaymentStatus = status;
                mainTransaction.UpdatedAt = DateTime.UtcNow;

                // Find and update all transactions related to the same order
                var relatedTransactions = await _unitOfWork.PaymentTransactions.FindAsync(
                    t => t.CustomerId == mainTransaction.CustomerId &&
                         t.CreatedAt >= mainTransaction.CreatedAt.AddMinutes(-1) &&
                         t.CreatedAt <= mainTransaction.CreatedAt.AddMinutes(1) &&
                         t.PaymentStatus == PaymentTransactionStatusEnum.PENDING);

                foreach (var transaction in relatedTransactions)
                {
                    transaction.PaymentStatus = status;
                    transaction.UpdatedAt = DateTime.UtcNow;
                }

                _logger.Info($"Updated {relatedTransactions.Count() + 1} payment transactions to {status} status");
            }
            else
            {
                _logger.Warn($"Không tìm thấy PaymentTransaction với ID: {payment.PaymentTransactionId.Value}");
            }
        }
    }

    // Helper method to clear a user's cart after successful payment
    private async Task ClearUserCart(int userId)
    {
        try
        {
            // Find the user's cart
            var cart = await _unitOfWork.Carts.FirstOrDefaultAsync(c => c.UserId == userId);
            if (cart != null)
            {
                // Find all cart items associated with this cart
                var cartItems = await _unitOfWork.CartItems.FindAsync(ci => ci.CartId == cart.Id);

                // Delete all cart items
                if (cartItems != null && cartItems.Any())
                {
                    _unitOfWork.CartItems.DeleteRange(cartItems);
                    _logger.Info($"Deleted {cartItems.Count()} cart items for user ID: {userId}");
                }

                await _unitOfWork.CompleteAsync();
                _logger.Info($"Successfully cleared cart for user ID: {userId}");
            }
            else
            {
                _logger.Info($"No active cart found for user ID: {userId}");
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Error clearing cart for user ID {userId}: {ex.Message}");
            // We don't want to throw the exception here as it would disrupt the payment process
            // Just log the error and continue
        }
    }
}