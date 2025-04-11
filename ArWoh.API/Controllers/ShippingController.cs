using ArWoh.API.DTOs.ShippingDTOs;
using ArWoh.API.Interface;
using ArWoh.API.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArWoh.API.Controllers;

[ApiController]
[Route("api/deliveries")]
public class ShippingController : ControllerBase
{
    private readonly IClaimService _claimService;
    private readonly IShippingService _shippingService;

    public ShippingController(IShippingService shippingService, IClaimService claimService)
    {
        _shippingService = shippingService;
        _claimService = claimService;
    }

    [HttpGet("shippable-images")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResult<IEnumerable<ShippableImageDto>>), 200)]
    [ProducesResponseType(typeof(ApiResult<string>), 400)]
    [ProducesResponseType(typeof(ApiResult<string>), 500)]
    public async Task<IActionResult> GetShippableImages()
    {
        try
        {
            // Lấy userId từ token JWT hiện tại
            var userId = _claimService.GetCurrentUserId();
            var shippableImages = await _shippingService.GetShippableImagesByUserId(userId);

            // Trả về kết quả thành công
            return Ok(ApiResult<IEnumerable<ShippableImageDto>>.Success(
                shippableImages,
                "Shippable images retrieved successfully"));
        }
        catch (Exception ex)
        {
            // Xử lý lỗi và trả về response lỗi
            return StatusCode(500, ApiResult<string>.Error($"An error occurred: {ex.Message}"));
        }
    }
}