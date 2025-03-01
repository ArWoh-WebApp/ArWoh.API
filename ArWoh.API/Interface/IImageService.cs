using ArWoh.API.DTOs.ImageDTOs;
using ArWoh.API.Entities;

namespace ArWoh.API.Interface;

public interface IImageService
{
    Task<IEnumerable<ImageDto>> GetAllImages();
    Task<ImageDto> GetImageById(int imageId);
    Task<ImageDto> UploadImageAsync(UploadImageDto uploadDto, int photographerId);
    Task<ImageDto> UpdateImageAsync(int imageId, UpdateImageDto updateDto);
    Task<bool> DeleteImageAsync(int imageId);
}