using ArWoh.API.DTOs.UserDTOs;
using ArWoh.API.Interface;
using ArWoh.API.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArWoh.API.Controllers;

[ApiController]
[Route("api/users")]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IClaimService _claimService;
    private readonly ILoggerService _logger;

    public UserController(IUserService userService, IClaimService claimService, ILoggerService logger)
    {
        _userService = userService;
        _claimService = claimService;
        _logger = logger;
    }


    [HttpGet("profile")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResult<UserProfileDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    [ProducesResponseType(typeof(ApiResult<object>), 401)]
    [ProducesResponseType(typeof(ApiResult<object>), 500)]
    public async Task<IActionResult> GetUserProfile()
    {
        try
        {
            var userId = _claimService.GetCurrentUserId();
            if (userId == null) return Unauthorized(ApiResult<object>.Error("User not authenticated."));

            var currentUser = await _userService.GetUserDetailsById(userId);
            if (currentUser == null) return NotFound(ApiResult<object>.Error("User profile not found."));

            var result = ApiResult<UserProfileDto>.Success(currentUser, "User profile retrieved successfully.");
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResult<object>.Error(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500,
                ApiResult<object>.Error($"An error occurred while retrieving user profile: {ex.Message}"));
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResult<List<UserProfileDto>>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    [ProducesResponseType(typeof(ApiResult<object>), 401)]
    [ProducesResponseType(typeof(ApiResult<object>), 500)]
    public async Task<IActionResult> GetAllUsers()
    {
        try
        {
            var users = await _userService.GetAllUsers();

            return Ok(new ApiResult<List<UserProfileDto>>
            {
                IsSuccess = true,
                Message = "Users retrieved successfully",
                Data = users
            });
        }
        catch (Exception ex)
        {
            _logger.Error($"Error while fetching users: {ex.Message}");
            return StatusCode(500, new ApiResult<object>
            {
                IsSuccess = false,
                Message = "An internal server error occurred",
                Data = null
            });
        }
    }


    [HttpGet("photographers")]
    [ProducesResponseType(typeof(ApiResult<List<UserProfileDto>>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    [ProducesResponseType(typeof(ApiResult<object>), 500)]
    public async Task<IActionResult> GetPhotographers()
    {
        try
        {
            var photographers = await _userService.GetPhotographers();
            if (photographers == null || !photographers.Any())
                return NotFound(ApiResult<object>.Error("No photographers found."));

            var result =
                ApiResult<List<UserProfileDto>>.Success(photographers, "Photographers retrieved successfully.");
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500,
                ApiResult<object>.Error($"An error occurred while retrieving photographers: {ex.Message}"));
        }
    }

    [HttpPut("me/avatar")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResult<UserProfileDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    [ProducesResponseType(typeof(ApiResult<object>), 401)]
    [ProducesResponseType(typeof(ApiResult<object>), 500)]
    public async Task<IActionResult> UpdateAvatar(IFormFile file)
    {
        if (file == null || file.Length == 0) return BadRequest(ApiResult<object>.Error("File is required"));

        try
        {
            var userId = _claimService.GetCurrentUserId();
            var updatedUser = await _userService.UserUpdateAvatar(userId, file);

            return Ok(ApiResult<UserProfileDto>.Success(updatedUser, "User profile updated successfully"));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiResult<object>.Error("User not found"));
        }
        catch (Exception e)
        {
            _logger.Error($"Failed to update user avatar: {e.Message}");
            return StatusCode(500, ApiResult<object>.Error("An error occurred while updating avatar"));
        }
    }
}