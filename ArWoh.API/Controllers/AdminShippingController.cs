using ArWoh.API.DTOs.ShippingDTOs;
using ArWoh.API.Interface;
using ArWoh.API.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
///     Controller để quản lý đơn hàng ship (dành cho Admin)
/// </summary>
[ApiController]
[Route("api/admin/shipping")]
[Authorize(Roles = "Admin")]
public class AdminShippingController : ControllerBase
{
    private readonly IShippingService _shippingService;

    public AdminShippingController(IShippingService shippingService)
    {
        _shippingService = shippingService;
    }

    /// <summary>
    ///     Lấy tất cả đơn hàng ship trong hệ thống
    /// </summary>
    [HttpGet("orders")]
    [ProducesResponseType(typeof(ApiResult<IEnumerable<ShippingOrderDto>>), 200)]
    [ProducesResponseType(typeof(ApiResult<string>), 500)]
    public async Task<IActionResult> GetAllShippingOrders()
    {
        try
        {
            var shippingOrders = await _shippingService.GetAllShippingOrders();

            return Ok(ApiResult<IEnumerable<ShippingOrderDto>>.Success(
                shippingOrders,
                "All shipping orders retrieved successfully"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResult<string>.Error($"An error occurred: {ex.Message}"));
        }
    }

    /// <summary>
    ///     Cập nhật trạng thái đơn hàng ship
    /// </summary>
    [HttpPut("orders/{id}/status")]
    [ProducesResponseType(typeof(ApiResult<ShippingOrderDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<string>), 400)]
    [ProducesResponseType(typeof(ApiResult<string>), 404)]
    [ProducesResponseType(typeof(ApiResult<string>), 500)]
    public async Task<IActionResult> UpdateShippingOrderStatus(int id,
        [FromForm] ShippingStatusUpdateDto statusUpdateDto)
    {
        try
        {
            var updatedOrder = await _shippingService.UpdateShippingOrderStatus(
                id, statusUpdateDto.Status, statusUpdateDto.Note);

            return Ok(ApiResult<ShippingOrderDto>.Success(
                updatedOrder,
                $"Shipping order status updated to {statusUpdateDto.Status}"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResult<string>.Error(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResult<string>.Error(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResult<string>.Error($"An error occurred: {ex.Message}"));
        }
    }

    /// <summary>
    ///     Upload hình ảnh chứng minh giao hàng
    /// </summary>
    [HttpPost("orders/{id}/proof-image")]
    [ProducesResponseType(typeof(ApiResult<ShippingOrderDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<string>), 400)]
    [ProducesResponseType(typeof(ApiResult<string>), 404)]
    [ProducesResponseType(typeof(ApiResult<string>), 500)]
    public async Task<IActionResult> UploadDeliveryProofImage(int id, IFormFile image)
    {
        try
        {
            if (image == null || image.Length == 0)
                return BadRequest(ApiResult<string>.Error("No image file provided"));

            var updatedOrder = await _shippingService.UploadDeliveryProofImage(id, image);

            return Ok(ApiResult<ShippingOrderDto>.Success(
                updatedOrder,
                "Delivery proof image uploaded successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResult<string>.Error(ex.Message));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResult<string>.Error(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResult<string>.Error(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResult<string>.Error($"An error occurred: {ex.Message}"));
        }
    }
}