using ArWoh.API.DTOs.ShipOrderDTOs;
using ArWoh.API.Enums;
using ArWoh.API.Interface;
using Microsoft.EntityFrameworkCore;

namespace ArWoh.API.Service;

public class ShippingOrderService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILoggerService _logger;
    private readonly IImageService _imageService;
    private readonly IClaimService _claimService;

    public ShippingOrderService(IUnitOfWork unitOfWork, ILoggerService logger, IImageService imageService, IClaimService claimService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _imageService = imageService;
        _claimService = claimService;
    }
    
    /// <summary>
    /// Lấy danh sách hình ảnh để người dùng chọn tạo đơn ship
    /// </summary>
    public async Task<IEnumerable<ShippableImageDto>> GetShippableImagesByUserId(int userId)
    {
        try
        {
            // Lấy tất cả hình ảnh mà user đã mua
            var boughtImages = await _imageService.GetAllImagesBoughtByUser(userId);
            
            // Lấy các giao dịch đã hoàn thành của user
            var completedTransactions = await _unitOfWork.PaymentTransactions
                .GetQueryable()
                .Where(pt => pt.CustomerId == userId && 
                             pt.PaymentStatus == PaymentTransactionStatusEnum.COMPLETED)
                .ToListAsync();
                
            // Lấy các transaction ID mà đã được tạo đơn ship
            var shippedTransactionIds = await _unitOfWork.ShippingOrders
                .GetQueryable()
                .Where(so => completedTransactions.Select(t => t.Id).Contains(so.TransactionId))
                .Select(so => so.TransactionId)
                .ToListAsync();
                
            // Danh sách các giao dịch chưa được ship
            var nonShippedTransactions = completedTransactions
                .Where(pt => !shippedTransactionIds.Contains(pt.Id))
                .ToList();
                
            // Danh sách các hình ảnh có thể ship
            var shippableImages = boughtImages
                .Where(img => nonShippedTransactions.Any(t => t.ImageId == img.Id))
                .Select(img => new ShippableImageDto
                {
                    Id = img.Id,
                    Title = img.Title,
                    Description = img.Description,
                    Price = img.Price,
                    Url = img.Url,
                    TransactionId = nonShippedTransactions.FirstOrDefault(t => t.ImageId == img.Id)?.Id ?? 0
                })
                .ToList();
                
            return shippableImages;
        }
        catch (Exception ex)
        {
            _logger.Error($"Lỗi khi lấy danh sách hình ảnh có thể ship cho user {userId}: {ex.Message}");
            throw;
        }
    }

}