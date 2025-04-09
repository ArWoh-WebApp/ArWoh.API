using ArWoh.API.DTOs.OrderDTOs;
using ArWoh.API.Entities;
using ArWoh.API.Enums;
using ArWoh.API.Interface;
using Net.payOS;
using Net.payOS.Types;
using Newtonsoft.Json;

namespace ArWoh.API.Service;

public class PaymentService : IPaymentService
{
    private readonly ILoggerService _logger;
    private readonly PayOS _payOs;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOrderService _orderService;
    private readonly IClaimService _claimService;

    public PaymentService(ILoggerService logger, PayOS payOs, IUnitOfWork unitOfWork, IOrderService orderService, IClaimService claimService)
    {
        _logger = logger;
        _payOs = payOs;
        _unitOfWork = unitOfWork;
        _orderService = orderService;
        _claimService = claimService;
    }


    public async Task<string> ProcessPayment(int userId, CreateOrderDto createOrderDto)
    {
        try
        {
            // Tạo order từ giỏ hàng
            var order = await _orderService.CreateOrderFromCart(createOrderDto);

            // Tạo payment record
            var payment = new Payment
            {
                OrderId = order.Id,
                Amount = order.TotalAmount,
                PaymentGateway = PaymentGatewayEnum.PAYOS,
                Status = PaymentStatusEnum.PENDING,
                RedirectUrl = createOrderDto.RedirectUrl ?? "http://localhost:9090/payment/result"
            };

            await _unitOfWork.Payments.AddAsync(payment);
            await _unitOfWork.CompleteAsync();

            // Chuẩn bị danh sách item cho PayOS
            var itemList = new List<ItemData>();
            foreach (var detail in order.OrderDetails)
            {
                itemList.Add(new ItemData(
                    detail.ImageTitle ?? "Ảnh không có tiêu đề",
                    detail.Quantity,
                    (int)detail.Price
                ));
            }

            // Tạo dữ liệu thanh toán
            var paymentData = new PaymentData(
                payment.Id, // Sử dụng payment ID làm orderCode
                (int)order.TotalAmount,
                $"Thanh toán đơn hàng #{order.Id}",
                itemList,
                cancelUrl: "http://localhost:9090/payment/cancel",
                returnUrl: payment.RedirectUrl
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
        try {
            // Xác thực webhook data
            var data = _payOs.verifyPaymentWebhookData(webhookData);
        
            // Tìm payment record
            var payment = await _unitOfWork.Payments.FirstOrDefaultAsync(p => 
                p.GatewayTransactionId == data.orderCode.ToString());
        
            if (payment == null) {
                _logger.Error($"Payment not found for orderCode: {data.orderCode}");
                return;
            }
        
            // Cập nhật trạng thái payment
            payment.Status = PaymentStatusEnum.COMPLETED;
            payment.GatewayResponse = JsonConvert.SerializeObject(data);
        
            // Cập nhật trạng thái order
            var order = await _unitOfWork.Orders.GetByIdAsync(payment.OrderId);
            order.Status = OrderStatusEnum.Completed;
        
            await _unitOfWork.CompleteAsync();
        
            _logger.Info($"Payment successful for order {order.Id}");
        }
        catch (Exception ex) {
            _logger.Error($"Error processing webhook: {ex.Message}");
        }
    }
    
    public async Task<PaymentStatusDto> GetPaymentStatus(int paymentId)
    {
        var payment = await _unitOfWork.Payments.GetByIdAsync(paymentId);
        if (payment == null)
            throw new NotFoundException("Payment not found");
        
        // Nếu payment chưa hoàn thành, kiểm tra với PayOS
        if (payment.Status == PaymentStatusEnum.PENDING)
        {
            try {
                var paymentInfo = await _payOs.getPaymentLinkInformation(long.Parse(payment.GatewayTransactionId));
            
                // Cập nhật trạng thái nếu đã thanh toán
                if (paymentInfo.status == "PAID")
                {
                    payment.Status = PaymentStatusEnum.COMPLETED;
                
                    var order = await _unitOfWork.Orders.GetByIdAsync(payment.OrderId);
                    order.Status = OrderStatusEnum.Completed;
                
                    await _unitOfWork.CompleteAsync();
                }
            }
            catch (Exception ex) {
                _logger.Error($"Error checking payment status: {ex.Message}");
            }
        }
    
        return new PaymentStatusDto
        {
            PaymentId = payment.Id,
            OrderId = payment.OrderId,
            Status = payment.Status,
            PaymentUrl = payment.PaymentUrl
        };
    }

    // public async Task<string> ProcessPayment(int userId)
    // {
    //     // PHASE 1: KHỞI TẠO VÀ KIỂM TRA GIỎ HÀNG
    //     // Lấy thông tin giỏ hàng của người dùng và kiểm tra tính hợp lệ
    //     var cart = await _cartService.GetCartByUserId(userId);
    //     if (cart == null || !cart.CartItems.Any())
    //         throw new Exception("Giỏ hàng trống!");
    //
    //     // PHASE 2: TẠO PAYMENT RECORD
    //     // Tạo bản ghi Payment với trạng thái chờ thanh toán
    //     var payment = new Payment
    //     {
    //         Amount = cart.TotalPrice,
    //         PaymentGateway = PaymentGatewayEnum.PAYOS,
    //         PaymentStatus = PaymentStatusEnum.PENDING
    //     };
    //
    //     await _unitOfWork.Payments.AddAsync(payment);
    //     await _unitOfWork.CompleteAsync(); // Lưu Payment để có ID
    //
    //     // PHASE 3: TẠO CÁC PAYMENT TRANSACTION
    //     // Tạo các bản ghi giao dịch cho từng mục trong giỏ hàng
    //     var transactions = new List<PaymentTransaction>();
    //     var itemList = new List<ItemData>();
    //
    //     foreach (var item in cart.CartItems)
    //     {
    //         var transaction = new PaymentTransaction
    //         {
    //             CustomerId = userId,
    //             ImageId = item.ImageId,
    //             Amount = item.Price * item.Quantity,
    //             PaymentStatus = PaymentTransactionStatusEnum.PENDING
    //         };
    //         transactions.Add(transaction);
    //
    //         itemList.Add(new ItemData(
    //             item.ImageTitle ?? "Ảnh không có tiêu đề",
    //             item.Quantity,
    //             (int)item.Price // PayOS xử lý đơn vị là VND * 100
    //         ));
    //     }
    //
    //     await _unitOfWork.PaymentTransactions.AddRangeAsync(transactions);
    //     await _unitOfWork.CompleteAsync();
    //
    //     // PHASE 4: CẬP NHẬT PAYMENT VỚI TRANSACTION_ID
    //     // Liên kết PaymentTransaction với Payment
    //     payment.PaymentTransactionId = transactions.First().Id; // Lấy transaction đầu tiên
    //     await _unitOfWork.CompleteAsync(); // Lưu cập nhật
    //
    //     // PHASE 5: KẾT NỐI VỚI PAYMENT GATEWAY
    //     // Gọi API của PayOS để tạo link thanh toán
    //     var paymentData = new PaymentData(
    //         payment.Id,
    //         (int)cart.TotalPrice,
    //         "image payment",
    //         itemList,
    //         returnUrl: "http://localhost:9090/api/payment/webhook",
    //         cancelUrl: "http://localhost:9090/api/payment/webhook"
    //     );
    //
    //     var paymentResult = await _payOs.createPaymentLink(paymentData);
    //     if (paymentResult == null || string.IsNullOrEmpty(paymentResult.checkoutUrl))
    //         throw new Exception("Không thể tạo link thanh toán!");
    //
    //     // PHASE 6: HOÀN TẤT QUY TRÌNH
    //     // Cập nhật thông tin từ payment gateway và lưu vào database
    //     payment.GatewayTransactionId = paymentResult.orderCode.ToString();
    //     await _unitOfWork.CompleteAsync();
    //
    //     return paymentResult.checkoutUrl;
    // }
    //
    // public async Task<IActionResult> PaymentWebhook([FromBody] WebhookData webhookData)
    // {
    //     try
    //     {
    //         _logger.Info($"Nhận webhook từ PayOS {webhookData}");
    //
    //         var orderCode = webhookData.orderCode;
    //         var statusCode = webhookData.code;
    //         var transactionId = webhookData.reference;
    //
    //         // 2. Tìm Payment tương ứng trong database
    //         var payment = await _unitOfWork.Payments.FirstOrDefaultAsync(p => p.Id == orderCode);
    //         if (payment == null)
    //         {
    //             _logger.Warn($"Không tìm thấy Payment với orderCode: {orderCode}");
    //             return new NotFoundObjectResult("Payment not found");
    //         }
    //
    //         var paymentTransaction =
    //             await _unitOfWork.PaymentTransactions.FirstOrDefaultAsync(t => t.Id == payment.PaymentTransactionId);
    //         if (paymentTransaction == null)
    //         {
    //             _logger.Warn($"Không tìm thấy PaymentTransaction với ID: {payment.PaymentTransactionId}");
    //             return new NotFoundObjectResult("PaymentTransaction not found");
    //         }
    //
    //         var userId = paymentTransaction.CustomerId;
    //
    //         // 3. Xử lý trạng thái thanh toán dựa trên statusCode từ PayOS
    //         switch (statusCode)
    //         {
    //             case "00": // Thanh toán thành công
    //                 payment.PaymentStatus = PaymentStatusEnum.COMPLETED;
    //                 payment.GatewayTransactionId = transactionId;
    //
    //                 //(Thêm skibidi dab dab)
    //                 _logger.Info($"Resetting cart for user {userId} after payment.");
    //                 await _cartService.ResetCartAfterPayment(userId);
    //
    //                 _logger.Success($"Cart reset successfully for user {userId}.");
    //
    //                 // Update PaymentTransaction status
    //                 if (payment.PaymentTransactionId.HasValue)
    //                 {
    //                     // Get the main transaction
    //                     var mainTransaction =
    //                         await _unitOfWork.PaymentTransactions.GetByIdAsync(payment.PaymentTransactionId.Value);
    //                     if (mainTransaction != null)
    //                     {
    //                         mainTransaction.PaymentStatus = PaymentTransactionStatusEnum.COMPLETED;
    //                         mainTransaction.UpdatedAt = DateTime.UtcNow;
    //
    //                         // Find and update all transactions related to the same order
    //                         var relatedTransactions = await _unitOfWork.PaymentTransactions.FindAsync(
    //                             t => t.CustomerId == mainTransaction.CustomerId &&
    //                                  t.CreatedAt >= mainTransaction.CreatedAt.AddMinutes(-1) &&
    //                                  t.CreatedAt <= mainTransaction.CreatedAt.AddMinutes(1) &&
    //                                  t.PaymentStatus == PaymentTransactionStatusEnum.PENDING);
    //
    //                         foreach (var transaction in relatedTransactions)
    //                         {
    //                             transaction.PaymentStatus = PaymentTransactionStatusEnum.COMPLETED;
    //                             transaction.UpdatedAt = DateTime.UtcNow;
    //                         }
    //
    //                         _logger.Info(
    //                             $"Updated {relatedTransactions.Count() + 1} payment transactions to COMPLETED status");
    //
    //                         // Clear the user's cart after successful payment
    //                         await ClearUserCart(mainTransaction.CustomerId);
    //                     }
    //                     else
    //                     {
    //                         _logger.Warn(
    //                             $"Không tìm thấy PaymentTransaction với ID: {payment.PaymentTransactionId.Value}");
    //                     }
    //                 }
    //
    //                 break;
    //
    //             case "01": // Thanh toán thất bại
    //                 payment.PaymentStatus = PaymentStatusEnum.CANCELED;
    //                 // Update related transactions to FAILED
    //                 await UpdateRelatedTransactions(payment, PaymentTransactionStatusEnum.FAILED);
    //                 break;
    //
    //             case "02": // Người dùng huỷ thanh toán
    //                 payment.PaymentStatus = PaymentStatusEnum.CANCELED;
    //                 // Update related transactions to FAILED
    //                 await UpdateRelatedTransactions(payment, PaymentTransactionStatusEnum.FAILED);
    //                 break;
    //
    //             default:
    //                 _logger.Warn($"Trạng thái không xác định từ PayOS: {statusCode}");
    //                 return new BadRequestObjectResult("Unknown payment status");
    //         }
    //
    //         await _unitOfWork.CompleteAsync();
    //         _logger.Success(
    //             $"Cập nhật trạng thái thanh toán và giao dịch thành công: OrderCode {orderCode}, Status {statusCode}");
    //
    //         return new OkObjectResult(new { success = true, message = "Webhook processed successfully" });
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.Error($"Lỗi khi xử lý webhook từ PayOS: {ex.Message}");
    //         return new StatusCodeResult(500);
    //     }
    // }
    //
    // // Helper method to update related transactions
    // private async Task UpdateRelatedTransactions(Payment payment, PaymentTransactionStatusEnum status)
    // {
    //     if (payment.PaymentTransactionId.HasValue)
    //     {
    //         var mainTransaction =
    //             await _unitOfWork.PaymentTransactions.GetByIdAsync(payment.PaymentTransactionId.Value);
    //         if (mainTransaction != null)
    //         {
    //             mainTransaction.PaymentStatus = status;
    //             mainTransaction.UpdatedAt = DateTime.UtcNow;
    //
    //             // Find and update all transactions related to the same order
    //             var relatedTransactions = await _unitOfWork.PaymentTransactions.FindAsync(
    //                 t => t.CustomerId == mainTransaction.CustomerId &&
    //                      t.CreatedAt >= mainTransaction.CreatedAt.AddMinutes(-1) &&
    //                      t.CreatedAt <= mainTransaction.CreatedAt.AddMinutes(1) &&
    //                      t.PaymentStatus == PaymentTransactionStatusEnum.PENDING);
    //
    //             foreach (var transaction in relatedTransactions)
    //             {
    //                 transaction.PaymentStatus = status;
    //                 transaction.UpdatedAt = DateTime.UtcNow;
    //             }
    //
    //             _logger.Info($"Updated {relatedTransactions.Count() + 1} payment transactions to {status} status");
    //         }
    //         else
    //         {
    //             _logger.Warn($"Không tìm thấy PaymentTransaction với ID: {payment.PaymentTransactionId.Value}");
    //         }
    //     }
    // }
    //
    // // Helper method to clear a user's cart after successful payment
    // private async Task ClearUserCart(int userId)
    // {
    //     try
    //     {
    //         // Find the user's cart
    //         var cart = await _unitOfWork.Carts.FirstOrDefaultAsync(c => c.UserId == userId);
    //         if (cart != null)
    //         {
    //             // Find all cart items associated with this cart
    //             var cartItems = await _unitOfWork.CartItems.FindAsync(ci => ci.CartId == cart.Id);
    //
    //             // Delete all cart items
    //             if (cartItems != null && cartItems.Any())
    //             {
    //                 _unitOfWork.CartItems.DeleteRange(cartItems);
    //                 _logger.Info($"Deleted {cartItems.Count()} cart items for user ID: {userId}");
    //             }
    //
    //             await _unitOfWork.CompleteAsync();
    //             _logger.Info($"Successfully cleared cart for user ID: {userId}");
    //         }
    //         else
    //         {
    //             _logger.Info($"No active cart found for user ID: {userId}");
    //         }
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.Error($"Error clearing cart for user ID {userId}: {ex.Message}");
    //         // We don't want to throw the exception here as it would disrupt the payment process
    //         // Just log the error and continue
    //     }
    // }
}