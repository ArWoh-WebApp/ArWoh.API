using ArWoh.API.DTOs.OrderDTOs;
using ArWoh.API.DTOs.PaymentDTOs;
using ArWoh.API.Interface;
using ArWoh.API.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Net.payOS.Types;

namespace ArWoh.API.Controllers;

[ApiController]
[Route("api/payment")]
public class PaymentController : ControllerBase
{
   private readonly IClaimService _claimService;
   private readonly IPaymentService _paymentService;

   public PaymentController(IPaymentService paymentService, IClaimService claimService)
   {
       _paymentService = paymentService;
       _claimService = claimService;
   }

   [HttpPost("create")]
   [Authorize]
   public async Task<IActionResult> CreatePayment([FromBody] CreateOrderDto createOrderDto)
   {
       try
       {
           var userId = _claimService.GetCurrentUserId();
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

   [HttpGet("{id}")]
   [Authorize]
   public async Task<IActionResult> GetPaymentStatus(int id)
   {
       try
       {
           var status = await _paymentService.GetPaymentStatus(id);
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

   [HttpPost("{id}/cancel")]
   [Authorize]
   public async Task<IActionResult> CancelPayment(int id, [FromBody] CancelPaymentDto cancelPaymentDto)
   {
       try
       {
           await _paymentService.CancelPayment(id, cancelPaymentDto.Reason);
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