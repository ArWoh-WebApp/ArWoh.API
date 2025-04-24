using System.Text;
using ArWoh.API.DTOs.EmailDTOs;
using ArWoh.API.Interface;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;

namespace ArWoh.API.Service;

public class EmailService : IEmailService
{
    private readonly ILoggerService _logger;
    private readonly IUnitOfWork _unitOfWork;

    public EmailService(ILoggerService logger, IUnitOfWork unitOfWork)
    {
        _logger = logger;
        _unitOfWork = unitOfWork;
    }

    public async Task SendWelcomeNewUserAsync(EmailRequestDTO emailRequest)
    {
        // Create a welcome email
        var welcomeEmail = new EmailDTO
        {
            To = emailRequest.UserEmail,
            Subject = "Welcome to VaccinaCare!",
            Body = $@"
        <div style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
            <h1 style='color: #1e1b4b; text-align: center;'>Welcome, {emailRequest.UserEmail}!</h1>
            <p style='font-size: 16px;'>Thank you for signing up for VaccinaCare, your trusted partner in managing your child's vaccination schedule. We're excited to have you on board!</p>
            <p style='font-size: 16px;'>Here are some of the features you can enjoy:</p>
            <ul style='font-size: 16px; padding-left: 20px;'>
                <li>Track your child's vaccination history</li>
                <li>Receive timely reminders for upcoming vaccines</li>
                <li>Book and manage vaccination appointments with ease</li>
            </ul>
            <p style='font-size: 16px;'>If you have any questions, feel free to reach out to our support team at <a href='mailto:support@vaccinacare.com' style='color: #1e1b4b;'>support@vaccinacare.com</a>.</p>
            <p style='font-size: 16px;'>Best regards,<br>
            <span style='color: #1e1b4b; font-weight: bold;'>The VaccinaCare Team</span></p>
        </div>
        "
        };
        // Send the email
        await SendEmailAsync(welcomeEmail);
    }

