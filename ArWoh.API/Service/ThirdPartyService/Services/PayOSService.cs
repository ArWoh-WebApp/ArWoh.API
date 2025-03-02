using ArWoh.API.Entities;
using ArWoh.API.Enums;
using ArWoh.API.Interface;
using ArWoh.API.Service.ThirdPartyService.Interfaces;
using ArWoh.API.Service.ThirdPartyService.Types;
using Microsoft.EntityFrameworkCore;
using Net.payOS;
using Net.payOS.Types;
using Newtonsoft.Json;
using PaymentTransaction = ArWoh.API.Entities.PaymentTransaction;

namespace ArWoh.API.Service.ThirdPartyService.Services;

public class PayOSService : IPayOSService
{
    private readonly ArWohDbContext _context;
    private readonly ILoggerService _logger;
    private readonly PayOS _payOS;

    public PayOSService(ILoggerService logger, ArWohDbContext context, PayOS payOS)
    {
        _logger = logger;
        _payOS = payOS;
        _context = context;
    }

    // Tạo Payment Link
    public async Task<CreatePaymentResponse> CreatePaymentLink(CreatePaymentRequest request)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var cart = _context.Carts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Image)
                .FirstOrDefault(c => c.UserId == request.UserId);


            if (cart == null || !cart.CartItems.Any())
                throw new Exception("Cart is empty");


            // 2. Tạo PaymentTransaction cho từng ảnh trong giỏ
            var transactions = new List<PaymentTransaction>();
            foreach (var item in cart.CartItems)
            {
                var transactionEntity = new PaymentTransaction
                {
                    CustomerId = cart.UserId,
                    ImageId = item.ImageId,
                    Amount = item.Price * item.Quantity, // Tính tổng giá theo số lượng
                    PaymentStatus = PaymentTransactionStatusEnum.PENDING,
                    IsPhysicalPrint = false // Vì đang chỉ xử lý ảnh số
                };
                transactions.Add(transactionEntity);
            }

            await _context.PaymentTransactions.AddRangeAsync(transactions);
            await _context.SaveChangesAsync();


            // 3. Tạo Payment record cho toàn bộ giỏ hàng
            var totalAmount = transactions.Sum(t => t.Amount);
            var payment = new Payment
            {
                PaymentTransactionId = transactions.First().Id,
                Amount = totalAmount,
                PaymentGateway = PaymentGatewayEnum.PAYOS,
                PaymentStatus = PaymentStatusEnum.PENDING
            };
            await _context.Payments.AddAsync(payment);
            await _context.SaveChangesAsync();

            // 4. Tạo Payment Link từ PayOS
            var orderCode = int.Parse(DateTimeOffset.Now.ToString("ffffff"));
            var items = transactions.Select(t => new ItemData(
                $"Ảnh {t.ImageId}", 1, (int)t.Amount
            )).ToList();

            var paymentData = new PaymentData(
                orderCode,
                (int)totalAmount,
                "Thanh toán giỏ hàng",
                items,
                request.ReturnUrl,
                $"{request.ReturnUrl}/cancel"
            );

            var createPayment = await _payOS.createPaymentLink(paymentData);

            // 5. Commit transaction & return checkout URL
            await transaction.CommitAsync();

            return new CreatePaymentResponse
            {
                PaymentUrl = createPayment.checkoutUrl
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.Error($"Error creating payment link: {ex.Message}");
            throw;
        }
    }

    // Xử lý Webhook khi thanh toán hoàn tất
    public async Task<WebhookResponse> HandleWebhook(WebhookType webhookType)
    {
        try
        {
            // Log the receipt of the webhook
            _logger.Info(JsonConvert.SerializeObject(webhookType));

            var verifiedData = _payOS.verifyPaymentWebhookData(webhookType); // Xác thực dữ liệu từ webhook
            var orderCode = verifiedData.orderCode;

            // Tìm PaymentTransaction dựa trên orderCode
            var paymentTransaction = await _context.PaymentTransactions.FirstOrDefaultAsync(x => x.Id == orderCode);

            if (paymentTransaction == null)
                return new WebhookResponse
                {
                    Success = false,
                    Note = "Transaction not found"
                };

            // Xử lý webhook dựa trên mã phản hồi từ PayOS
            switch (verifiedData.code)
            {
                case "00": // Thanh toán thành công
                    paymentTransaction.PaymentStatus = PaymentTransactionStatusEnum.COMPLETED;
                    await _context.SaveChangesAsync();

                    return new WebhookResponse
                    {
                        Success = true,
                        Note = "Payment processed successfully"
                    };

                case "01": // Lỗi thanh toán
                    paymentTransaction.PaymentStatus = PaymentTransactionStatusEnum.FAILED;
                    await _context.SaveChangesAsync();

                    return new WebhookResponse
                    {
                        Success = false,
                        Note = "Invalid parameters"
                    };

                default: // Trường hợp khác
                    paymentTransaction.PaymentStatus = PaymentTransactionStatusEnum.FAILED;
                    await _context.SaveChangesAsync();

                    return new WebhookResponse
                    {
                        Success = false,
                        Note = "Unhandled code"
                    };
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Webhook error: {ex.Message}");
            return new WebhookResponse { Success = false, Note = "Internal server error" };
        }
    }
}

public class WebhookResponse
{
    public bool Success { get; set; }
    public string Note { get; set; }
}