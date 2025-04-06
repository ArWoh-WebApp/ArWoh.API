using ArWoh.API.DTOs.AdminDtos;
using ArWoh.API.Enums;
using ArWoh.API.Interface;
using Microsoft.EntityFrameworkCore;

namespace ArWoh.API.Service;

public class AdminService : IAdminService
{
    private readonly ILoggerService _loggerService;
    private readonly IUnitOfWork _unitOfWork;

    public AdminService(ILoggerService loggerService, IUnitOfWork unitOfWork)
    {
        _loggerService = loggerService;
        _unitOfWork = unitOfWork;
    }

    // Lấy tổng quan dữ liệu
    public async Task<OverviewDTO> GetOverviewAsync()
    {
        var totalUsers = await _unitOfWork.Users.CountAsync();
        var totalImages = await _unitOfWork.Images.CountAsync();

        var totalRevenue = await _unitOfWork.Payments.GetQueryable()
            .Where(p => p.PaymentStatus == PaymentStatusEnum.COMPLETED)
            .SumAsync(p => p.Amount);

        return new OverviewDTO
        {
            TotalUsers = totalUsers,
            TotalImages = totalImages,
            TotalRevenue = totalRevenue
        };
    }

    // Lấy thống kê user
    public async Task<UserSummaryDTO> GetUserSummaryAsync()
    {
        var totalUsers = await _unitOfWork.Users.CountAsync();

        var users = _unitOfWork.Users.GetQueryable();
        var adminCount = await users.CountAsync(u => u.Role == UserRole.Admin);
        var userCount = await users.CountAsync(u => u.Role == UserRole.Customer);
        var photographerCount = await users.CountAsync(u => u.Role == UserRole.Photographer);

        return new UserSummaryDTO
        {
            TotalUsers = totalUsers,
            AdminCount = adminCount,
            UserCount = userCount,
            PhotographerCount = photographerCount
        };
    }

    // Lấy thống kê tranh
    public async Task<ImageSummaryDTO> GetImageSummaryAsync()
    {
        var totalImages = await _unitOfWork.Images.CountAsync();

        var imageOrientations = await _unitOfWork.Images.GetQueryable()
            .GroupBy(i => i.Orientation)
            .Select(g => new { Orientation = g.Key.ToString(), Count = g.Count() })
            .ToDictionaryAsync(x => x.Orientation, x => x.Count);

        return new ImageSummaryDTO
        {
            TotalImages = totalImages,
            ImageOrientations = imageOrientations
        };
    }

    // Lấy doanh thu theo từng tháng
    public async Task<RevenueSummaryDTO> GetRevenueSummaryAsync()
    {
        var payments = _unitOfWork.Payments.GetQueryable()
            .Where(p => p.PaymentStatus == PaymentStatusEnum.COMPLETED);

        var totalRevenue = await payments.SumAsync(p => p.Amount);

        var monthlyRevenue = await payments
            .GroupBy(p => new { p.CreatedAt.Year, p.CreatedAt.Month })
            .Select(g => new
            {
                Month = $"{g.Key.Year}-{g.Key.Month:D2}",
                Revenue = g.Sum(p => p.Amount)
            })
            .ToDictionaryAsync(x => x.Month, x => x.Revenue);

        return new RevenueSummaryDTO
        {
            TotalRevenue = totalRevenue,
            MonthlyRevenue = monthlyRevenue
        };
    }
}