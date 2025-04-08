using ArWoh.API.Entities;

namespace ArWoh.API.Interface;

public interface IPaymentTransactionService
{
    Task<IEnumerable<PaymentTransaction>> GetAllTransactions();
}