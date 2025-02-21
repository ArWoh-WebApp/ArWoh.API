using ArWoh.API.DTOs.ImageDTOs;
using ArWoh.API.Entities;
using ArWoh.API.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VaccinaCare.Application.Ultils;

namespace ArWoh.API.Controllers;

[ApiController]
[Route("api/images")]
public class ImageController : ControllerBase
{
    private readonly IImageService _imageService;
    private readonly IClaimService _claimService;

    public ImageController(IImageService imageService, IClaimService claimService)
    {
        _imageService = imageService;
        _claimService = claimService;
    }

    [HttpPost("upload")]
    [Authorize(Policy = "PhotographerPolicy")]
    [ProducesResponseType(typeof(ApiResult<Image>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    [ProducesResponseType(typeof(ApiResult<object>), 500)]
    public async Task<IActionResult> UploadImage([FromForm] UploadImageDto uploadDto)
    {
        if (uploadDto == null || uploadDto.File == null || uploadDto.File.Length == 0)
        {
            return BadRequest(new ApiResult<object>
            {
                IsSuccess = false,
                Message = "No file uploaded."
            });
        }

        try
        {
            var photographerId = _claimService.GetCurrentUserId();
            if (photographerId == 0)
            {
                return Unauthorized(new ApiResult<object>
                {
                    IsSuccess = false,
                    Message = "User authentication failed."
                });
            }

            var image = await _imageService.UploadImageAsync(uploadDto, photographerId);

            return Ok(new ApiResult<Image>
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
}