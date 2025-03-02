using ArWoh.API.Interface;
using ArWoh.API.Utils;
using Microsoft.AspNetCore.Mvc;

namespace ArWoh.API.Controllers;

[ApiController]
[Route("api/payment")]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly IClaimService _claimService;

    public PaymentController(IPaymentService paymentService, IClaimService claimService)
    {
        _paymentService = paymentService;
        _claimService = claimService;
    }
    
    [HttpPost("checkout")]
    [ProducesResponseType(typeof(ApiResult<string>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    [ProducesResponseType(typeof(ApiResult<object>), 500)]
    public async Task<IActionResult> Checkout()
    {
        try
        {
            var userId = _claimService.GetCurrentUserId();
            string paymentUrl = await _paymentService.ProcessPayment(userId);

            return Ok(new { success = true, url = paymentUrl });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }
}