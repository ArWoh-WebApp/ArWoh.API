using ArWoh.API.Entities;
using ArWoh.API.Interface;
using ArWoh.API.Service.ThirdPartyService.Interfaces;
using ArWoh.API.Service.ThirdPartyService.Types;
using VNPAY.NET;
using VNPAY.NET.Enums;
using VNPAY.NET.Models;

namespace ArWoh.API.Service.ThirdPartyService.Services
{
    public class VnPayService : IVnPayService
    {
        private readonly ArWohDbContext _context;
        private readonly ILoggerService _logger;
        private readonly IVnpay _vnPay;

        public VnPayService(ILoggerService logger, ArWohDbContext context, IVnpay vnPay)
        {
            _logger = logger;
            _vnPay = vnPay;
            _context = context;
        }

        public async Task<CreatePaymentResponse> CreateLink(CreatePaymentRequest createPaymentRequest)
        {
            if (createPaymentRequest.PaymentId == null)
            {
                throw new Exception("400 - PaymentId is required");
            }

            // Lấy thông tin thanh toán từ DB
            var payment = await _context.Payments.FindAsync(createPaymentRequest.PaymentId);
            if (payment == null)
            {
                throw new Exception("404 - Payment not found");
            }

            // Tạo mã đơn hàng dựa trên timestamp
            long orderCode = long.Parse(DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmssfff"));

            // Tạo mới Transaction liên kết với Payment
            Transaction transaction = new Transaction
            {
                CustomerId = payment.Transaction.CustomerId, // Lấy từ Transaction ban đầu
                ImageId = payment.Transaction.ImageId, // Lấy từ Transaction ban đầu
                Amount = payment.Amount,
                PaymentStatus = Enums.PaymentStatusEnum.PENDING,
                IsPhysicalPrint = payment.Transaction.IsPhysicalPrint,
            };

            // Lưu transaction vào database
            await _context.Transactions.AddAsync(transaction);
            await _context.SaveChangesAsync();

            // Tạo request cho VnPay
            var paymentRequest = new PaymentRequest
            {
                PaymentId = orderCode,
                Money = (int)(payment.Amount * 100), // VnPay yêu cầu đơn vị là VND (đồng)
                Description = "Thanh toán hóa đơn: " + orderCode,
                IpAddress = "SAMPLE_IP_ADDRESS",
                BankCode = BankCode.ANY, // Mặc định cho tất cả ngân hàng
                CreatedDate = DateTime.UtcNow.AddHours(7), // Giờ Việt Nam
                Currency = Currency.VND, // Đơn vị tiền tệ
                Language = DisplayLanguage.Vietnamese // Ngôn ngữ hiển thị
            };

            // Gọi VnPay để lấy URL thanh toán
            string paymentUrl = _vnPay.GetPaymentUrl(paymentRequest);

            if (string.IsNullOrEmpty(paymentUrl))
            {
                throw new Exception("500 - Failed to generate payment link.");
            }

            // Cập nhật TransactionId vào Payment để theo dõi
            payment.TransactionId = transaction.Id;
            _context.Payments.Update(payment);
            await _context.SaveChangesAsync();

            // Ghi log
            _logger.Info($"Created VnPay payment link for PaymentId: {payment.Id}, TransactionId: {transaction.Id}, URL: {paymentUrl}");

            return new CreatePaymentResponse { PaymentUrl = paymentUrl };
        }




    }
}
