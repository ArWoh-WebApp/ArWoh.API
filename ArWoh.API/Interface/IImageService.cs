using ArWoh.API.DTOs.ImageDTOs;
using ArWoh.API.Entities;

namespace ArWoh.API.Interface;

public interface IImageService
{
    Task<Image> UploadImageAsync(UploadImageDto uploadDto, int photographerId);
}
