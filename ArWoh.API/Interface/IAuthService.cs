using ArWoh.API.DTOs.UserDTOs;
using ArWoh.API.Entities;

namespace ArWoh.API.Interface;

public interface IAuthService
{
    Task<User> RegisterCustomer(UserRegistrationDto registrationDto);
    Task<User> RegisterPhotographer(UserRegistrationDto registrationDto);
    Task<string> Login(UserLoginDto loginDto, IConfiguration configuration);
}