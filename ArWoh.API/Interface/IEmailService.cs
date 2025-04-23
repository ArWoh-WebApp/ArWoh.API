

namespace ArWoh.API.Interface;

public interface IEmailService
{
    Task SendPurchasedImagesEmailAsync(int orderId);
}