using ArWoh.API.Commons;
using ArWoh.API.DTOs.ImageDTOs;
using ArWoh.API.Enums;

namespace ArWoh.API.Interface;

public interface IImageService
{
    Task<IEnumerable<ImageDto>> GetImagesUploadedByPhotographer(int photographerId);

    Task<IEnumerable<ImageDto>> GetRandomImages();

    Task<Pagination<ImageDto>>
        GetAllImages(PaginationParameter paginationParams,
            OrientationType? orientation = null); // Task<IEnumerable<ImageDto>> GetAllImagesBoughtByUser(int userId);

    Task<ImageDto> GetImageById(int imageId);

    Task<ImageDto> UploadImageAsync(UploadImageDto uploadDto, int photographerId);

    Task<ImageDto> UpdateImageAsync(int imageId, UpdateImageDto updateDto);

    Task<bool> DeleteImageAsync(int imageId);
}