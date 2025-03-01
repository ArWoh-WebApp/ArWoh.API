using ArWoh.API.Enums;
using ArWoh.API.Interface;
using ArWoh.API.Service.ThirdPartyService.Interfaces;
using ArWoh.API.Service.ThirdPartyService.Types;

namespace ArWoh.API.Service
{
    //Service này là chung để gọi các hàm VnPayService, PayOSService
    public class PaymentService : IPaymentService
    {
        private readonly ILoggerService _logger;
        private readonly IVnPayService _vnPayService;
        private readonly IPayOSService _payOSService;

        public PaymentService(ILoggerService logger, IVnPayService vnPayService, IPayOSService payOSService)
        {
            _logger = logger;
            _vnPayService = vnPayService;
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
                PaymentGatewayEnum.VNPAY => await _vnPayService.CreateLink(createPaymentRequest),
                PaymentGatewayEnum.PAYOS => await _payOSService.CreateLink(createPaymentRequest),
                _ => throw new Exception("400 - PaymentType is invalid"),
            };
        }
    }
}
