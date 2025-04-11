using ArWoh.API.Commons;
using ArWoh.API.DTOs.ImageDTOs;
using ArWoh.API.Enums;

namespace ArWoh.API.Interface;

public interface IImageService
{
    //GET
    Task<Pagination<ImageDto>> GetImagesUploadedByPhotographer(int photographerId,
        PaginationParameter paginationParams);

    Task<Pagination<ImageDto>> GetAllImages(PaginationParameter paginationParams,
        OrientationType? orientation = null);

    Task<IEnumerable<ImageDto>> GetRandomImages();

    Task<ImageDto> GetImageById(int imageId);

    Task<IEnumerable<ImageDto>> GetAllImagesBoughtByUser(int userId);

    //CREATE
    Task<ImageDto> UploadImageAsync(UploadImageDto uploadDto, int photographerId);

    //UPDATE
    Task<ImageDto> UpdateImageAsync(int imageId, UpdateImageDto updateDto);

    //DELETE
    Task<bool> DeleteImageAsync(int imageId);
}