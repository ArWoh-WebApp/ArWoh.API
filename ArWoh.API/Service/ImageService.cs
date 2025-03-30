using ArWoh.API.DTOs.ImageDTOs;
using ArWoh.API.Entities;
using ArWoh.API.Interface;
using Microsoft.EntityFrameworkCore;

namespace ArWoh.API.Service;

public class ImageService : IImageService
{
    private readonly IBlobService _blobService;
    private readonly ILoggerService _loggerService;
    private readonly IUnitOfWork _unitOfWork;

    public ImageService(ILoggerService loggerService, IBlobService blobService, IUnitOfWork unitOfWork)
    {
        _loggerService = loggerService;
        _blobService = blobService;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Lấy list tất cả các images kèm theo thông tin photographer
    /// </summary>
    public async Task<IEnumerable<ImageDto>> GetAllImages()
    {
        try
        {
            _loggerService.Info("Fetching all images with photographer information from database.");

            // Sử dụng GetQueryable để có thể thêm Include
            var imagesQuery = _unitOfWork.Images.GetQueryable();

            // Include thông tin photographer
            var images = await imagesQuery
                .Include(i => i.Photographer)
                .ToListAsync();

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
                Url = image.Url,
                // Thêm thông tin photographer nếu có
                PhotographerName = image.Photographer?.Username ?? "Unknown",
                PhotographerEmail = image.Photographer?.Email
            }).ToList();

            _loggerService.Success($"Successfully retrieved {imageDtos.Count} images with photographer information.");

            return imageDtos;
        }
        catch (Exception ex)
        {
            _loggerService.Error($"Unexpected error in GetAllImages: {ex.Message}");
            throw new Exception("An error occurred while retrieving images.", ex);
        }
    }

    /// <summary>
    /// Lấy tất cả hình sau khi User đã mua dựa trên table PaymentTransaction
    /// </summary>
    public async Task<IEnumerable<ImageDto>> GetAllImagesBoughtByUser(int userId)
    {
        try
        {
            var paymentTransactions = await _unitOfWork.PaymentTransactions
                .GetQueryable()
                .Where(pt => pt.CustomerId == userId)
                .Include(pt => pt.Image)
                .ToListAsync();

            var images = paymentTransactions
                .Where(pt => pt.Image != null)
                .Select(pt => new ImageDto
                {
                    Id = pt.Image.Id,
                    PhotographerId = pt.Image.PhotographerId,
                    Title = pt.Image.Title,
                    Description = pt.Image.Description,
                    Price = pt.Image.Price,
                    StoryOfArt = pt.Image.StoryOfArt,
                    Orientation = pt.Image.Orientation,
                    Tags = pt.Image.Tags,
                    Location = pt.Image.Location,
                    FileName = pt.Image.FileName,
                    Url = pt.Image.Url
                })
                .ToList();

            return images;
        }
        catch (Exception ex)
        {
            _loggerService.Error($"Error fetching images for user {userId}: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Lấy details của 1 tấm hình kèm thông tin photographer
    /// </summary>
    public async Task<ImageDto> GetImageById(int imageId)
    {
        _loggerService.Info($"Fetching image details for ID: {imageId}");
        try
        {
            // Sử dụng GetQueryable để có thể thêm Include
            var imageQuery = _unitOfWork.Images.GetQueryable();

            // Include thông tin photographer và lấy image theo id
            var image = await imageQuery
                .Include(i => i.Photographer)
                .FirstOrDefaultAsync(i => i.Id == imageId && !i.IsDeleted);

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
                Url = image.Url,
                // Thêm thông tin photographer nếu có
                PhotographerName = image.Photographer?.Username ?? "Unknown",
                PhotographerEmail = image.Photographer?.Email
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

    public async Task<IEnumerable<ImageDto>> GetImagesUploadedByPhotographer(int photographerId)
    {
        try
        {
            var photographer = await _unitOfWork.Users.GetByIdAsync(photographerId);
            if (photographer == null)
            {
                _loggerService.Info($"Photographer with ID {photographerId} not found.");
                throw new KeyNotFoundException($"Photographer with ID {photographerId} not found.");
            }

            var images =
                await _unitOfWork.Images.FindAsync(img => img.PhotographerId == photographerId && !img.IsDeleted);

            if (images == null || !images.Any())
            {
                _loggerService.Info($"No images found for photographer ID {photographerId}.");
                return Enumerable.Empty<ImageDto>();
            }

            return images.Select(img => new ImageDto
            {
                Id = img.Id,
                Title = img.Title,
                Description = img.Description,
                Price = img.Price,
                Url = img.Url,
                PhotographerId = img.PhotographerId ?? 0,
                Tags = img.Tags
            }).ToList();
        }
        catch (Exception e)
        {
            _loggerService.Error($"Error retrieving images for photographer ID {photographerId}: {e.Message}");
            throw;
        }
    }

    /// <summary>
    ///     Dùng cho Photographer up hình ảnh lên system
    /// </summary>
    /// <param name="uploadDto"></param>
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
                Location = image.Location,
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
    ///     Cập nhật thông tin của 1 tấm ảnh
    /// </summary>
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
    ///     Xóa mềm 1 tấm ảnh trong DB
    /// </summary>
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