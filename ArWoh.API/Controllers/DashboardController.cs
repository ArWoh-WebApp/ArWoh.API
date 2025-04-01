using ArWoh.API.Interface;
using ArWoh.API.Utils;
using Microsoft.AspNetCore.Authorization; 
using Microsoft.AspNetCore.Mvc;

namespace ArWoh.API.Controllers
{
    [Route("api/dashboard")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly IAdminService _adminService;

        public DashboardController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        [Authorize(Policy = "AdminPolicy")]
        [HttpGet("users/summary")]
        [ProducesResponseType(typeof(ApiResult<object>), 200)]
        [ProducesResponseType(typeof(ApiResult<object>), 500)]
        public async Task<IActionResult> GetUserSummary()
        {
            try
            {
                var data = await _adminService.GetUserSummaryAsync();
                return Ok(ApiResult<object>.Success(data, "User summary retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResult<object>
                {
                    IsSuccess = false,
                    Message = $"An unexpected error occurred: {ex.Message}",
                    Data = null
                });
            }
        }

        [Authorize(Policy = "AdminPolicy")]
        [HttpGet("images/summary")]
        [ProducesResponseType(typeof(ApiResult<object>), 200)]
        [ProducesResponseType(typeof(ApiResult<object>), 500)]
        public async Task<IActionResult> GetImageSummary()
        {
            try
            {
                var data = await _adminService.GetImageSummaryAsync();
                return Ok(ApiResult<object>.Success(data, "Image summary retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResult<object>
                {
                    IsSuccess = false,
                    Message = $"An unexpected error occurred: {ex.Message}",
                    Data = null
                });
            }
        }

        [Authorize(Policy = "AdminPolicy")]
        [HttpGet("revenue/summary")]
        [ProducesResponseType(typeof(ApiResult<object>), 200)]
        [ProducesResponseType(typeof(ApiResult<object>), 500)]
        public async Task<IActionResult> GetRevenueSummary()
        {
            try
            {
                var data = await _adminService.GetRevenueSummaryAsync();
                return Ok(ApiResult<object>.Success(data, "Revenue summary retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResult<object>
                {
                    IsSuccess = false,
                    Message = $"An unexpected error occurred: {ex.Message}",
                    Data = null
                });
            }
        }
    }
}