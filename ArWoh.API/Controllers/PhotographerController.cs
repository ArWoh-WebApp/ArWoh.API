using ArWoh.API.DTOs.ImageDTOs;
using ArWoh.API.Interface;
using ArWoh.API.Utils;
using Microsoft.AspNetCore.Mvc;

namespace ArWoh.API.Controllers;

[ApiController]
[Route("api/photographers")]
public class PhotographerController : ControllerBase
{
    private readonly IImageService _imageService;
    private readonly ILoggerService _loggerService;
    private readonly IClaimService _claimService;


    public PhotographerController(IImageService imageService, ILoggerService loggerService, IClaimService claimService)
    {
        _imageService = imageService;
        _loggerService = loggerService;
        _claimService = claimService;
    }

    [HttpGet("{photographerId}/images")]
    [ProducesResponseType(typeof(ApiResult<IEnumerable<ImageDto>>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    [ProducesResponseType(typeof(ApiResult<object>), 404)]
    [ProducesResponseType(typeof(ApiResult<object>), 500)]
    public async Task<IActionResult> GetImagesByPhotographer(int photographerId)
    {
        if (photographerId <= 0) return BadRequest(ApiResult<object>.Error("Invalid photographer ID"));

        try
        {
            var images = await _imageService.GetImagesByPhotographer(photographerId);

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
            _loggerService.Error($"Error retrieving images for photographer {photographerId}: {e.Message}");
            return StatusCode(500, ApiResult<object>.Error("An error occurred while retrieving images"));
        }
    }
}