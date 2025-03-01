using ArWoh.API.Service.ThirdPartyService.Services;
using ArWoh.API.Service.ThirdPartyService.Types;
using Net.payOS.Types;

namespace ArWoh.API.Service.ThirdPartyService.Interfaces
{
    public interface IPayOSService
    {
        Task<CreatePaymentResponse> CreatePaymentLink(CreatePaymentRequest request);
        Task<WebhookResponse> HandleWebhook(WebhookType webhookData);
    }


}
