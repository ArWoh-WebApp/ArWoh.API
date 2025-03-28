using ArWoh.API.DTOs.PaymentDTOs;
using ArWoh.API.Entities;
using Microsoft.AspNetCore.Mvc;
using Net.payOS.Types;

namespace ArWoh.API.Interface;

public interface IPaymentService
{
    Task<string> ProcessPayment(int userId);

    Task<IActionResult> PaymentWebhook([FromBody] WebhookData webhookData);

    Task<IEnumerable<PaymentTransaction>> GetAllTransactions();

    Task<IEnumerable<PaymentTransaction>> GetUserTransactions(int userId);

    Task<RevenueDto> GetPhotographerRevenue(int photographerId);
}