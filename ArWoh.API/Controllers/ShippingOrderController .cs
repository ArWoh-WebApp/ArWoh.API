using ArWoh.API.DTOs.ShipOrderDTOs;
using ArWoh.API.Interface;
using ArWoh.API.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArWoh.API.Controllers;

[ApiController]
[Route("api/shipping")]
public class ShippingOrderController : ControllerBase
{
    private readonly IClaimService _claimService;
    private readonly ILoggerService _logger;
    private readonly IShippingOrderService _shippingService;

    public ShippingOrderController(
        IShippingOrderService shippingService,
        IClaimService claimService,
        ILoggerService logger)
    {
        _shippingService = shippingService;
        _claimService = claimService;
        _logger = logger;
    }

    /// <summary>
    ///     Lấy danh sách hình ảnh có thể ship của user hiện tại
    /// </summary>
    [HttpGet("shippable-images")]
    [Authorize]
    public async Task<ActionResult<ApiResult<IEnumerable<ShippableImageDto>>>> GetShippableImages()
    {
        try
        {
            var userId = _claimService.GetCurrentUserId();
            var shippableImages = await _shippingService.GetShippableImagesByUserId(userId);
            return Ok(ApiResult<IEnumerable<ShippableImageDto>>.Success(
                shippableImages,
                "Lấy danh sách hình ảnh có thể ship thành công"));
        }
        catch (Exception ex)
        {
            _logger.Error($"Lỗi khi lấy danh sách hình ảnh có thể ship: {ex.Message}");
            return StatusCode(500, ApiResult<IEnumerable<ShippableImageDto>>.Error(
                "Có lỗi xảy ra khi lấy danh sách hình ảnh"));
        }
    }

    /// <summary>
    ///     Tạo đơn hàng ship mới
    /// </summary>
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<ApiResult<ShippingOrderDto>>> CreateShippingOrder(
        [FromBody] CreateShippingOrderDto createDto)
    {
        try
        {
            var newOrder = await _shippingService.CreateShippingOrder(createDto);
            return CreatedAtAction(
                nameof(GetShippingOrderById),
                new { id = newOrder.Id },
                ApiResult<ShippingOrderDto>.Success(newOrder, "Tạo đơn hàng ship thành công"));
        }
        catch (KeyNotFoundException ex)
        {
            _logger.Warn($"Không tìm thấy giao dịch: {ex.Message}");
            return NotFound(ApiResult<ShippingOrderDto>.Error(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            _logger.Warn($"Lỗi khi tạo đơn hàng: {ex.Message}");
            return BadRequest(ApiResult<ShippingOrderDto>.Error(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.Error($"Lỗi khi tạo đơn hàng ship: {ex.Message}");
            return StatusCode(500, ApiResult<ShippingOrderDto>.Error("Có lỗi xảy ra khi tạo đơn hàng"));
        }
    }

    /// <summary>
    ///     Lấy danh sách đơn hàng ship của user hiện tại
    /// </summary>
    [HttpGet("user")]
    [Authorize]
    public async Task<ActionResult<ApiResult<IEnumerable<ShippingOrderDto>>>> GetUserShippingOrders()
    {
        try
        {
            var userId = _claimService.GetCurrentUserId();
            var orders = await _shippingService.GetUserShippingOrders(userId);
            return Ok(ApiResult<IEnumerable<ShippingOrderDto>>.Success(
                orders,
                "Lấy danh sách đơn hàng ship thành công"));
        }
        catch (Exception ex)
        {
            _logger.Error($"Lỗi khi lấy danh sách đơn hàng: {ex.Message}");
            return StatusCode(500, ApiResult<IEnumerable<ShippingOrderDto>>.Error(
                "Có lỗi xảy ra khi lấy danh sách đơn hàng"));
        }
    }

    /// <summary>
    ///     Lấy chi tiết một đơn hàng ship
    /// </summary>
    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<ApiResult<ShippingOrderDto>>> GetShippingOrderById(int id)
    {
        try
        {
            var order = await _shippingService.GetShippingOrderById(id);

            // Chỉ cho phép user xem đơn hàng của chính họ hoặc admin xem tất cả
            var userId = _claimService.GetCurrentUserId();
            var userRole = _claimService.GetCurrentUserRole();

            if (order.CustomerId != userId && userRole != "Admin")
                return Forbid(ApiResult<ShippingOrderDto>.Error(
                    "Bạn không có quyền xem đơn hàng này").ToString());

            return Ok(ApiResult<ShippingOrderDto>.Success(
                order,
                "Lấy thông tin đơn hàng ship thành công"));
        }
        catch (KeyNotFoundException ex)
        {
            _logger.Warn($"Không tìm thấy đơn hàng: {ex.Message}");
            return NotFound(ApiResult<ShippingOrderDto>.Error(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.Error($"Lỗi khi lấy chi tiết đơn hàng: {ex.Message}");
            return StatusCode(500, ApiResult<ShippingOrderDto>.Error(
                "Có lỗi xảy ra khi lấy chi tiết đơn hàng"));
        }
    }

    /// <summary>
    ///     Lấy tất cả đơn hàng ship (Admin only)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResult<IEnumerable<ShippingOrderDto>>>> GetAllShippingOrders()
    {
        try
        {
            var orders = await _shippingService.GetAllShippingOrders();
            return Ok(ApiResult<IEnumerable<ShippingOrderDto>>.Success(
                orders,
                "Lấy tất cả đơn hàng ship thành công"));
        }
        catch (Exception ex)
        {
            _logger.Error($"Lỗi khi lấy tất cả đơn hàng: {ex.Message}");
            return StatusCode(500, ApiResult<IEnumerable<ShippingOrderDto>>.Error(
                "Có lỗi xảy ra khi lấy tất cả đơn hàng"));
        }
    }

    /// <summary>
    ///     Cập nhật trạng thái đơn hàng (Admin only)
    /// </summary>
    [HttpPut("status")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResult<ShippingOrderDto>>> UpdateShippingStatus(
        [FromForm] UpdateShippingStatusDto updateDto)
    {
        try
        {
            var updatedOrder = await _shippingService.UpdateShippingOrderStatus(updateDto);
            return Ok(ApiResult<ShippingOrderDto>.Success(
                updatedOrder,
                $"Cập nhật trạng thái đơn hàng thành {updatedOrder.Status} thành công"));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ApiResult<ShippingOrderDto>.Error(
                "Bạn không có quyền cập nhật trạng thái đơn hàng").ToString());
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResult<ShippingOrderDto>.Error(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResult<ShippingOrderDto>.Error(
                "Có lỗi xảy ra khi cập nhật trạng thái đơn hàng"));
        }
    }
}