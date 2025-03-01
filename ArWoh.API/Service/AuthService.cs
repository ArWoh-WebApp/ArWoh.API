using ArWoh.API.DTOs.UserDTOs;
using ArWoh.API.Entities;
using ArWoh.API.Enums;
using ArWoh.API.Interface;
using ArWoh.API.Utils;
using VaccinaCare.Application.Ultils;

namespace ArWoh.API.Service;

public class AuthService : IAuthService
{
    private readonly ILoggerService _loggerService;
    private readonly IUnitOfWork _unitOfWork;

    public AuthService(ILoggerService loggerService, IUnitOfWork unitOfWork)
    {
        _loggerService = loggerService;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Tạo tài khoản cho customer
    /// </summary>
    /// <param name="registrationDto"></param>
    /// <returns></returns>
    public async Task<User> RegisterCustomer(UserRegistrationDto registrationDto)
    {
        try
        {
            // Kiểm tra xem email đã tồn tại chưa
            var existingUser = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == registrationDto.Email);
            if (existingUser != null)
            {
                return null; // Email đã được sử dụng
            }

            // Băm mật khẩu
            var passwordHasher = new PasswordHasher();
            string passwordHash = passwordHasher.HashPassword(registrationDto.Password);

            // Tạo username từ email nếu không có username được cung cấp
            string username = string.IsNullOrWhiteSpace(registrationDto.Username)
                ? registrationDto.Email.Split('@')[0]
                : registrationDto.Username;

            var user = new User
            {
                Username = username,
                Email = registrationDto.Email,
                PasswordHash = passwordHash,
                Role = UserRole.Customer, // Đăng ký với vai trò Customer
                Bio = registrationDto.Bio, // Mô tả ngắn (có thể null)
                ProfilePictureUrl = registrationDto.ProfilePictureUrl // Ảnh đại diện (có thể null)
            };

            // Thêm user vào database
            await _unitOfWork.Users.AddAsync(user);
            await _unitOfWork.CompleteAsync();

            return user;
        }
        catch (Exception ex)
        {
            _loggerService.Error($"Registration failed: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Tạo tài khoản cho photographer
    /// </summary>
    /// <param name="registrationDto"></param>
    /// <returns></returns>
    public async Task<User> RegisterPhotographer(UserRegistrationDto registrationDto)
    {
        try
        {
            // Kiểm tra xem email đã tồn tại chưa
            var existingUser = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == registrationDto.Email);
            if (existingUser != null)
            {
                return null; // Email đã được sử dụng
            }

            // Băm mật khẩu
            var passwordHasher = new PasswordHasher();
            string passwordHash = passwordHasher.HashPassword(registrationDto.Password);

            // Tạo username từ email nếu không có username được cung cấp
            string username = string.IsNullOrWhiteSpace(registrationDto.Username)
                ? registrationDto.Email.Split('@')[0]
                : registrationDto.Username;

            var user = new User
            {
                Username = username,
                Email = registrationDto.Email,
                PasswordHash = passwordHash,
                Role = UserRole.Photographer, // Đăng ký với vai trò Photographer
                Bio = registrationDto.Bio, // Mô tả ngắn (có thể null)
                ProfilePictureUrl = registrationDto.ProfilePictureUrl // Ảnh đại diện (có thể null)
            };

            // Thêm user vào database
            await _unitOfWork.Users.AddAsync(user);
            await _unitOfWork.CompleteAsync();

            return user;
        }
        catch (Exception ex)
        {
            _loggerService.Error($"Registration failed: {ex.Message}");
            return null;
        }
    }

    public async Task<string> Login(UserLoginDto loginDto, IConfiguration configuration)
    {
        try
        {
            // Find the user by email
            var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == loginDto.Email);
            if (user == null)
            {
                return null; // User does not exist
            }

            var passwordHasher = new PasswordHasher();
            bool isPasswordValid = passwordHasher.VerifyPassword(loginDto.Password, user.PasswordHash);

            if (!isPasswordValid)
            {
                return null; // Incorrect password
            }

            // Generate JWT token
            string token = JwtUtils.GenerateJwtToken(
                user.Id,
                user.Email,
                user.Role.ToString(),
                configuration,
                TimeSpan.FromHours(2) // Token validity (2 hours)
            );

            return token;
        }
        catch (Exception ex)
        {
            _loggerService.Error($"Login failed: {ex.Message}");
            return null;
        }
    }
}