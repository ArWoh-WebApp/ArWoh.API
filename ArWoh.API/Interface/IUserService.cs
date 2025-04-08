using ArWoh.API.DTOs.UserDTOs;

namespace ArWoh.API.Interface;

public interface IUserService
{
    Task<UserProfileDto> GetUserDetailsById(int userId);

    Task<UserProfileDto> GetPhotographerProfile(int photographerId);
    // Task<IEnumerable<PaymentTransaction>> GetUserTransactions(int userId);
    // Task<RevenueDto> GetPhotographerRevenue(int photographerId);

    Task<List<UserProfileDto>> GetAllUsers();
    Task<List<UserProfileDto>> GetPhotographers();
    Task<UserProfileDto> UserUpdateAvatar(int userId, IFormFile file);
    Task<UserProfileDto> UpdateUserInfo(int userId, UserUpdateDto updateDto);
}