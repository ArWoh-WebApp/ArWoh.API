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

    public async Task<Image> UploadImageAsync(UploadImageDto uploadDto, int photographerId)
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

            var image = new Image
            {
                Title = uploadDto.Title,
                Description = uploadDto.Description,
                Price = uploadDto.Price,
                StoryOfArt = uploadDto.StoryOfArt,
                FileName = fileName,
                Url = fileUrl,
                PhotographerId = photographerId
            };

            await _unitOfWork.Images.AddAsync(image);
            await _unitOfWork.CompleteAsync();

            _loggerService.Info($"Image uploaded successfully: {fileName}");

            return image;
        }
        catch (Exception ex)
        {
            _loggerService.Error($"Image upload failed: {ex.Message}");
            throw new Exception("Failed to upload image.", ex);
        }
    }

}