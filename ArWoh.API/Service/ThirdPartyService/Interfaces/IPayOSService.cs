using ArWoh.API.Service.ThirdPartyService.Services;
using ArWoh.API.Service.ThirdPartyService.Types;
using Net.payOS.Types;

namespace ArWoh.API.Service.ThirdPartyService.Interfaces
{
    public interface IPayOSService
    {
        public Task<CreatePaymentResponse> CreateLink(CreatePaymentRequest createPaymentRequest);
        public Task<WebhookResponse> ReturnWebhook(WebhookType webhookType);
    }
}
