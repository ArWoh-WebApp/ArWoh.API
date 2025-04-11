using System.Transactions;
using ArWoh.API.DTOs.PaymentDTOs;
using ArWoh.API.DTOs.UserDTOs;
using ArWoh.API.Enums;
using ArWoh.API.Interface;
using Microsoft.EntityFrameworkCore;

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

    public async Task<IEnumerable<TransactionDto>> GetUserTransactions(int userId)
    {
        try
        {
            List<TransactionDto> transactions = new List<TransactionDto>();

            // Sử dụng khối using để đảm bảo connection được đóng đúng cách
            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                // PHASE 1: Lấy Orders của user
                var userOrdersQuery = _unitOfWork.Orders.GetQueryable()
                    .Where(o => o.CustomerId == userId);

                var userOrders = await userOrdersQuery.ToListAsync();

                if (!userOrders.Any()) return transactions;

                var orderIds = userOrders.Select(o => o.Id).ToList();

                // PHASE 2: Lấy Payments
                var paymentsQuery = _unitOfWork.Payments.GetQueryable()
                    .Where(p => orderIds.Contains(p.OrderId));

                var payments = await paymentsQuery.ToListAsync();

                // PHASE 3: Lấy OrderDetails
                var orderDetailsQuery = _unitOfWork.OrderDetails.GetQueryable()
                    .Where(od => orderIds.Contains(od.OrderId));

                var orderDetails = await orderDetailsQuery.ToListAsync();

                // Group order details by OrderId
                var orderDetailsMap = orderDetails
                    .GroupBy(od => od.OrderId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Sum(od => od.Quantity));

                // PHASE 4: Map to DTOs
                foreach (var payment in payments)
                {
                    var itemCount = orderDetailsMap.ContainsKey(payment.OrderId)
                        ? orderDetailsMap[payment.OrderId]
                        : 0;

                    var order = userOrders.FirstOrDefault(o => o.Id == payment.OrderId);

                    transactions.Add(new TransactionDto
                    {
                        TransactionId = payment.Id,
                        OrderId = payment.OrderId,
                        Date = payment.CreatedAt,
                        Amount = payment.Amount,
                        PaymentGateway = payment.PaymentGateway.ToString(),
                        PaymentStatus = payment.Status.ToString(),
                        GatewayTransactionId = payment.GatewayTransactionId ?? string.Empty,
                        OrderStatus = order?.Status.ToString() ?? "Unknown",
                        ItemCount = itemCount
                    });
                }

                scope.Complete();
            }

            // Sort by date, newest first
            return transactions.OrderByDescending(t => t.Date).ToList();
        }
        catch (Exception ex)
        {
            _logger.Error($"Error fetching transactions for user {userId}: {ex.Message}");
            throw;
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