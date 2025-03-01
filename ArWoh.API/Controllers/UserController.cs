using ArWoh.API.DTOs.UserDTOs;
using ArWoh.API.Interface;
using ArWoh.API.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArWoh.API.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IClaimService _claimService;

        public UserController(IUserService userService, IClaimService claimService)
        {
            _userService = userService;
            _claimService = claimService;
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
                if (userId == null)
                {
                    return Unauthorized(ApiResult<object>.Error("User not authenticated."));
                }

                var currentUser = await _userService.GetUserDetailsById(userId);
                if (currentUser == null)
                {
                    return NotFound(ApiResult<object>.Error("User profile not found."));
                }

                var result = ApiResult<UserProfileDto>.Success(currentUser, "User profile retrieved successfully.");
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ApiResult<object>.Error(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResult<object>.Error($"An error occurred while retrieving user profile: {ex.Message}"));
            }
        }


    }
}
