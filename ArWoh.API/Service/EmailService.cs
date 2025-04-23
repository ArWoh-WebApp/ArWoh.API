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

    public async Task SendPurchasedImagesEmailAsync(EmailRequestDTO request, int orderId)
    {
        try
        {
            _logger.Info($"Preparing to send purchased images email for order {orderId} to {request.UserEmail}");

            // Get order with details
            var order = await _unitOfWork.Orders.GetByIdAsync(
                orderId,
                o => o.OrderDetails.Where(od => !od.IsDeleted)
            );

            if (order == null)
            {
                _logger.Error($"Failed to send images email: Order {orderId} not found");
                return;
            }

            // Retrieve order details with images
            var orderDetails = await _unitOfWork.OrderDetails.FindAsync(
                od => od.OrderId == orderId && !od.IsDeleted,
                od => od.Image
            );

            if (!orderDetails.Any())
            {
                _logger.Error($"Failed to send images email: No order details found for order {orderId}");
                return;
            }

            // Build the image rows HTML for the email
            var imageRowsHtml = new StringBuilder();
            foreach (var detail in orderDetails)
            {
                if (detail.Image == null) continue;

                var downloadUrl = detail.Image.Url;
                var thumbnailUrl = detail.Image.Url;

                imageRowsHtml.AppendLine($@"
                <tr>
                    <td style='padding: 10px; border-bottom: 1px solid #ddd;'><img src='{thumbnailUrl}' alt='{detail.ImageTitle}' style='max-width: 100px; max-height: 100px;' /></td>
                    <td style='padding: 10px; border-bottom: 1px solid #ddd;'>{detail.ImageTitle}</td>
                    <td style='padding: 10px; text-align: center; border-bottom: 1px solid #ddd;'><a href='{downloadUrl}' style='background-color: #4CAF50; color: white; padding: 8px 12px; text-decoration: none; border-radius: 4px;'>Download</a></td>
                </tr>");
            }

            // Create physical print info HTML if applicable
            var physicalPrintHtml = "";
            if (order.IsPhysicalPrint)
            {
                physicalPrintHtml = $@"
                <div style='margin-top: 20px; padding: 15px; background-color: #f9f9f9; border-left: 4px solid #2196F3;'>
                    <h3>Physical Print Information</h3>
                    <p>Your physical prints will be prepared and shipped to the following address:</p>
                    <p><strong>Shipping Address:</strong> {order.ShippingAddress}</p>
                    <p>You will receive a separate notification when your order has been shipped.</p>
                </div>";
            }

            // Create the email
            var purchaseEmail = new EmailDTO
            {
                To = request.UserEmail,
                Subject = $"Your ArWoh Order #{orderId} - Image Downloads",
                Body = $@"
            <div style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                <h2 style='color: #2196F3; text-align: center;'>Thank you for your purchase, {request.UserName}!</h2>
                <p style='font-size: 16px;'>Your payment has been successfully processed. Below are the links to download your purchased images:</p>
                
                <table style='width: 100%; border-collapse: collapse;'>
                    <tr style='background-color: #f2f2f2;'>
                        <th style='padding: 10px; text-align: left; border-bottom: 1px solid #ddd;'>Image</th>
                        <th style='padding: 10px; text-align: left; border-bottom: 1px solid #ddd;'>Title</th>
                        <th style='padding: 10px; text-align: center; border-bottom: 1px solid #ddd;'>Download</th>
                    </tr>
                    {imageRowsHtml}
                </table>
                
                {physicalPrintHtml}
                
                <p style='margin-top: 20px; font-size: 16px;'>These download links will be active for 30 days. Please save your images in a secure location.</p>
                <p style='font-size: 16px;'>If you have any questions or need assistance, feel free to contact our customer support at <a href='mailto:support@arwoh.com' style='color: #2196F3;'>support@arwoh.com</a>.</p>
                <p style='font-size: 16px;'>Thank you for choosing ArWoh for your art needs!</p>
                <p style='font-size: 16px;'>Regards,<br>
                <span style='color: #2196F3; font-weight: bold;'>The ArWoh Team</span></p>
            </div>"
            };

            // Send the email
            await SendEmailAsync(purchaseEmail);
            _logger.Success($"Successfully sent purchased images email for order {orderId} to {request.UserEmail}");
        }
        catch (Exception ex)
        {
            _logger.Error($"Error sending purchased images email for order {orderId}: {ex.Message}");
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