using ArWoh.API.Service.ThirdPartyService.Types;

namespace ArWoh.API.Interface;

public interface IPaymentService
{
    Task<CreatePaymentResponse> CreatePayment(CreatePaymentRequest createPaymentRequest);
}