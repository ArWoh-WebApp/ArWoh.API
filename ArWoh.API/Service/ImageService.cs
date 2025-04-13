using ArWoh.API.Commons;
using ArWoh.API.DTOs.ImageDTOs;
using ArWoh.API.Entities;
using ArWoh.API.Enums;
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
    ///     Lấy tất cả hình sau khi User đã mua dựa trên table Order và OrderDetail
    /// </summary>
    public async Task<IEnumerable<ImageDto>> GetAllImagesBoughtByUser(int userId)
    {
        try
        {
            // PHASE 1: Query completed orders
            // Lấy tất cả đơn hàng đã hoàn thành của người dùng cùng với OrderDetails và Payments
            var successfulOrders = await _unitOfWork.Orders.FindAsync(
                o => o.CustomerId == userId && o.Status == OrderStatusEnum.Completed,
                o => o.OrderDetails,
                o => o.Payments);

            // PHASE 2: Filter orders with successful payment
            // Lọc đơn hàng có ít nhất một payment thành công để đảm bảo sản phẩm đã được thanh toán
            var ordersWithSuccessfulPayment = successfulOrders.Where(o =>
                o.Payments.Any(p => p.Status == PaymentStatusEnum.COMPLETED));

            // PHASE 3: Extract order details
            // Lấy tất cả OrderDetail từ các đơn hàng thành công để truy cập thông tin hình ảnh
            var orderIds = ordersWithSuccessfulPayment.Select(o => o.Id).ToList();
            var orderDetails = await _unitOfWork.OrderDetails.FindAsync(
                od => orderIds.Contains(od.OrderId),
                od => od.Image);

            // PHASE 4: Transform to DTOs
            // Chuyển đổi thành ImageDto và loại bỏ trùng lặp để trả về cho client
            var images = orderDetails
                .Select(od => new ImageDto
                {
                    Id = od.Image.Id,
                    PhotographerId = od.Image.PhotographerId,
                    Title = od.Image.Title,
                    Description = od.Image.Description,
                    Price = od.Image.Price,
                    StoryOfArt = od.Image.StoryOfArt,
                    Orientation = od.Image.Orientation,
                    Tags = od.Image.Tags,
                    Location = od.Image.Location,
                    FileName = od.Image.FileName,
                    Url = od.Image.Url
                })
                .DistinctBy(img => img.Id) // Loại bỏ hình ảnh trùng lặp nếu user mua cùng một hình nhiều lần
                .ToList();

            return images;
        }
        catch (Exception ex)
        {
            // PHASE 5: Error handling
            // Ghi log lỗi và ném ngoại lệ để xử lý ở tầng controller
            _loggerService.Error($"Error fetching images for user {userId}: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    ///     Lấy tất cả ảnh trong hệ thống với thứ tự ngẫu nhiên
    /// </summary>
    /// <returns>Danh sách tất cả ảnh được sắp xếp ngẫu nhiên</returns>
    public async Task<IEnumerable<ImageDto>> GetRandomImages()
    {
        try
        {
            _loggerService.Info("Fetching all images in random order from database.");

            // Lấy tất cả các ảnh không bị xóa mềm
            var imagesQuery = _unitOfWork.Images.GetQueryable();

            // Include thông tin photographer
            var allImages = await imagesQuery
                .Include(i => i.Photographer)
                .ToListAsync();

            if (!allImages.Any())
            {
                _loggerService.Warn("No images found in the database.");
                return new List<ImageDto>(); // Trả về danh sách rỗng thay vì null
            }

            // Tạo một Random instance
            var random = new Random();

            // Sắp xếp tất cả ảnh theo thứ tự ngẫu nhiên
            var randomImages = allImages
                .OrderBy(_ => random.Next())
                .ToList();

            var imageDtos = randomImages.Select(image => new ImageDto
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
                PhotographerAvatar = image.Photographer.ProfilePictureUrl,
                PhotographerName = image.Photographer?.Username ?? "Unknown",
                PhotographerEmail = image.Photographer?.Email
            }).ToList();

            _loggerService.Success($"Successfully retrieved {imageDtos.Count} images in random order.");

            return imageDtos;
        }
        catch (Exception ex)
        {
            _loggerService.Error($"Unexpected error in GetRandomImages: {ex.Message}");
            throw new Exception("An error occurred while retrieving random images.", ex);
        }
    }

    /// <summary>
    ///     Lấy list tất cả các images kèm theo thông tin photographer với phân trang và filter theo orientation
    /// </summary>
    public async Task<Pagination<ImageDto>> GetAllImages(PaginationParameter paginationParams,
        OrientationType? orientation = null)
    {
        try
        {
            _loggerService.Info(
                $"Fetching images with pagination (Page {paginationParams.PageIndex}, Size {paginationParams.PageSize}) " +
                (orientation.HasValue ? $"and orientation filter: {orientation}" : "without orientation filter"));

            // Sử dụng GetQueryable để có thể thêm Include
            var imagesQuery = _unitOfWork.Images.GetQueryable();

            // Include thông tin photographer
            imagesQuery = imagesQuery.Include(i => i.Photographer);

            // Áp dụng filter theo orientation nếu có
            if (orientation.HasValue) imagesQuery = imagesQuery.Where(i => i.Orientation == orientation);

            // Đếm tổng số records trước khi phân trang
            var totalCount = await imagesQuery.CountAsync();

            // Áp dụng phân trang
            var pagedImages = await imagesQuery
                .Skip((paginationParams.PageIndex - 1) * paginationParams.PageSize)
                .Take(paginationParams.PageSize)
                .ToListAsync();

            if (!pagedImages.Any() && totalCount > 0)
            {
                _loggerService.Warn(
                    $"No images found for page {paginationParams.PageIndex}. Total records: {totalCount}");
                // Nếu không có dữ liệu ở trang hiện tại nhưng có dữ liệu trong DB, trả về trang cuối cùng
                paginationParams.PageIndex = (int)Math.Ceiling(totalCount / (double)paginationParams.PageSize);
                pagedImages = await imagesQuery
                    .Skip((paginationParams.PageIndex - 1) * paginationParams.PageSize)
                    .Take(paginationParams.PageSize)
                    .ToListAsync();
            }
            else if (!pagedImages.Any())
            {
                _loggerService.Warn(orientation.HasValue
                    ? $"No images found with orientation {orientation}"
                    : "No images found in the database.");

                return new Pagination<ImageDto>(new List<ImageDto>(), 0, paginationParams.PageIndex,
                    paginationParams.PageSize);
            }

            var imageDtos = pagedImages.Select(image => new ImageDto
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
                PhotographerAvatar = image.Photographer?.ProfilePictureUrl,
                PhotographerName = image.Photographer?.Username ?? "Unknown",
                PhotographerEmail = image.Photographer?.Email
            }).ToList();

            _loggerService.Success(
                $"Successfully retrieved {imageDtos.Count} images from page {paginationParams.PageIndex} " +
                (orientation.HasValue ? $"with orientation {orientation} " : "") +
                $"(total records: {totalCount}).");

            // Tạo đối tượng phân trang chứa dữ liệu và thông tin phân trang
            var paginatedResult = new Pagination<ImageDto>(
                imageDtos,
                totalCount,
                paginationParams.PageIndex,
                paginationParams.PageSize
            );

            return paginatedResult;
        }
        catch (Exception ex)
        {
            _loggerService.Error($"Unexpected error in GetAllImages: {ex.Message}");
            throw new Exception("An error occurred while retrieving images.", ex);
        }
    }


    /// <summary>
    ///     Lấy details của 1 tấm hình kèm thông tin photographer
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
                PhotographerAvatar = image.Photographer?.ProfilePictureUrl,
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

    public async Task<Pagination<ImageDto>> GetImagesUploadedByPhotographer(int photographerId,
        PaginationParameter paginationParams)
    {
        try
        {
            var photographer = await _unitOfWork.Users.GetByIdAsync(photographerId);
            if (photographer == null)
            {
                _loggerService.Info($"Photographer with ID {photographerId} not found.");
                throw new KeyNotFoundException($"Photographer with ID {photographerId} not found.");
            }

            // Get queryable to apply pagination
            var query = _unitOfWork.Images.GetQueryable()
                .Where(img => img.PhotographerId == photographerId && !img.IsDeleted)
                .OrderByDescending(img => img.CreatedAt);

            // Get total count for pagination
            var totalCount = await query.CountAsync();

            // Apply pagination
            var paginatedItems = await query
                .Skip((paginationParams.PageIndex - 1) * paginationParams.PageSize)
                .Take(paginationParams.PageSize)
                .ToListAsync();

            if (paginatedItems == null || !paginatedItems.Any())
            {
                _loggerService.Info(
                    $"No images found for photographer ID {photographerId} on page {paginationParams.PageIndex}.");
                return new Pagination<ImageDto>(new List<ImageDto>(), totalCount, paginationParams.PageIndex,
                    paginationParams.PageSize);
            }

            // Convert to DTO
            var imageDtos = paginatedItems.Select(img => new ImageDto
            {
                Id = img.Id,
                Title = img.Title,
                Description = img.Description,
                Price = img.Price,
                Url = img.Url,
                PhotographerId = img.PhotographerId ?? 0,
                Tags = img.Tags
            }).ToList();

            return new Pagination<ImageDto>(
                imageDtos,
                totalCount,
                paginationParams.PageIndex,
                paginationParams.PageSize);
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
            // Thay đổi đường dẫn thành artworks/
            var fileName = $"artworks/{photographerId}_{Guid.NewGuid()}{Path.GetExtension(uploadDto.File.FileName)}";

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