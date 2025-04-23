using ArWoh.API.DTOs.EmailDTOs;

namespace ArWoh.API.Interface;

public interface IEmailService
{
    Task SendPurchasedImagesEmailAsync(EmailRequestDTO request, int orderId);
}