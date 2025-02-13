using ArWoh.API.DTOs.UserDTOs;
using ArWoh.API.Entities;
using ArWoh.API.Interface;
using Microsoft.AspNetCore.Identity;
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
    
    
    public async Task<User> Register(UserRegistrationDto registrationDto)
    {
        try
        {
            // Check if the email is already in use
            var existingUser = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == registrationDto.Email);
            if (existingUser != null)
            {
                return null; // Email already exists
            }

            var passwordHasher = new PasswordHasher();
            string passwordHash = passwordHasher.HashPassword(registrationDto.Password);

            string username = string.IsNullOrWhiteSpace(registrationDto.Username)
                ? registrationDto.Email.Split('@')[0] 
                : registrationDto.Username;

            var user = new User
            {
                Username = username, 
                Email = registrationDto.Email,
                PasswordHash = passwordHash
            };

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

    
    
}