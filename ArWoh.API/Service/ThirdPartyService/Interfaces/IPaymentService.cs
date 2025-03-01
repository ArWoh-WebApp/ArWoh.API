using ArWoh.API.Service.ThirdPartyService.Types;

namespace ArWoh.API.Service.ThirdPartyService.Interfaces
{
    public interface IPaymentService
    {
        Task<CreatePaymentResponse> CreatePayment(CreatePaymentRequest createPaymentRequest);
    }
}
