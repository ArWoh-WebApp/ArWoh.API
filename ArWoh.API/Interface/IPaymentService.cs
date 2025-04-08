using Microsoft.AspNetCore.Mvc;
using Net.payOS.Types;

namespace ArWoh.API.Interface;

public interface IPaymentService
{
    Task<string> ProcessPayment(int userId);
    Task<IActionResult> PaymentWebhook([FromBody] WebhookData webhookData);
}