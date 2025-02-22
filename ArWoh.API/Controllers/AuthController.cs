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

    [HttpPost("register/customer")]
    [ProducesResponseType(typeof(ApiResult<User>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    [ProducesResponseType(typeof(ApiResult<object>), 500)]
    public async Task<IActionResult> RegisterCustomer([FromBody] UserRegistrationDto registerDTO)
    {
        try
        {
            if (registerDTO == null)
            {
                return BadRequest(new ApiResult<object>
                {
                    IsSuccess = false,
                    Message = "Registration data is missing."
                });
            }

            if (string.IsNullOrWhiteSpace(registerDTO.Email) || string.IsNullOrWhiteSpace(registerDTO.Password))
            {
                return BadRequest(new ApiResult<object>
                {
                    IsSuccess = false,
                    Message = "Email and password are required."
                });
            }

            // Kiểm tra xem email đã tồn tại chưa
            var user = await _authService.RegisterCustomer(registerDTO);
            if (user == null)
            {
                return BadRequest(new ApiResult<object>
                {
                    IsSuccess = false,
                    Message = "Email is already in use."
                });
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
            return StatusCode(500, new ApiResult<object>
            {
                IsSuccess = false,
                Message = "An error occurred while processing the request."
            });
        }
    }
    
    [HttpPost("register/photographer")]
    [ProducesResponseType(typeof(ApiResult<User>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    [ProducesResponseType(typeof(ApiResult<object>), 500)]
    public async Task<IActionResult> RegisterPhotographer([FromBody] UserRegistrationDto registerDTO)
    {
        try
        {
            if (registerDTO == null)
            {
                return BadRequest(new ApiResult<object>
                {
                    IsSuccess = false,
                    Message = "Registration data is missing."
                });
            }

            if (string.IsNullOrWhiteSpace(registerDTO.Email) || string.IsNullOrWhiteSpace(registerDTO.Password))
            {
                return BadRequest(new ApiResult<object>
                {
                    IsSuccess = false,
                    Message = "Email and password are required."
                });
            }

            // Kiểm tra xem email đã tồn tại chưa
            var user = await _authService.RegisterPhotographer(registerDTO);
            if (user == null)
            {
                return BadRequest(new ApiResult<object>
                {
                    IsSuccess = false,
                    Message = "Email is already in use."
                });
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
            return StatusCode(500, new ApiResult<object>
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
}