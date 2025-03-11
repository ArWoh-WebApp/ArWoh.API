using ArWoh.API.DTOs.ImageDTOs;
using ArWoh.API.Interface;
using ArWoh.API.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArWoh.API.Controllers;

[ApiController]
[Route("api/images")]
public class ImageController : ControllerBase
{
    private readonly IImageService _imageService;
    private readonly ILoggerService _loggerService;
    private readonly IClaimService _claimService;

    public ImageController(IImageService imageService, IClaimService claimService, ILoggerService loggerService)
    {
        _imageService = imageService;
        _claimService = claimService;
        _loggerService = loggerService;
    }


    [HttpGet]
    [ProducesResponseType(typeof(ApiResult<IEnumerable<ImageDto>>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    [ProducesResponseType(typeof(ApiResult<object>), 500)]
    public async Task<IActionResult> GetAllImages()
    {
        try
        {
            _loggerService.Info("Fetching all images via API.");

            var images = await _imageService.GetAllImages();

            if (!images.Any())
            {
                _loggerService.Warn("No images found.");
                return Ok(ApiResult<IEnumerable<ImageDto>>.Success(new List<ImageDto>()));
            }

            _loggerService.Success($"Successfully retrieved {images.Count()} images.");
            return Ok(ApiResult<IEnumerable<ImageDto>>.Success(images));
        }
        catch (Exception ex)
        {
            _loggerService.Error($"Unexpected error in GetAllImages: {ex.Message}");
            return StatusCode(500, ApiResult<object>.Error("An unexpected error occurred"));
        }
    }

    [HttpGet("bought-by-user")]
    [ProducesResponseType(typeof(ApiResult<IEnumerable<ImageDto>>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    [ProducesResponseType(typeof(ApiResult<object>), 500)]
    public async Task<IActionResult> GetAllImagesBoughtByUser()
    {
        try
        {
            var userId = _claimService.GetCurrentUserId();
            var images = await _imageService.GetAllImagesBoughtByUser(userId);

            return Ok(ApiResult<IEnumerable<ImageDto>>.Success(images));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResult<object>.Error("An error occurred while processing your request."));
        }
    }


    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResult<ImageDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    [ProducesResponseType(typeof(ApiResult<object>), 500)]
    public async Task<IActionResult> GetImageDetails(int id)
    {
        try
        {
            var imageDetails = await _imageService.GetImageById(id);
            return Ok(ApiResult<ImageDto>.Success(imageDetails));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResult<object>.Error("Image not found"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResult<object>.Error("An unexpected error occurred"));
        }
    }


    [HttpPost("upload")]
    [Authorize(Policy = "PhotographerPolicy")]
    [ProducesResponseType(typeof(ApiResult<ImageDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    [ProducesResponseType(typeof(ApiResult<object>), 500)]
    public async Task<IActionResult> UploadImage([FromForm] UploadImageDto uploadDto)
    {
        if (uploadDto == null || uploadDto.File == null || uploadDto.File.Length == 0)
            return BadRequest(new ApiResult<object>
            {
                IsSuccess = false,
                Message = "No file uploaded."
            });

        try
        {
            var photographerId = _claimService.GetCurrentUserId();
            if (photographerId == 0)
                return Unauthorized(new ApiResult<object>
                {
                    IsSuccess = false,
                    Message = "User authentication failed."
                });

            var image = await _imageService.UploadImageAsync(uploadDto, photographerId);

            return Ok(new ApiResult<ImageDto>
            {
                IsSuccess = true,
                Message = "Image uploaded successfully.",
                Data = image
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ApiResult<object>
            {
                IsSuccess = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResult<object>
            {
                IsSuccess = false,
                Message = "An error occurred while uploading the image."
            });
        }
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "PhotographerPolicy")]
    [ProducesResponseType(typeof(ApiResult<ImageDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    [ProducesResponseType(typeof(ApiResult<object>), 404)]
    [ProducesResponseType(typeof(ApiResult<object>), 500)]
    public async Task<IActionResult> UpdateImage(int id, [FromBody] UpdateImageDto updateDto)
    {
        try
        {
            var updatedImage = await _imageService.UpdateImageAsync(id, updateDto);
            return Ok(ApiResult<ImageDto>.Success(updatedImage));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResult<object>.Error("Image not found"));
        }
        catch (Exception ex)
        {
            _loggerService.Error($"Unexpected error in UpdateImage: {ex.Message}");
            return StatusCode(500, ApiResult<object>.Error("An unexpected error occurred"));
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "PhotographerPolicy")]
    [ProducesResponseType(typeof(ApiResult<bool>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    [ProducesResponseType(typeof(ApiResult<object>), 404)]
    [ProducesResponseType(typeof(ApiResult<object>), 500)]
    public async Task<IActionResult> DeleteImage(int id)
    {
        try
        {
            var result = await _imageService.DeleteImageAsync(id);
            return Ok(ApiResult<bool>.Success(result));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResult<object>.Error("Image not found"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResult<object>.Error(ex.Message));
        }
        catch (Exception ex)
        {
            _loggerService.Error($"Unexpected error in DeleteImage: {ex.Message}");
            return StatusCode(500, ApiResult<object>.Error("An unexpected error occurred"));
        }
    }
}