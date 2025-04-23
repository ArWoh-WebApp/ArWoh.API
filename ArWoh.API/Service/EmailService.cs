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

    // Add to EmailService.cs
    public async Task SendPurchasedImagesEmailAsync(int orderId)
    {
        try
        {
            _logger.Info($"Preparing to send purchased images email for order {orderId}");

            // Get order with details and customer information
            var order = await _unitOfWork.Orders.GetByIdAsync(
                orderId,
                o => o.Customer,
                o => o.OrderDetails,
                o => o.OrderDetails.Select(od => od.Image)
            );

            if (order == null)
            {
                _logger.Error($"Failed to send images email: Order {orderId} not found");
                return;
            }

            if (order.Customer == null)
            {
                _logger.Error($"Failed to send images email: Customer for order {orderId} not found");
                return;
            }

            // Prepare email content
            var emailSubject = $"Your ArWoh Order #{orderId} - Image Downloads";
            var emailBuilder = new StringBuilder();

            // Build email HTML
            emailBuilder.AppendLine("<html><body style='font-family: Arial, sans-serif;'>");
            emailBuilder.AppendLine("<h2>Thank you for your purchase!</h2>");
            emailBuilder.AppendLine(
                "<p>Your payment has been successfully processed. Below are the links to download your purchased images:</p>");
            emailBuilder.AppendLine("<ul>");

            foreach (var detail in order.OrderDetails)
            {
                if (detail.Image == null) continue;

                // Create a signed/secure URL if needed or use the direct file URL
                var downloadUrl = detail.Image.Url;

                emailBuilder.AppendLine(
                    $"<li><strong>{detail.ImageTitle}</strong>: <a href='{downloadUrl}'>Download Image</a></li>");
            }

            emailBuilder.AppendLine("</ul>");
            emailBuilder.AppendLine(
                "<p>These links will be active for 7 days. Please save your images in a secure location.</p>");
            emailBuilder.AppendLine("<p>If you have any questions, please contact our customer support.</p>");
            emailBuilder.AppendLine("<p>Regards,<br/>ArWoh Team</p>");
            emailBuilder.AppendLine("</body></html>");

            // Create email request
            var emailRequest = new EmailDTO
            {
                To = order.Customer.Email,
                Subject = emailSubject,
                Body = emailBuilder.ToString()
            };

            // Send email
            await SendEmailAsync(emailRequest);
            _logger.Success($"Successfully sent purchased images email for order {orderId}");
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

        if (string.IsNullOrEmpty(emailUserName) || string.IsNullOrEmpty(emailPassword) ||
            string.IsNullOrEmpty(emailHost))
            throw new InvalidOperationException("Email configuration is missing in environment variables.");

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
            await smtp.ConnectAsync(emailHost, 587, SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(emailUserName, emailPassword);
            await smtp.SendAsync(email);
        }
        catch (Exception ex)
        {
            _logger.Error($"Error sending email: {ex.Message}");
        }
        finally
        {
            await smtp.DisconnectAsync(true);
        }
    }
}