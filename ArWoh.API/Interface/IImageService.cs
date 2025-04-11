using ArWoh.API.Commons;
using ArWoh.API.DTOs.ImageDTOs;
using ArWoh.API.Enums;

namespace ArWoh.API.Interface;

public interface IImageService
{
    Task<Pagination<ImageDto>> GetImagesUploadedByPhotographer(int photographerId,
        PaginationParameter paginationParams);

    Task<Pagination<ImageDto>>
        GetAllImages(PaginationParameter paginationParams,
            OrientationType? orientation = null);

    Task<IEnumerable<ImageDto>> GetRandomImages();

    Task<ImageDto> GetImageById(int imageId);

    Task<ImageDto> UploadImageAsync(UploadImageDto uploadDto, int photographerId);

    Task<ImageDto> UpdateImageAsync(int imageId, UpdateImageDto updateDto);

    Task<bool> DeleteImageAsync(int imageId);
}