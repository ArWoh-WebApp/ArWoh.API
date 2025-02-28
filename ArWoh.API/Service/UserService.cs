using ArWoh.API.DTOs.UserDTOs;
using ArWoh.API.Interface;

namespace ArWoh.API.Service
{
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILoggerService _logger;

        public UserService(IUnitOfWork unitOfWork, ILoggerService loggerService)
        {
            _unitOfWork = unitOfWork;
            _logger = loggerService;
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


    }
}