    public async Task SendPurchasedImagesEmailAsync(EmailRequestDTO emailRequest, int orderId)
    {
        try
        {
            // Lấy thông tin đơn hàng và chi tiết đơn hàng
            var order = await _unitOfWork.Orders.GetByIdAsync(
                orderId,
                o => o.OrderDetails
            );

            if (order == null)
            {
                _logger.Error($"Cannot send purchased images email: Order {orderId} not found");
                return;
            }

            // Tạo danh sách các hình ảnh đã mua
            StringBuilder imageList = new StringBuilder();
            foreach (var detail in order.OrderDetails)
            {
                imageList.AppendLine($@"
                <div style='margin-bottom: 20px; border: 1px solid #ddd; padding: 15px; border-radius: 8px;'>
                    <div style='font-weight: bold; color: #333; font-size: 16px;'>{detail.ImageTitle ?? "Untitled Image"}</div>
                    <div style='margin-top: 5px; color: #555;'>Số lượng: {detail.Quantity}</div>
                    <div style='margin-top: 5px; color: #555;'>Giá: {detail.Price:N0} VND</div>
                </div>
            ");
            }

            // Tạo email nội dung
            var purchasedImagesEmail = new EmailDTO
            {
                To = emailRequest.UserEmail,
                Subject = $"Cảm ơn bạn đã mua hàng tại ArWoh - Đơn hàng #{orderId}",
                Body = $@"
            <div style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto;'>
                <div style='background-color: #1e1b4b; padding: 20px; text-align: center;'>
                    <h1 style='color: #ffffff; margin: 0;'>Đơn hàng của bạn đã được xác nhận</h1>
                </div>
                
                <div style='padding: 20px;'>
                    <p style='font-size: 16px;'>Xin chào {emailRequest.UserName},</p>
                    
                    <p style='font-size: 16px;'>Cảm ơn bạn đã mua hàng tại ArWoh. Đơn hàng #{orderId} của bạn đã được thanh toán thành công!</p>
                    
                    <div style='background-color: #f7f7f7; padding: 15px; border-radius: 8px; margin: 20px 0;'>
                        <h2 style='color: #1e1b4b; margin-top: 0;'>Chi tiết đơn hàng</h2>
                        <p><strong>Mã đơn hàng:</strong> #{orderId}</p>
                        <p><strong>Ngày đặt hàng:</strong> {order.CreatedAt:dd/MM/yyyy HH:mm}</p>
                        <p><strong>Tổng tiền:</strong> {order.TotalAmount:N0} VND</p>
                    </div>
                    
                    <h2 style='color: #1e1b4b;'>Ảnh đã mua</h2>
                    {imageList}
                    
                    <p style='font-size: 16px;'>Bạn có thể tải xuống các hình ảnh đã mua bằng cách đăng nhập vào tài khoản của mình trên website của chúng tôi.</p>
                    
                    <div style='background-color: #f7f7f7; padding: 15px; border-radius: 8px; margin: 20px 0;'>
                        <h2 style='color: #1e1b4b; margin-top: 0;'>Cần hỗ trợ?</h2>
                        <p>Nếu bạn có bất kỳ câu hỏi nào về đơn hàng hoặc cần hỗ trợ, vui lòng liên hệ với chúng tôi qua email: <a href='mailto:support@arwoh.com' style='color: #1e1b4b;'>support@arwoh.com</a></p>
                    </div>
                    
                    <p style='font-size: 16px;'>Trân trọng,<br>
                    <span style='font-weight: bold; color: #1e1b4b;'>Đội ngũ ArWoh</span></p>
                </div>
                
                <div style='background-color: #f2f2f2; padding: 20px; text-align: center; font-size: 14px; color: #777;'>
                    <p>© 2024 ArWoh. Tất cả các quyền được bảo lưu.</p>
                </div>
            </div>
            "
            };

            // Gửi email 
            await SendEmailAsync(purchasedImagesEmail);
            _logger.Success(
                $"Purchased images email sent successfully to {emailRequest.UserEmail} for order {orderId}");
        }
        catch (Exception ex)
        {
            _logger.Error($"Error sending purchased images email: {ex.Message}");
            throw;
        }
    }

    private async Task SendEmailAsync(EmailDTO request)
    {
        var email = new MimeMessage();

        // Read environment variables
        var emailUserName = Environment.GetEnvironmentVariable("EMAIL_USERNAME");
        var emailPassword = Environment.GetEnvironmentVariable("EMAIL_PASSWORD");
        var emailHost = Environment.GetEnvironmentVariable("EMAIL_HOST");

        // Add logging to check environment variables
        _logger.Info("Email configuration check:");
        _logger.Info($"EMAIL_USERNAME exists: {!string.IsNullOrEmpty(emailUserName)}");
        _logger.Info($"EMAIL_PASSWORD exists: {!string.IsNullOrEmpty(emailPassword)}");
        _logger.Info($"EMAIL_HOST exists: {!string.IsNullOrEmpty(emailHost)}");
        _logger.Info($"EMAIL_HOST value: {emailHost ?? "null"}");

        if (string.IsNullOrEmpty(emailUserName) || string.IsNullOrEmpty(emailPassword) ||
            string.IsNullOrEmpty(emailHost))
        {
            _logger.Error("Email configuration is missing in environment variables.");
            _logger.Error($"Missing variables: " +
                          (string.IsNullOrEmpty(emailUserName) ? "EMAIL_USERNAME " : "") +
                          (string.IsNullOrEmpty(emailPassword) ? "EMAIL_PASSWORD " : "") +
                          (string.IsNullOrEmpty(emailHost) ? "EMAIL_HOST" : ""));
            throw new InvalidOperationException("Email configuration is missing in environment variables.");
        }

        _logger.Info($"Preparing to send email to: {request.To}");
        email.From.Add(MailboxAddress.Parse(emailUserName));
        email.To.Add(MailboxAddress.Parse(request.To));
        email.Subject = request.Subject;
        email.Body = new TextPart(TextFormat.Html)
        {
            Text = request.Body
        };

        using var smtp = new SmtpClient();
        try
        {
            _logger.Info($"Connecting to SMTP server: {emailHost}:587");
            await smtp.ConnectAsync(emailHost, 587, SecureSocketOptions.StartTls);

            _logger.Info("Authenticating with SMTP server");
            await smtp.AuthenticateAsync(emailUserName, emailPassword);

            _logger.Info("Sending email");
            await smtp.SendAsync(email);
            _logger.Success("Email sent successfully");
        }
        catch (Exception ex)
        {
            _logger.Error($"Error sending email: {ex.Message}");
            _logger.Error($"Stack trace: {ex.StackTrace}");

            // Check for specific error types to provide more helpful debugging
            if (ex is MailKit.Security.AuthenticationException)
                _logger.Error("Authentication failed - check username and password");
            else if (ex is MailKit.Net.Smtp.SmtpCommandException smtpEx)
                _logger.Error($"SMTP command error: {smtpEx.StatusCode}, {smtpEx.ErrorCode}");
            else if (ex is MailKit.Net.Smtp.SmtpProtocolException)
                _logger.Error("SMTP protocol error - check host and port configuration");
            else if (ex is System.Net.Sockets.SocketException)
                _logger.Error("Socket error - check network connectivity and firewall settings");

            throw;
        }
        finally
        {
            try
            {
                _logger.Info("Disconnecting from SMTP server");
                await smtp.DisconnectAsync(true);
            }
            catch (Exception exDisconnect)
            {
                _logger.Error($"Error disconnecting from SMTP server: {exDisconnect.Message}");
            }
        }
    }
}