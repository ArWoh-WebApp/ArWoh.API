using ArWoh.API.DTOs.EmailDTOs;
using ArWoh.API.Interface;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;
using System.Text;
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

    public async Task SendPurchasedImagesEmailAsync(int orderId)
    {
        try
        {
            _logger.Info($"Preparing to send purchased images email for order {orderId}");
            
            // Get order with details and customer information
            var order = await _unitOfWork.Orders.GetByIdAsync(
                orderId,
                o => o.Customer,
                o => o.OrderDetails.Where(od => !od.IsDeleted)
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
            
            // Prepare email content
            var emailSubject = $"Your ArWoh Order #{orderId} - Image Downloads";
            var emailBuilder = new StringBuilder();
            
            // Build email HTML
            emailBuilder.AppendLine("<html><body style='font-family: Arial, sans-serif;'>");
            emailBuilder.AppendLine("<h2>Thank you for your purchase!</h2>");
            emailBuilder.AppendLine("<p>Your payment has been successfully processed. Below are the links to download your purchased images:</p>");
            emailBuilder.AppendLine("<table style='width: 100%; border-collapse: collapse;'>");
            emailBuilder.AppendLine("<tr style='background-color: #f2f2f2;'>");
            emailBuilder.AppendLine("<th style='padding: 10px; text-align: left; border-bottom: 1px solid #ddd;'>Image</th>");
            emailBuilder.AppendLine("<th style='padding: 10px; text-align: left; border-bottom: 1px solid #ddd;'>Title</th>");
            emailBuilder.AppendLine("<th style='padding: 10px; text-align: center; border-bottom: 1px solid #ddd;'>Download</th>");
            emailBuilder.AppendLine("</tr>");
            
            foreach (var detail in orderDetails)
            {
                if (detail.Image == null) continue;
                
                var downloadUrl = detail.Image.Url;
                var thumbnailUrl = detail.Image.Url; // You might want a smaller thumbnail version if available
                
                emailBuilder.AppendLine("<tr>");
                emailBuilder.AppendLine($"<td style='padding: 10px; border-bottom: 1px solid #ddd;'><img src='{thumbnailUrl}' alt='{detail.ImageTitle}' style='max-width: 100px; max-height: 100px;' /></td>");
                emailBuilder.AppendLine($"<td style='padding: 10px; border-bottom: 1px solid #ddd;'>{detail.ImageTitle}</td>");
                emailBuilder.AppendLine($"<td style='padding: 10px; text-align: center; border-bottom: 1px solid #ddd;'><a href='{downloadUrl}' style='background-color: #4CAF50; color: white; padding: 8px 12px; text-decoration: none; border-radius: 4px;'>Download</a></td>");
                emailBuilder.AppendLine("</tr>");
            }
            
            emailBuilder.AppendLine("</table>");
            
            // Add additional information for physical prints if applicable
            if (order.IsPhysicalPrint)
            {
                emailBuilder.AppendLine("<div style='margin-top: 20px; padding: 15px; background-color: #f9f9f9; border-left: 4px solid #2196F3;'>");
                emailBuilder.AppendLine("<h3>Physical Print Information</h3>");
                emailBuilder.AppendLine("<p>Your physical prints will be prepared and shipped to the following address:</p>");
                emailBuilder.AppendLine($"<p><strong>Shipping Address:</strong> {order.ShippingAddress}</p>");
                emailBuilder.AppendLine("<p>You will receive a separate notification when your order has been shipped.</p>");
                emailBuilder.AppendLine("</div>");
            }
            
            
            // Create email request
            var emailRequest = new EmailDTO
            {
                To = order.Customer.Email,
                Subject = emailSubject,
                Body = emailBuilder.ToString()
            };
            
            // Send email
            await SendEmailAsync(emailRequest);
            _logger.Success($"Successfully sent purchased images email for order {orderId} to {order.Customer.Email}");
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
            throw; // Add throw here to propagate the error for better debugging
        }
        finally
        {
            await smtp.DisconnectAsync(true);
        }
    }
}