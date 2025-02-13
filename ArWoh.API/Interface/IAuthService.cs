using ArWoh.API.DTOs.UserDTOs;
using ArWoh.API.Entities;

namespace ArWoh.API.Interface;

public interface IAuthService
{
    Task<User> Register(UserRegistrationDto registrationDto);
}