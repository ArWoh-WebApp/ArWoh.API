using ArWoh.API.DTOs.AdminDtos;

namespace ArWoh.API.Interface;

public interface IAdminService
{
    Task<OverviewDTO> GetOverviewAsync();
    Task<UserSummaryDTO> GetUserSummaryAsync();
    Task<ImageSummaryDTO> GetImageSummaryAsync();
    Task<RevenueSummaryDTO> GetRevenueSummaryAsync();
}