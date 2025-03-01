using ArWoh.API.Enums;
using ArWoh.API.Interface;
using ArWoh.API.Service.ThirdPartyService.Interfaces;
using ArWoh.API.Service.ThirdPartyService.Types;

namespace ArWoh.API.Service.ThirdPartyService
{
    //Service chung để gọi các hàm VnPayService, PayOSService
    public class PaymentService : IPaymentService
    {
        // Tạo link truyền enum PaymentType[VnPay, PayOS] params: orderId, paymentType

        // IPN callback từ PayOS
        //1. Webhook của PayOS (Gọi PayOS để update transaction)

        private readonly ILoggerService _logger;
        private readonly IPayOSService _payOSService;

        public PaymentService(ILoggerService logger, IPayOSService payOSService)
        {
            _logger = logger;
            _payOSService = payOSService;
        }

        public async Task<CreatePaymentResponse> CreatePayment(CreatePaymentRequest createPaymentRequest)
        {
            // String to enum conversion with validation
            if (!Enum.TryParse<PaymentGatewayEnum>(createPaymentRequest.PaymentMethod, true, out var paymentGatewayEnum))
            {
                throw new Exception("400 - PaymentMethod is invalid");
            }

            // Use switch to determine the appropriate payment gateway
            return paymentGatewayEnum switch
            {
                PaymentGatewayEnum.PAYOS => await _payOSService.CreatePaymentLink(createPaymentRequest),
                _ => throw new Exception("400 - PaymentType is invalid"),
            };
        }

    }
}
