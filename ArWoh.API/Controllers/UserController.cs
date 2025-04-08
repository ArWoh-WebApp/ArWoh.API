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
    private readonly IClaimService _claimService;
    private readonly ILoggerService _logger;
    private readonly IPaymentService _paymentService;
    private readonly IUserService _userService;

    public UserController(IUserService userService, IClaimService claimService, ILoggerService logger,
        IPaymentService paymentService)
    {
        _userService = userService;
        _claimService = claimService;
        _logger = logger;
        _paymentService = paymentService;
    }

    [HttpGet("photographers")]
    [ProducesResponseType(typeof(ApiResult<List<UserProfileDto>>), 200)]
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

    [HttpGet("me/transactions")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResult<UserProfileDto>), 200)]
    public async Task<IActionResult> GetMyTransactions()
    {
        try
        {
            var userId = _claimService.GetCurrentUserId();
            var transactions = _userService.GetUserTransactions(userId);

            return Ok(ApiResult<object>.Success(transactions, "Transactions retrieved successfully"));
        }
        catch (Exception e)
        {
            return Ok(ApiResult<object>.Error(e.Message));
        }
    }

    [HttpGet("me/profile")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResult<UserProfileDto>), 200)]
    public async Task<IActionResult> GetMyProfile()
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

    [HttpPut("me/profile")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResult<UserProfileDto>), 200)]
    public async Task<IActionResult> UpdateMyProfile([FromBody] UserUpdateDto updateDto)
    {
        if (updateDto == null)
            return BadRequest(ApiResult<object>.Error("User update data is required"));

        try
        {
            var userId = _claimService.GetCurrentUserId();
            if (userId <= 0)
                return Unauthorized(ApiResult<object>.Error("User not authenticated."));

            var updatedUser = await _userService.UpdateUserInfo(userId, updateDto);
            return Ok(ApiResult<UserProfileDto>.Success(updatedUser, "User profile updated successfully"));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiResult<object>.Error("User not found"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResult<object>.Error(ex.Message));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResult<object>.Error(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to update user profile: {ex.Message}");
            return StatusCode(500, ApiResult<object>.Error("An error occurred while updating user profile"));
        }
    }

    [HttpPut("me/avatar")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResult<UserProfileDto>), 200)]
    public async Task<IActionResult> UpdateMyAvatar(IFormFile file)
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