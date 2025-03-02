using ArWoh.API.DTOs.ImageDTOs;
using ArWoh.API.Entities;
using ArWoh.API.Interface;

namespace ArWoh.API.Service;

public class ImageService : IImageService
{
    private readonly ILoggerService _loggerService;
    private readonly IBlobService _blobService;
    private readonly IUnitOfWork _unitOfWork;

    public ImageService(ILoggerService loggerService, IBlobService blobService, IUnitOfWork unitOfWork)
    {
        _loggerService = loggerService;
        _blobService = blobService;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Lấy list tất cả các images lên
    /// </summary>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public async Task<IEnumerable<ImageDto>> GetAllImages()
    {
        try
        {
            _loggerService.Info("Fetching all images from database.");

            var images = await _unitOfWork.Images.GetAllAsync();

            if (!images.Any())
            {
                _loggerService.Warn("No images found in the database.");
                return new List<ImageDto>(); // Trả về danh sách rỗng thay vì null
            }

            var imageDtos = images.Select(image => new ImageDto
            {
                Id = image.Id,
                Title = image.Title,
                Description = image.Description,
                Price = image.Price,
                StoryOfArt = image.StoryOfArt,
                Orientation = image.Orientation,
                Location = image.Location,
                Tags = image.Tags,
                FileName = image.FileName,
                PhotographerId = image.PhotographerId,
                Url = image.Url
            }).ToList();

            _loggerService.Success($"Successfully retrieved {imageDtos.Count} images.");

            return imageDtos;
        }
        catch (Exception ex)
        {
            _loggerService.Error($"Unexpected error in GetAllImages: {ex.Message}");
            throw new Exception("An error occurred while retrieving images.", ex);
        }
    }

    /// <summary>
    /// Lấy details của 1 tấm hình
    /// </summary>
    /// <param name="imageId"></param>
    /// <returns></returns>
    /// <exception cref="KeyNotFoundException"></exception>
    /// <exception cref="Exception"></exception>
    public async Task<ImageDto> GetImageById(int imageId)
    {
        _loggerService.Info($"Fetching image details for ID: {imageId}");

        try
        {
            var image = await _unitOfWork.Images.GetByIdAsync(imageId);

            if (image == null)
            {
                _loggerService.Warn($"Image with ID {imageId} not found.");
                throw new KeyNotFoundException($"Image with ID {imageId} not found.");
            }

            var imageDto = new ImageDto
            {
                Id = image.Id,
                Title = image.Title,
                Description = image.Description,
                Price = image.Price,
                StoryOfArt = image.StoryOfArt,
                Orientation = image.Orientation,
                Location = image.Location,
                Tags = image.Tags,
                FileName = image.FileName,
                PhotographerId = image.PhotographerId,
                Url = image.Url
            };

            _loggerService.Success($"Successfully fetched image details for ID: {imageId}");

            return imageDto;
        }
        catch (KeyNotFoundException ex)
        {
            _loggerService.Warn($"GetImageDetails failed: {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            _loggerService.Error($"Unexpected error in GetImageDetails: {ex.Message}");
            throw new Exception("An error occurred while retrieving the image details.", ex);
        }
    }

    /// <summary>
    /// Dùng cho Photographer up hình ảnh lên system
    /// </summary>
    /// <param name="uploadDto"></param>
    /// <param name="photographerId"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="Exception"></exception>
    public async Task<ImageDto> UploadImageAsync(UploadImageDto uploadDto, int photographerId)
    {
        if (uploadDto == null || uploadDto.File == null || uploadDto.File.Length == 0)
        {
            _loggerService.Warn("Invalid image upload request.");
            throw new ArgumentException("Invalid image upload request.");
        }

        try
        {
            var fileName = $"{Guid.NewGuid()}_{uploadDto.File.FileName}";

            using (var stream = uploadDto.File.OpenReadStream())
            {
                await _blobService.UploadFileAsync(fileName, stream);
            }

            var fileUrl = await _blobService.GetFileUrlAsync(fileName);

            // Tạo entity Image mới
            var image = new Image
            {
                Title = uploadDto.Title,
                Description = uploadDto.Description,
                Price = uploadDto.Price,
                StoryOfArt = uploadDto.StoryOfArt,
                Orientation = uploadDto.Orientation,
                Location = uploadDto.Location,
                Tags = uploadDto.Tags,
                FileName = fileName,
                Url = fileUrl,
                PhotographerId = photographerId // hoặc có thể ánh xạ sang UserId nếu cần
            };

            await _unitOfWork.Images.AddAsync(image);
            await _unitOfWork.CompleteAsync();

            _loggerService.Info($"Image uploaded successfully: {fileName}");

            // Map entity sang DTO trả về
            var responseDto = new ImageDto
            {
                Id = image.Id,
                Title = image.Title,
                Description = image.Description,
                Price = image.Price,
                StoryOfArt = image.StoryOfArt,
                Orientation = image.Orientation,
                Tags = image.Tags,
                FileName = image.FileName,
                PhotographerId = image.PhotographerId,
                Url = image.Url
            };

            return responseDto;
        }
        catch (Exception ex)
        {
            _loggerService.Error($"Image upload failed: {ex.Message}");
            throw new Exception("Failed to upload image.", ex);
        }
    }

    /// <summary>
    /// Cập nhật thông tin của 1 tấm ảnh
    /// </summary>
    /// <param name="imageId"></param>
    /// <param name="updateDto"></param>
    /// <returns></returns>
    /// <exception cref="KeyNotFoundException"></exception>
    /// <exception cref="Exception"></exception>
    public async Task<ImageDto> UpdateImageAsync(int imageId, UpdateImageDto updateDto)
    {
        try
        {
            _loggerService.Info($"Updating image with ID: {imageId}");

            var image = await _unitOfWork.Images.GetByIdAsync(imageId);
            if (image == null)
            {
                _loggerService.Warn($"Image with ID {imageId} not found.");
                throw new KeyNotFoundException("Image not found.");
            }

            // ✅ Chỉ update field nếu `updateDto` có giá trị
            if (!string.IsNullOrEmpty(updateDto.Title))
                image.Title = updateDto.Title;

            if (!string.IsNullOrEmpty(updateDto.Description))
                image.Description = updateDto.Description;

            if (updateDto.Price.HasValue) // Giá trị số có thể null
                image.Price = updateDto.Price.Value;

            if (!string.IsNullOrEmpty(updateDto.StoryOfArt))
                image.StoryOfArt = updateDto.StoryOfArt;

            if (!string.IsNullOrEmpty(updateDto.Location))
                image.Location = updateDto.Location;

            if (updateDto.Orientation.HasValue) // Enum có thể null
                image.Orientation = updateDto.Orientation.Value;

            if (updateDto.Tags != null && updateDto.Tags.Any()) // Đảm bảo tags không phải null hoặc rỗng
                image.Tags = updateDto.Tags;

            _unitOfWork.Images.Update(image);
            await _unitOfWork.CompleteAsync();

            _loggerService.Success($"Successfully updated image ID: {imageId}");

            return new ImageDto
            {
                Id = image.Id,
                Title = image.Title,
                Description = image.Description,
                Price = image.Price,
                StoryOfArt = image.StoryOfArt,
                Location = image.Location,
                Orientation = image.Orientation,
                Tags = image.Tags,
                FileName = image.FileName,
                Url = image.Url
            };
        }
        catch (KeyNotFoundException ex)
        {
            _loggerService.Warn($"Update failed: {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            _loggerService.Error($"Unexpected error in UpdateImageAsync: {ex.Message}");
            throw new Exception("An error occurred while updating the image.", ex);
        }
    }

    /// <summary>
    /// Xóa mềm 1 tấm ảnh trong DB
    /// </summary>
    /// <param name="imageId"></param>
    /// <returns></returns>
    /// <exception cref="KeyNotFoundException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="Exception"></exception>
    public async Task<bool> DeleteImageAsync(int imageId)
    {
        try
        {
            _loggerService.Info($"Soft deleting image with ID: {imageId}");

            var image = await _unitOfWork.Images.GetByIdAsync(imageId);
            if (image == null)
            {
                _loggerService.Warn($"Image with ID {imageId} not found.");
                throw new KeyNotFoundException("Image not found.");
            }

            // Kiểm tra xem ảnh đã bị xóa trước đó chưa
            if (image.IsDeleted)
            {
                _loggerService.Warn($"Image with ID {imageId} is already deleted.");
                throw new InvalidOperationException("This image has already been deleted.");
            }

            // Sử dụng hàm Delete trong repository để soft delete
            _unitOfWork.Images.Delete(image);
            await _unitOfWork.CompleteAsync();

            _loggerService.Success($"Successfully soft deleted image ID: {imageId}");

            return true;
        }
        catch (KeyNotFoundException ex)
        {
            _loggerService.Warn($"Soft delete failed: {ex.Message}");
            throw;
        }
        catch (InvalidOperationException ex)
        {
            _loggerService.Warn($"Soft delete failed: {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            _loggerService.Error($"Unexpected error in DeleteImageAsync: {ex.Message}");
            throw new Exception("An error occurred while deleting the image.", ex);
        }
    }
}