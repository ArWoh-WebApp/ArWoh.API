using ArWoh.API.Interface;
using ArWoh.API.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArWoh.API.Controllers;

public class AdminController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly ILoggerService _loggerService;

    public AdminController(IPaymentService paymentService, ILoggerService loggerService)
    {
        _paymentService = paymentService;
        _loggerService = loggerService;
    }

    [Authorize(Policy = "AdminPolicy")]
    [ProducesResponseType(typeof(ApiResult<object>), 200)]
    public async Task<IActionResult> GetAllTransactions()
    {
        try
        {
            var transactions = await _paymentService.GetAllTransactions();

            return Ok(ApiResult<object>.Success(transactions, "Transactions retrieved successfully."));
        }
        catch (KeyNotFoundException ex)
        {
            return Ok(ApiResult<object>.Error(ex.Message));
        }
        catch (Exception ex)
        {
            return Ok(ApiResult<object>.Error($"An unexpected error occurred: {ex.Message}"));
        }
    }
}