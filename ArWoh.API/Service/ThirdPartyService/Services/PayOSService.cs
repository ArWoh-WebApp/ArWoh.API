using ArWoh.API.Entities;
using ArWoh.API.Interface;
using ArWoh.API.Service.ThirdPartyService.Interfaces;
using Net.payOS;

namespace ArWoh.API.Service.ThirdPartyService.Services
{
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

        //public async Task<CreatePaymentResponse> CreateLink(CreatePaymentRequest createPaymentRequest)
        //{
        //    if (createPaymentRequest.PaymentId == null)
        //    {
        //        throw new Exception("400 - PaymentId is required");
        //    }
        //    Payment payment = await _context.Payments.FindAsync(createPaymentRequest.PaymentId);

        //    if (payment == null)
        //    {
        //        throw new Exception("404 - Payment not found");
        //    }
        //    long orderCode = int.Parse(DateTimeOffset.Now.ToString("ffffff"));
        //    // Tạo mới Transaction liên kết với Payment

        //    Entities.Transaction transaction = new Entities.Transaction
        //    {
        //        CustomerId = payment.Transaction.CustomerId, // Lấy từ Transaction ban đầu
        //        ImageId = payment.Transaction.ImageId, // Lấy từ Transaction ban đầu
        //        Amount = payment.Amount,
        //        PaymentStatus = Enums.PaymentStatusEnum.PENDING,
        //        IsPhysicalPrint = payment.Transaction.IsPhysicalPrint,
        //    };

        //    //Add into DB
        //    await _context.Transactions.AddAsync(transaction);

        //    var paymentLinkRequest = new PaymentData(
        //        orderCode: orderCode,
        //        amount: (int)payment.Amount,
        //        description: "Thanh toán hóa đơn: " + orderCode,
        //        items: [new("Hóa đơn " + orderCode, 1, (int)payment.Amount)],
        //        returnUrl: createPaymentRequest.ReturnUrl + "?success=true&paymentId=" + orderCode + "&amount=" + (int)payment.Amount,
        //        cancelUrl: createPaymentRequest.ReturnUrl + "?canceled=true&paymentId=" + orderCode + "&amount=" + (int)payment.Amount
        //    );
        //    var response = await _payOS.createPaymentLink(paymentLinkRequest);

        //    return new CreatePaymentResponse
        //    {
        //        PaymentUrl = response.paymentLinkId
        //    };
        //}

        //public async Task<WebhookResponse> ReturnWebhook(WebhookType webhookType)
        //{
        //    try
        //    {
        //        // Log the receipt of the webhook
        //        _logger.Info(JsonConvert.SerializeObject(webhookType));

        //        WebhookData verifiedData = _payOS.verifyPaymentWebhookData(webhookType); //xác thực data from webhook
        //        string responseCode = verifiedData.code;
        //        long orderCode = verifiedData.orderCode;

        //        Entities.Transaction paymentTransaction = await _context.Transactions.FirstOrDefaultAsync(x => x.Id == orderCode);

        //        // Handle the webhook based on the transaction status
        //        switch (verifiedData.code)
        //        {
        //            case "00":
        //                // Update the transaction status
        //                paymentTransaction.PaymentStatus = PaymentTransactionStatusEnum.COMPLETED;
        //                paymentTransaction.TransactionLog = "Payment processed successfully";
        //                await _context.SaveChangesAsync();

        //                return new WebhookResponse
        //                {
        //                    Success = true,
        //                    Note = "Payment processed successfully"
        //                };

        //            case "01":
        //                // Update the transaction status
        //                paymentTransaction.Status = PaymentTransactionStatusEnum.FAILED.ToString();
        //                paymentTransaction.TransactionLog = "Payment failed";

        //                return new WebhookResponse
        //                {
        //                    Success = false,
        //                    Note = "Invalid parameters"
        //                };

        //            default:
        //                paymentTransaction.Status = PaymentTransactionStatusEnum.FAILED.ToString();
        //                paymentTransaction.TransactionLog = "Unhandled code";

        //                return new WebhookResponse
        //                {
        //                    Success = false,
        //                    Note = "Unhandled code"
        //                };
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.Error(ex.Message);
        //        throw ex;
        //    }
        //}

    }
    public class WebhookResponse
    {
        public bool Success { get; set; }
        public string Note { get; set; }
    }
}
