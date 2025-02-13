using ArWoh.API.DTOs.UserDTOs;
using ArWoh.API.Entities;
using ArWoh.API.Interface;
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

}