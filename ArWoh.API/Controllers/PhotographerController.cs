using ArWoh.API.DTOs.ImageDTOs;
using ArWoh.API.DTOs.UserDTOs;
using ArWoh.API.Interface;
using ArWoh.API.Utils;
using Microsoft.AspNetCore.Mvc;

namespace ArWoh.API.Controllers;

[ApiController]
[Route("api/photographers")]
public class PhotographerController : ControllerBase
{
    private readonly IClaimService _claimService;
    private readonly IImageService _imageService;
    private readonly ILoggerService _loggerService;
    private readonly IPaymentService _paymentService;
    private readonly IUserService _userService;

    public PhotographerController(IImageService imageService, ILoggerService loggerService, IClaimService claimService,
        IPaymentService paymentService, IUserService userService)
    {
        _imageService = imageService;
        _loggerService = loggerService;
        _claimService = claimService;
        _paymentService = paymentService;
        _userService = userService;
    }

    [HttpGet("me/images")]
    [ProducesResponseType(typeof(ApiResult<IEnumerable<ImageDto>>), 200)]
    public async Task<IActionResult> GetImagesByPhotographer()
    {
        var photographerId = _claimService.GetCurrentUserId();
        try
        {
            var images = await _imageService.GetImagesUploadedByPhotographer(photographerId);

            if (!images.Any()) return NotFound(ApiResult<object>.Error("No images found for this photographer"));

            return Ok(ApiResult<IEnumerable<ImageDto>>.Success(images, "Images retrieved successfully"));
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

    [HttpGet("{photographerId}/images")]
    [ProducesResponseType(typeof(ApiResult<IEnumerable<ImageDto>>), 200)]
    public async Task<IActionResult> GetImagesByPhotographer(int photographerId)
    {
        if (photographerId <= 0) return BadRequest(ApiResult<object>.Error("Invalid photographer ID"));
        try
        {
            var images = await _imageService.GetImagesUploadedByPhotographer(photographerId);

            if (!images.Any()) return NotFound(ApiResult<object>.Error("No images found for this photographer"));

            return Ok(ApiResult<IEnumerable<ImageDto>>.Success(images, "Images retrieved successfully"));
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


    // [HttpGet("revenue/me")]
    // [Authorize(Policy = "PhotographerPolicy")]
    // [ProducesResponseType(typeof(ApiResult<object>), 200)]
    // public async Task<IActionResult> GetPhotographerRevenue()
    // {
    //     try
    //     {
    //         var photographerId = _claimService.GetCurrentUserId();
    //         var revenue = await _userService.GetPhotographerRevenue(photographerId);
    //         return Ok(ApiResult<object>.Success(revenue));
    //     }
    //     catch (Exception ex)
    //     {
    //         return StatusCode(500, ApiResult<object>.Error("An error occurred while retrieving images"));
    //     }
    // }
}