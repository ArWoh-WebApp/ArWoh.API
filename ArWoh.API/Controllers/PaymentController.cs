using ArWoh.API.DTOs.OrderDTOs;
using ArWoh.API.DTOs.PaymentDTOs;
using ArWoh.API.Enums;
using ArWoh.API.Interface;
using ArWoh.API.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Net.payOS.Types;

namespace ArWoh.API.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentController : ControllerBase
{
    private readonly IClaimService _claimService;
    private readonly IPaymentService _paymentService;

    public PaymentController(IPaymentService paymentService, IClaimService claimService)
    {
        _paymentService = paymentService;
        _claimService = claimService;
    }

    /// <summary>
    ///     Tạo link thanh toán từ giỏ hàng hiện tại của người dùng
    /// </summary>
    [HttpGet("create-link")]
    [Authorize]
    public async Task<IActionResult> CreatePaymentLink()
    {
        try
        {
            var userId = _claimService.GetCurrentUserId();
            // Tạo createOrderDto mặc định
            var createOrderDto = new CreateOrderDto
            {
                IsPhysicalPrint = false,
                PaymentGateway = PaymentGatewayEnum.PAYOS,
                RedirectUrl = "https://arwoh.vercel.app/payment-success"
            };

            var paymentUrl = await _paymentService.ProcessPayment(userId, createOrderDto);
            return Ok(new ApiResult<string>
            {
                IsSuccess = true,
                Data = paymentUrl,
                Message = "Payment link created successfully"
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResult<object>
            {
                IsSuccess = false,
                Message = ex.Message
            });
        }
    }

    /// <summary>
    ///     Lấy thông tin trạng thái thanh toán theo paymentId
    /// </summary>
    [HttpGet("{paymentId}")]
    [Authorize]
    public async Task<IActionResult> GetPaymentStatus(int paymentId)
    {
        try
        {
            var status = await _paymentService.GetPaymentStatus(paymentId);
            return Ok(new ApiResult<PaymentStatusDto>
            {
                IsSuccess = true,
                Data = status
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResult<object>
            {
                IsSuccess = false,
                Message = ex.Message
            });
        }
    }

    /// <summary>
    ///     Endpoint nhận callback từ PayOS sau khi thanh toán
    /// </summary>
    [HttpPost("webhook")]
    [AllowAnonymous] // Webhook từ PayOS không cần xác thực
    public async Task<IActionResult> ProcessWebhook([FromBody] WebhookType webhookData)
    {
        try
        {
            await _paymentService.ProcessWebhook(webhookData);
            return Ok();
        }
        catch (Exception ex)
        {
            // Vẫn trả về 200 OK để PayOS không gửi lại webhook
            // Nhưng log lỗi để theo dõi
            return Ok();
        }
    }

    /// <summary>
    ///     Hủy thanh toán đang chờ xử lý
    /// </summary>
    [HttpPost("{paymentId}/cancel")]
    [Authorize]
    public async Task<IActionResult> CancelPayment(int paymentId, [FromBody] CancelPaymentDto cancelPaymentDto)
    {
        try
        {
            await _paymentService.CancelPayment(paymentId, cancelPaymentDto.Reason);
            return Ok(new ApiResult<bool>
            {
                IsSuccess = true,
                Data = true,
                Message = "Payment cancelled successfully"
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResult<object>
            {
                IsSuccess = false,
                Message = ex.Message
            });
        }
    }

    /// <summary>
    ///     Lấy lịch sử thanh toán của một đơn hàng
    /// </summary>
    [HttpGet("order/{orderId}")]
    [Authorize]
    public async Task<IActionResult> GetPaymentsByOrderId(int orderId)
    {
        try
        {
            var payments = await _paymentService.GetPaymentsByOrderId(orderId);
            return Ok(new ApiResult<List<PaymentStatusDto>>
            {
                IsSuccess = true,
                Data = payments
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResult<object>
            {
                IsSuccess = false,
                Message = ex.Message
            });
        }
    }
}