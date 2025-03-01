using ArWoh.API.Service.ThirdPartyService.Types;

namespace ArWoh.API.Service.ThirdPartyService.Interfaces
{
    public interface IVnPayService
    {
        public Task<CreatePaymentResponse> CreateLink(CreatePaymentRequest createPaymentRequest);
    }
}
