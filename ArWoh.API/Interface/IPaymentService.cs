using ArWoh.API.Service.ThirdPartyService.Types;

namespace ArWoh.API.Interface;

public interface IPaymentService
{
    Task<string> ProcessPayment(int userId);
}