using ArWoh.API.DTOs.ShippingDTOs;
using ArWoh.API.Enums;
using ArWoh.API.Interface;

namespace ArWoh.API.Service;

public class ShippingService : IShippingService
{
    private readonly ILoggerService _loggerService;
    private readonly IUnitOfWork _unitOfWork;

    public ShippingService(IUnitOfWork unitOfWork, ILoggerService loggerService)
    {
        _unitOfWork = unitOfWork;
        _loggerService = loggerService;
    }

    public async Task<IEnumerable<ShippableImageDto>> GetShippableImagesByUserId(int userId)
    {
        try
        {
            // Lấy tất cả các đơn hàng đã hoàn thành của user này
            // Sửa lại cách include để tránh lỗi
            var completedOrders = await _unitOfWork.Orders.FindAsync(
                o => o.CustomerId == userId && o.Status == OrderStatusEnum.Completed,
                o => o.OrderDetails);

            if (completedOrders == null || !completedOrders.Any()) return Enumerable.Empty<ShippableImageDto>();

            var result = new List<ShippableImageDto>();

            // Danh sách ID các hình ảnh cần lấy thông tin
            var imageIds = completedOrders
                .SelectMany(o => o.OrderDetails)
                .Select(od => od.ImageId)
                .Distinct()
                .ToList();

            // Lấy thông tin chi tiết của các hình ảnh
            var images = await _unitOfWork.Images.FindAsync(
                img => imageIds.Contains(img.Id));

            // Dictionary để map image ID -> Image object cho truy cập nhanh
            var imageDict = images.ToDictionary(img => img.Id);

            // Kiểm tra các hình ảnh từ những đơn hàng đã hoàn thành
            foreach (var order in completedOrders)
            foreach (var orderDetail in order.OrderDetails)
            {
                // Kiểm tra xem image có tồn tại trong dictionary không
                if (!imageDict.TryGetValue(orderDetail.ImageId,
                        out var image)) continue; // Skip nếu không tìm thấy image

                // Kiểm tra xem hình ảnh này đã được đặt ship chưa
                var isAlreadyShipped = await _unitOfWork.Orders.ExistsAsync(
                    o => o.CustomerId == userId
                         && o.IsPhysicalPrint == true
                         && o.OrderDetails.Any(od => od.ImageId == orderDetail.ImageId));

                // Nếu hình ảnh chưa được đặt ship, thêm vào danh sách kết quả
                if (!isAlreadyShipped)
                    result.Add(new ShippableImageDto
                    {
                        ImageId = orderDetail.ImageId,
                        Title = image.Title,
                        Description = image.Description,
                        Price = image.Price,
                        Url = image.Url,
                        PurchaseDate = order.CreatedAt,
                        OrderId = order.Id
                    });
            }

            return result;
        }
        catch (Exception e)
        {
            _loggerService.Error($"Error in GetShippableImagesByUserId: {e.Message}");
            throw;
        }
    }
}