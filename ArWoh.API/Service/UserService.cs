using ArWoh.API.DTOs.PaymentDTOs;
using ArWoh.API.DTOs.UserDTOs;
using ArWoh.API.Entities;
using ArWoh.API.Enums;
using ArWoh.API.Interface;

namespace ArWoh.API.Service;

public class UserService : IUserService
{
    private readonly IBlobService _blobService;
    private readonly ILoggerService _logger;
    private readonly IUnitOfWork _unitOfWork;

    public UserService(IUnitOfWork unitOfWork, ILoggerService loggerService, IBlobService blobService)
    {
        _unitOfWork = unitOfWork;
        _logger = loggerService;
        _blobService = blobService;
    }

    public async Task<RevenueDto> GetPhotographerRevenue(int photographerId)
    {
        _logger.Info($"Starting GetPhotographerRevenue for photographerId: {photographerId}");

        try
        {
            if (photographerId <= 0)
            {
                _logger.Warn($"Invalid photographer ID: {photographerId}");
                throw new ArgumentException("Invalid photographer ID");
            }

            // Get images by photographer
            _logger.Info($"Fetching images for photographerId: {photographerId}");
            var images = await _unitOfWork.Images.FindAsync(i => i.PhotographerId == photographerId);

            if (images == null || !images.Any())
            {
                _logger.Warn($"No images found for photographerId: {photographerId}");
                throw new KeyNotFoundException("No images found for this photographer");
            }

            _logger.Info($"Found {images.Count()} images for photographerId: {photographerId}");
            var imageIds = images.Select(i => i.Id).ToList();
            _logger.Info($"Image IDs: {string.Join(", ", imageIds)}");

            // Get transactions
            _logger.Info($"Fetching transactions for imageIds: {string.Join(", ", imageIds)}");
            var transactions = await _unitOfWork.PaymentTransactions.FindAsync(t => imageIds.Contains(t.ImageId));
            _logger.Info($"Found {transactions.Count()} total transactions");

            // Filter for completed transactions
            var completedTransactions = transactions
                .Where(t => (int)t.PaymentStatus == (int)PaymentTransactionStatusEnum.COMPLETED).ToList();
            _logger.Info($"Found {completedTransactions.Count} completed transactions");

            foreach (var tx in completedTransactions)
                _logger.Info(
                    $"Transaction ID: {tx.Id}, ImageId: {tx.ImageId}, Amount: {tx.Amount}, Status: {tx.PaymentStatus}");

            var result = new RevenueDto
            {
                TotalRevenue = completedTransactions.Sum(t => t.Amount),
                TotalImagesSold = completedTransactions.Select(t => t.ImageId).Distinct().Count()
            };

            _logger.Info($"Calculated TotalRevenue: {result.TotalRevenue}, TotalImagesSold: {result.TotalImagesSold}");

            // Dictionary for image lookup
            var imageDict = images.ToDictionary(i => i.Id);

            // Group transactions by image
            var groupedTransactions = completedTransactions
                .GroupBy(t => t.ImageId)
                .ToDictionary(g => g.Key, g => g.ToList());

            _logger.Info($"Grouped transactions by image: {groupedTransactions.Count} images have transactions");

            // Process each image with transactions
            foreach (var imageId in imageIds)
            {
                _logger.Info($"Processing ImageId: {imageId}");

                if (groupedTransactions.TryGetValue(imageId, out var imageTrans) && imageTrans.Any())
                {
                    var image = imageDict[imageId];
                    var detail = new ImageSalesDetail
                    {
                        ImageId = imageId,
                        ImageTitle = image.Title ?? "Untitled",
                        ImageUrl = image.Url ?? "",
                        SalesCount = imageTrans.Count,
                        TotalAmount = imageTrans.Sum(t => t.Amount)
                    };

                    _logger.Info(
                        $"Added detail for ImageId: {imageId}, Title: {detail.ImageTitle}, SalesCount: {detail.SalesCount}, TotalAmount: {detail.TotalAmount}");
                    result.ImageSales.Add(detail);
                }
                else
                {
                    _logger.Info($"No completed transactions found for ImageId: {imageId}");
                }
            }

            // Sort by sales count
            result.ImageSales = result.ImageSales.OrderByDescending(i => i.SalesCount).ToList();
            _logger.Success($"Successfully retrieved revenue details for photographerId: {photographerId}");

            return result;
        }
        catch (Exception ex)
        {
            _logger.Error($"Error in GetPhotographerRevenue: {ex.Message}, Stack: {ex.StackTrace}");
            throw new Exception($"Error retrieving photographer revenue details: {ex.Message}", ex);
        }
    }

