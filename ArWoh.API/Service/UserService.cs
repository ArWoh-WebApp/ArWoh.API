using ArWoh.API.DTOs.UserDTOs;
using ArWoh.API.Entities;
using ArWoh.API.Enums;
using ArWoh.API.Interface;

namespace ArWoh.API.Service;

public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILoggerService _logger;
    private readonly IBlobService _blobService;

    public UserService(IUnitOfWork unitOfWork, ILoggerService loggerService, IBlobService blobService)
    {
        _unitOfWork = unitOfWork;
        _logger = loggerService;
        _blobService = blobService;
    }

    public async Task<UserProfileDto> UserUpdateAvatar(int userId, IFormFile file)
    {
        if (file == null || file.Length == 0) throw new ArgumentException("File is required");

        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null) throw new KeyNotFoundException("User not found");

        // Tạo tên file duy nhất
        var fileName = $"user-avatars/{userId}_{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";

        // Upload file lên Blob Storage
        using (var stream = file.OpenReadStream())
        {
            await _blobService.UploadFileAsync(fileName, stream);
        }

        // Lấy URL của file
        var fileUrl = await _blobService.GetFileUrlAsync(fileName);

        // Cập nhật URL ảnh đại diện trong User
        user.ProfilePictureUrl = fileUrl;
        user.UpdatedAt = DateTime.Now;
        _unitOfWork.Users.Update(user);
        await _unitOfWork.CompleteAsync();

        // Trả về thông tin user đã cập nhật
        return new UserProfileDto
        {
            UserId = user.Id,
            Username = user.Username,
            ProfilePictureUrl = user.ProfilePictureUrl
        };
    }

    public async Task<UserProfileDto> GetUserDetailsById(int userId)
    {
        if (userId == null)
        {
            _logger.Warn("Attempted to fetch user with an empty GUID.");
            throw new ArgumentException("invalid userId");
        }

        try
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);

            if (user.Id == null)
            {
                _logger.Warn($"No user found with ID: {userId}");
                throw new KeyNotFoundException($"User with ID {userId} not found.");
            }

            _logger.Info($"Successfully fetched user with ID: {userId}.");

            return new UserProfileDto
            {
                Username = user.Username,
                Email = user.Email,
                Role = user.Role,
                Bio = user.Bio,
                ProfilePictureUrl = user.ProfilePictureUrl
            };
        }
        catch (KeyNotFoundException knfEx)
        {
            _logger.Error($"User retrieval error: {knfEx.Message}");
            throw;
        }
        catch (ArgumentException argEx)
        {
            _logger.Error($"Invalid argument: {argEx.Message}");
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error($"An unexpected error occurred while fetching user details for ID {userId}: {ex.Message}");
            throw;
        }
    }

    public async Task<List<UserProfileDto>> GetAllUsers()
    {
        try
        {
            var users = await _unitOfWork.Users.GetAllAsync();

            if (users == null || !users.Any())
            {
                _logger.Warn("No users found in the database.");
                return new List<UserProfileDto>();
            }

            _logger.Info($"Successfully retrieved {users.Count()} users.");

            return users.Select(user => new UserProfileDto
            {
                UserId = user.Id,
                Username = user.Username,
                Email = user.Email,
                Bio = user.Bio,
                ProfilePictureUrl = user.ProfilePictureUrl
            }).ToList();
        }
        catch (Exception e)
        {
            _logger.Error($"An error occurred while fetching all users: {e.Message}");
            throw;
        }
    }

    public async Task<List<UserProfileDto>> GetPhotographers()
    {
        try
        {
            var users = await _unitOfWork.Users.FindAsync(u => u.Role == UserRole.Photographer);

            if (users == null || !users.Any())
            {
                _logger.Warn("No users found with the Photographer role.");
                return new List<UserProfileDto>();
            }

            _logger.Info($"Successfully fetched {users.Count()} photographers.");

            return users.Select(user => new UserProfileDto
            {
                Username = user.Username,
                Email = user.Email,
                Bio = user.Bio,
                ProfilePictureUrl = user.ProfilePictureUrl
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.Error($"An unexpected error occurred while fetching photographers: {ex.Message}");
            throw;
        }
    }
}