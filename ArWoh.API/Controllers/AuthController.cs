using System.Security.Claims;
using ArWoh.API.DTOs.UserDTOs;
using ArWoh.API.Entities;
using ArWoh.API.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VaccinaCare.Application.Ultils;

namespace ArWoh.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase

{
    private readonly IAuthService _authService;
    private readonly ILoggerService _loggerService;

    public AuthController(IAuthService authService, ILoggerService loggerService)
    {
        _authService = authService;
        _loggerService = loggerService;
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResult<User>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    [ProducesResponseType(typeof(ApiResult<object>), 500)]
    public async Task<IActionResult> Register([FromBody] UserRegistrationDto registerDTO)
    {
        try
        {
            if (registerDTO == null || string.IsNullOrWhiteSpace(registerDTO.Email) ||
                string.IsNullOrWhiteSpace(registerDTO.Password))
            {
                return BadRequest(new ApiResult<object> { IsSuccess = false, Message = "Invalid registration data." });
            }

            var user = await _authService.Register(registerDTO);

            if (user == null)
            {
                return BadRequest(new ApiResult<object> { IsSuccess = false, Message = "Email is already in use." });
            }

            return Ok(new ApiResult<User>
            {
                IsSuccess = true,
                Message = "User registered successfully.",
                Data = user
            });
        }
        catch (Exception ex)
        {
            _loggerService.Error($"Registration error: {ex.Message}");
            return StatusCode(500,
                new ApiResult<object>
                {
                    IsSuccess = false,
                    Message = "An error occurred while processing the request."
                });
        }
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResult<string>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    [ProducesResponseType(typeof(ApiResult<object>), 500)]
    public async Task<IActionResult> Login([FromBody] UserLoginDto loginDto,
        [FromServices] IConfiguration configuration)
    {
        try
        {
            if (loginDto == null || string.IsNullOrWhiteSpace(loginDto.Email) ||
                string.IsNullOrWhiteSpace(loginDto.Password))
            {
                return BadRequest(new ApiResult<object> { IsSuccess = false, Message = "Invalid login data." });
            }

            var token = await _authService.Login(loginDto, configuration);

            if (string.IsNullOrEmpty(token))
            {
                return Unauthorized(new ApiResult<object>
                    { IsSuccess = false, Message = "Invalid email or password." });
            }

            return Ok(new ApiResult<string>
            {
                IsSuccess = true,
                Message = "Login successful.",
                Data = token
            });
        }
        catch (Exception ex)
        {
            _loggerService.Error($"Login error: {ex.Message}");
            return StatusCode(500, new ApiResult<object>
            {
                IsSuccess = false,
                Message = "An error occurred while processing the request."
            });
        }
    }
    
    
    [HttpGet("check-role")]
    [Authorize] // Requires authentication
    public IActionResult CheckUserRole()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(userRole))
            {
                return Unauthorized(new ApiResult<object>
                {
                    IsSuccess = false,
                    Message = "Invalid token or user information."
                });
            }

            return Ok(new ApiResult<object>
            {
                IsSuccess = true,
                Message = "User role retrieved successfully.",
                Data = new { UserId = userId, Role = userRole }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResult<object>
            {
                IsSuccess = false,
                Message = "An error occurred while processing the request."
            });
        }
    }
    
    [HttpGet("User-access")]
    [Authorize(Policy = "UserPolicy")]
    public IActionResult CustomerAccess()
    {
        return Ok(new ApiResult<object>
        {
            IsSuccess = true,
            Message = "Welcome, Customer!",
            Data = "This is a protected customer-only route."
        });
    }

    


}