    public async Task<IEnumerable<PaymentTransaction>> GetUserTransactions(int userId)
    {
        try
        {
            if (userId <= 0)
                throw new ArgumentException("Invalid user ID");

            var transactions = await _unitOfWork.PaymentTransactions.FindAsync(t => t.CustomerId == userId);

            if (transactions == null || !transactions.Any())
                throw new KeyNotFoundException("No transactions found for this user");

            return transactions;
        }
        catch (Exception ex)
        {
            // Log lỗi tại đây nếu có hệ thống logging
            throw new Exception($"Error retrieving user transactions: {ex.Message}", ex);
        }
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

    public async Task<UserProfileDto> UpdateUserInfo(int userId, UserUpdateDto updateDto)
    {
        if (userId <= 0)
        {
            _logger.Warn($"Attempted to update user with invalid ID: {userId}");
            throw new ArgumentException("Invalid userId");
        }

        try
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);

            if (user == null)
            {
                _logger.Warn($"No user found with ID: {userId}");
                throw new KeyNotFoundException($"User with ID {userId} not found.");
            }

            // Update user properties if provided in the DTO
            if (!string.IsNullOrWhiteSpace(updateDto.Username)) user.Username = updateDto.Username;

            if (!string.IsNullOrWhiteSpace(updateDto.Email))
            {
                // Check if email is already in use by another user
                var existingUser =
                    await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == updateDto.Email && u.Id != userId);
                if (existingUser != null)
                {
                    _logger.Warn($"Email {updateDto.Email} is already in use by another user.");
                    throw new InvalidOperationException("Email is already in use.");
                }

                user.Email = updateDto.Email;
            }

            if (!string.IsNullOrWhiteSpace(updateDto.Bio)) user.Bio = updateDto.Bio;

            // Update the UpdatedAt timestamp
            user.UpdatedAt = DateTime.UtcNow;

            // Save changes to the database
            _unitOfWork.Users.Update(user);
            await _unitOfWork.CompleteAsync();

            _logger.Info($"Successfully updated user with ID: {userId}");

            // Return updated user profile
            return new UserProfileDto
            {
                UserId = user.Id,
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
        catch (InvalidOperationException ioEx)
        {
            _logger.Error($"Invalid operation: {ioEx.Message}");
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error($"An unexpected error occurred while updating user with ID {userId}: {ex.Message}");
            throw new Exception($"Error updating user profile: {ex.Message}", ex);
        }
    }

    public async Task<UserProfileDto> GetPhotographerProfile(int photographerId)
    {
        if (photographerId <= 0)
        {
            _logger.Warn($"Attempted to fetch photographer with invalid ID: {photographerId}");
            throw new ArgumentException("Invalid photographerId");
        }

        try
        {
            var user = await _unitOfWork.Users.GetByIdAsync(photographerId);

            if (user == null)
            {
                _logger.Warn($"No user found with ID: {photographerId}");
                throw new KeyNotFoundException($"User with ID {photographerId} not found.");
            }

            // Check if the user has the Photographer role
            if (user.Role != UserRole.Photographer)
            {
                _logger.Warn($"User with ID {photographerId} is not a photographer. Role: {user.Role}");
                throw new UnauthorizedAccessException($"User with ID {photographerId} is not a photographer.");
            }

            _logger.Info($"Successfully fetched photographer with ID: {photographerId}");

            return new UserProfileDto
            {
                UserId = user.Id,
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
        catch (UnauthorizedAccessException uaEx)
        {
            _logger.Error($"Unauthorized access: {uaEx.Message}");
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error(
                $"An unexpected error occurred while fetching photographer details for ID {photographerId}: {ex.Message}");
            throw new Exception($"Error retrieving photographer profile: {ex.Message}", ex);
        }
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
                UserId = userId,
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
                UserId = user.Id,
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