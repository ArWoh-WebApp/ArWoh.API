using ArWoh.API.Entities;
using ArWoh.API.Interface;

namespace ArWoh.API.Service;

public class PaymentTransactionService
{
    private readonly ILoggerService _logger;
    private readonly IUnitOfWork _unitOfWork;

    public PaymentTransactionService(IUnitOfWork unitOfWork, ILoggerService logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<IEnumerable<PaymentTransaction>> GetAllTransactions()
    {
        try
        {
            var transactions = await _unitOfWork.PaymentTransactions.GetAllAsync();

            if (transactions == null || !transactions.Any())
                throw new KeyNotFoundException("No transactions found");

            return transactions;
        }
        catch (Exception ex)
        {
            throw new Exception($"Error retrieving all transactions: {ex.Message}", ex);
        }
    }
}