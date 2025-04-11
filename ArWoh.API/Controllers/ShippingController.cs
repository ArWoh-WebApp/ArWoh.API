using ArWoh.API.DTOs.ShippingDTOs;
using ArWoh.API.Interface;
using ArWoh.API.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArWoh.API.Controllers;

[ApiController]
[Route("api/shipping")]
public class ShippingController : ControllerBase
{
    private readonly IClaimService _claimService;
    private readonly IShippingService _shippingService;

    public ShippingController(IShippingService shippingService, IClaimService claimService)
    {
        _shippingService = shippingService;
        _claimService = claimService;
    }

    /// <summary>
    ///     Lấy danh sách hình ảnh có thể ship của user hiện tại
    /// </summary>
    [HttpGet("shippable-images")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResult<IEnumerable<ShippableImageDto>>), 200)]
    [ProducesResponseType(typeof(ApiResult<string>), 400)]
    [ProducesResponseType(typeof(ApiResult<string>), 500)]
    public async Task<IActionResult> GetShippableImages()
    {
        try
        {
            var userId = _claimService.GetCurrentUserId();
            var shippableImages = await _shippingService.GetShippableImagesByUserId(userId);

            return Ok(ApiResult<IEnumerable<ShippableImageDto>>.Success(
                shippableImages,
                "Shippable images retrieved successfully"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResult<string>.Error($"An error occurred: {ex.Message}"));
        }
    }

    /// <summary>
    ///     Tạo đơn hàng ship mới
    /// </summary>
    [HttpPost("orders")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResult<ShippingOrderDto>), 201)]
    [ProducesResponseType(typeof(ApiResult<string>), 400)]
    [ProducesResponseType(typeof(ApiResult<string>), 500)]
    public async Task<IActionResult> CreateShippingOrder([FromBody] ShippingOrderCreateDto orderDto)
    {
        try
        {
            var userId = _claimService.GetCurrentUserId();
            var shippingOrder = await _shippingService.CreateShippingOrder(orderDto, userId);

            return StatusCode(201, ApiResult<ShippingOrderDto>.Success(
                shippingOrder,
                "Shipping order created successfully"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResult<string>.Error(ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, ApiResult<string>.Error(ex.Message));
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
    ///     Lấy danh sách đơn hàng ship của user hiện tại
    /// </summary>
    [HttpGet("orders")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResult<IEnumerable<ShippingOrderDto>>), 200)]
    [ProducesResponseType(typeof(ApiResult<string>), 500)]
    public async Task<IActionResult> GetUserShippingOrders()
    {
        try
        {
            var userId = _claimService.GetCurrentUserId();
            var shippingOrders = await _shippingService.GetUserShippingOrders(userId);

            return Ok(ApiResult<IEnumerable<ShippingOrderDto>>.Success(
                shippingOrders,
                "Shipping orders retrieved successfully"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResult<string>.Error($"An error occurred: {ex.Message}"));
        }
    }

    /// <summary>
    ///     Lấy chi tiết một đơn hàng ship cụ thể
    /// </summary>
    [HttpGet("orders/{id}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResult<ShippingOrderDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<string>), 404)]
    [ProducesResponseType(typeof(ApiResult<string>), 500)]
    public async Task<IActionResult> GetShippingOrderById(int id)
    {
        try
        {
            var userId = _claimService.GetCurrentUserId();
            var shippingOrder = await _shippingService.GetShippingOrderById(id, userId);

            return Ok(ApiResult<ShippingOrderDto>.Success(
                shippingOrder,
                "Shipping order retrieved successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResult<string>.Error(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResult<string>.Error($"An error occurred: {ex.Message}"));
        }
    }
}