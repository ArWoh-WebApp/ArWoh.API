using ArWoh.API.DTOs.UserDTOs;
using ArWoh.API.Interface;
using ArWoh.API.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArWoh.API.Controllers;

[ApiController]
[Route("api/admins")]
public class AdminController : ControllerBase
{
    private readonly IClaimService _claimService;
    private readonly ILoggerService _loggerService;
    private readonly IPaymentService _paymentService;
    private readonly IUserService _userService;


    public AdminController(IPaymentService paymentService, ILoggerService loggerService, IClaimService claimService,
        IUserService userService)
    {
        _paymentService = paymentService;
        _loggerService = loggerService;
        _claimService = claimService;
        _userService = userService;
    }

    // [Authorize(Policy = "AdminPolicy")]
    // [HttpGet("transactions")]
    // [ProducesResponseType(typeof(ApiResult<IEnumerable<PaymentTransaction>>), 200)]
    // [ProducesResponseType(typeof(ApiResult<object>), 404)]
    // [ProducesResponseType(typeof(ApiResult<object>), 500)]
    // public async Task<IActionResult> GetAllTransactions()
    // {
    //     try
    //     {
    //         var transactions = await _paymentTransactionService.GetAllTransactions();
    //         return Ok(ApiResult<IEnumerable<PaymentTransaction>>.Success(transactions, "Transactions retrieved successfully."));
    //     }
    //     catch (KeyNotFoundException ex)
    //     {
    //         return NotFound(ApiResult<object>.Error(ex.Message));
    //     }
    //     catch (Exception ex)
    //     {
    //         return StatusCode(500, ApiResult<object>.Error($"An unexpected error occurred: {ex.Message}"));
    //     }
    // }

    [Authorize(Policy = "AdminPolicy")]
    [HttpGet("users")]
    [ProducesResponseType(typeof(ApiResult<List<UserProfileDto>>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    public async Task<IActionResult> GetAllUsers()
    {
        try
        {
            var users = await _userService.GetAllUsers();

            return Ok(new ApiResult<List<UserProfileDto>>
            {
                IsSuccess = true,
                Message = "Users retrieved successfully",
                Data = users
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResult<object>
            {
                IsSuccess = false,
                Message = "An internal server error occurred",
                Data = null
            });
        }
    }
}