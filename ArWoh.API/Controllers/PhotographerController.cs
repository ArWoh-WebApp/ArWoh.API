using ArWoh.API.Commons;
using ArWoh.API.DTOs.ImageDTOs;
using ArWoh.API.DTOs.UserDTOs;
using ArWoh.API.Interface;
using ArWoh.API.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArWoh.API.Controllers;

[ApiController]
[Route("api/photographers")]
public class PhotographerController : ControllerBase
{
    private readonly IClaimService _claimService;
    private readonly IImageService _imageService;
    private readonly ILoggerService _loggerService;
    private readonly IOrderService _orderService;
    private readonly IPaymentService _paymentService;
    private readonly IUserService _userService;

    public PhotographerController(IImageService imageService, ILoggerService loggerService, IClaimService claimService,
        IPaymentService paymentService, IUserService userService, IOrderService orderService)
    {
        _imageService = imageService;
        _loggerService = loggerService;
        _claimService = claimService;
        _paymentService = paymentService;
        _userService = userService;
        _orderService = orderService;
    }

    [HttpGet("me/images")]
    [ProducesResponseType(typeof(ApiResult<Pagination<ImageDto>>), 200)]
    public async Task<IActionResult> GetImagesByPhotographer([FromQuery] PaginationParameter paginationParams)
    {
        var photographerId = _claimService.GetCurrentUserId();
        try
        {
            var paginatedImages = await _imageService.GetImagesUploadedByPhotographer(photographerId, paginationParams);

            if (paginatedImages.Count == 0 && paginationParams.PageIndex == 1)
                return NotFound(ApiResult<object>.Error("No images found for this photographer"));

            return Ok(ApiResult<Pagination<ImageDto>>.Success(paginatedImages, "Images retrieved successfully"));
        }
        catch (KeyNotFoundException e)
        {
            _loggerService.Error(e.Message);
            return NotFound(ApiResult<object>.Error(e.Message));
        }
        catch (Exception e)
        {
            _loggerService.Error($"Error retrieving images: {e.Message}");
            return StatusCode(500, ApiResult<object>.Error("An error occurred while retrieving images"));
        }
    }

    [HttpGet("{photographerId}/images")]
    [ProducesResponseType(typeof(ApiResult<Pagination<ImageDto>>), 200)]
    public async Task<IActionResult> GetImagesByPhotographer(int photographerId,
        [FromQuery] PaginationParameter paginationParams)
    {
        if (photographerId <= 0) return BadRequest(ApiResult<object>.Error("Invalid photographer ID"));
        try
        {
            var paginatedImages = await _imageService.GetImagesUploadedByPhotographer(photographerId, paginationParams);

            if (paginatedImages.Count == 0 && paginationParams.PageIndex == 1)
                return NotFound(ApiResult<object>.Error("No images found for this photographer"));

            return Ok(ApiResult<Pagination<ImageDto>>.Success(paginatedImages, "Images retrieved successfully"));
        }
        catch (KeyNotFoundException e)
        {
            _loggerService.Error(e.Message);
            return NotFound(ApiResult<object>.Error(e.Message));
        }
        catch (Exception e)
        {
            _loggerService.Error($"Error retrieving images: {e.Message}");
            return StatusCode(500, ApiResult<object>.Error("An error occurred while retrieving images"));
        }
    }


    [HttpGet("{photographerId}/profile")]
    [ProducesResponseType(typeof(ApiResult<UserProfileDto>), 200)]
    public async Task<IActionResult> GetPhotographerProfile(int photographerId)
    {
        try
        {
            var photographer = await _userService.GetPhotographerProfile(photographerId);

            if (photographer == null) return NotFound(ApiResult<object>.Error("No photographer found "));

            return Ok(ApiResult<UserProfileDto>.Success(photographer, "photographer retrieved successfully"));
        }
        catch (KeyNotFoundException e)
        {
            _loggerService.Error(e.Message);
            return NotFound(ApiResult<object>.Error(e.Message));
        }
        catch (Exception e)
        {
            return StatusCode(500, ApiResult<object>.Error("An error occurred while retrieving images"));
        }
    }

    /// <summary>
    ///     lấy doanh thu của 1 photographer
    /// </summary>
    [HttpGet("revenue/me")]
    [Authorize(Policy = "PhotographerPolicy")]
    [ProducesResponseType(typeof(ApiResult<object>), 200)]
    public async Task<IActionResult> GetPhotographerRevenue()
    {
        try
        {
            var photographerId = _claimService.GetCurrentUserId();
            var revenue = await _orderService.GetPhotographerRevenue(photographerId);
            return Ok(ApiResult<object>.Success(revenue));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResult<object>.Error("An error occurred while retrieving images"));
        }
    }
}