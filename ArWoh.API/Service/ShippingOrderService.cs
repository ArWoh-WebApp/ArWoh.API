using ArWoh.API.DTOs.ShipOrderDTOs;
using ArWoh.API.Entities;
using ArWoh.API.Enums;
using ArWoh.API.Interface;
using Microsoft.EntityFrameworkCore;

namespace ArWoh.API.Service;

public class ShippingOrderService : IShippingOrderService
{
    private readonly IClaimService _claimService;
    private readonly IImageService _imageService;
    private readonly ILoggerService _logger;
    private readonly IUnitOfWork _unitOfWork;

    public ShippingOrderService(IUnitOfWork unitOfWork, ILoggerService logger, IImageService imageService,
        IClaimService claimService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _imageService = imageService;
        _claimService = claimService;
    }

    /// <summary>
    ///     Lấy danh sách hình ảnh để người dùng chọn tạo đơn ship
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

    /// <summary>
    ///     Tạo đơn hàng ship cho hình ảnh
    /// </summary>
    public async Task<ShippingOrderDto> CreateShippingOrder(CreateShippingOrderDto createDto)
    {
        try
        {
            // Kiểm tra xem transaction có tồn tại không
            var transaction = await _unitOfWork.PaymentTransactions.GetByIdAsync(createDto.TransactionId);
            if (transaction == null)
                throw new KeyNotFoundException($"Không tìm thấy giao dịch với ID {createDto.TransactionId}");

            // Kiểm tra trạng thái transaction
            if (transaction.PaymentStatus != PaymentTransactionStatusEnum.COMPLETED)
                throw new InvalidOperationException("Giao dịch chưa hoàn thành thanh toán");

            // Kiểm tra xem transaction đã có đơn ship chưa
            var existingOrder = await _unitOfWork.ShippingOrders
                .FirstOrDefaultAsync(so => so.TransactionId == createDto.TransactionId);

            if (existingOrder != null)
                throw new InvalidOperationException("Giao dịch này đã có đơn hàng ship");

            // Tính phí ship (20% tổng đơn)
            var shippingFee = transaction.Amount * 0.2m;

            // Tạo đơn hàng ship mới
            var shippingOrder = new ShippingOrder
            {
                TransactionId = createDto.TransactionId,
                ShippingAddress = createDto.ShippingAddress,
                ShippingFee = shippingFee,
                Status = ShippingStatusEnum.Pending
            };

            await _unitOfWork.ShippingOrders.AddAsync(shippingOrder);
            await _unitOfWork.CompleteAsync();

            // Lấy thông tin hình ảnh từ transaction
            var image = await _unitOfWork.Images.GetByIdAsync(transaction.ImageId);

            // Kiểm tra xem có tìm thấy hình ảnh không
            if (image == null)
            {
                _logger.Warn($"Không tìm thấy hình ảnh với ID {transaction.ImageId} cho transaction {transaction.Id}");

                // Vẫn trả về thông tin đơn hàng nhưng không có thông tin hình ảnh
                return new ShippingOrderDto
                {
                    Id = shippingOrder.Id,
                    TransactionId = shippingOrder.TransactionId,
                    ImageId = transaction.ImageId,
                    ImageTitle = "Không tìm thấy thông tin hình ảnh",
                    ImageUrl = null,
                    ShippingAddress = shippingOrder.ShippingAddress,
                    ShippingFee = shippingOrder.ShippingFee,
                    OrderAmount = transaction.Amount,
                    TotalAmount = transaction.Amount + shippingOrder.ShippingFee,
                    Status = shippingOrder.Status,
                    CreatedAt = shippingOrder.CreatedAt
                };
            }

            return new ShippingOrderDto
            {
                Id = shippingOrder.Id,
                TransactionId = shippingOrder.TransactionId,
                ImageId = image.Id,
                ImageTitle = image.Title ?? "Không có tiêu đề",
                ImageUrl = image.Url,
                ShippingAddress = shippingOrder.ShippingAddress,
                ShippingFee = shippingOrder.ShippingFee,
                OrderAmount = transaction.Amount,
                TotalAmount = transaction.Amount + shippingOrder.ShippingFee,
                Status = shippingOrder.Status,
                CreatedAt = shippingOrder.CreatedAt
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Lỗi khi tạo đơn hàng ship: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    ///     Lấy danh sách đơn hàng ship của user
    /// </summary>
    public async Task<IEnumerable<ShippingOrderDto>> GetUserShippingOrders(int userId)
    {
        try
        {
            // Lấy các transaction của user
            var userTransactions = await _unitOfWork.PaymentTransactions
                .GetQueryable()
                .Where(pt => pt.CustomerId == userId)
                .Select(pt => pt.Id)
                .ToListAsync();

            // Lấy các đơn hàng ship dựa trên transaction
            var shippingOrders = await _unitOfWork.ShippingOrders
                .GetQueryable()
                .Where(so => userTransactions.Contains(so.TransactionId))
                .Include(so => so.Transaction)
                .ThenInclude(t => t.Image)
                .ToListAsync();

            return shippingOrders.Select(so => new ShippingOrderDto
            {
                Id = so.Id,
                TransactionId = so.TransactionId,
                ImageId = so.Transaction.ImageId,
                ImageTitle = so.Transaction.Image.Title,
                ImageUrl = so.Transaction.Image.Url,
                ShippingAddress = so.ShippingAddress,
                ShippingFee = so.ShippingFee,
                OrderAmount = so.Transaction.Amount,
                TotalAmount = so.Transaction.Amount + so.ShippingFee,
                Status = so.Status,
                ConfirmNote = so.ConfirmNote,
                PackagingNote = so.PackagingNote,
                ShippingNote = so.ShippingNote,
                DeliveryNote = so.DeliveryNote,
                DeliveryProofImageUrl = so.DeliveryProofImageUrl,
                CreatedAt = so.CreatedAt,
                UpdatedAt = so.UpdatedAt
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.Error($"Lỗi khi lấy danh sách đơn hàng ship của user {userId}: {ex.Message}");
            throw;
        }
    }


    /// <summary>
    ///     Lấy chi tiết đơn hàng ship
    /// </summary>
    public async Task<ShippingOrderDto> GetShippingOrderById(int orderId)
    {
        try
        {
            var shippingOrder = await _unitOfWork.ShippingOrders
                .GetQueryable()
                .Where(so => so.Id == orderId)
                .Include(so => so.Transaction)
                .ThenInclude(t => t.Image)
                .FirstOrDefaultAsync();

            if (shippingOrder == null)
                throw new KeyNotFoundException($"Không tìm thấy đơn hàng ship với ID {orderId}");

            return new ShippingOrderDto
            {
                Id = shippingOrder.Id,
                TransactionId = shippingOrder.TransactionId,
                ImageId = shippingOrder.Transaction.ImageId,
                ImageTitle = shippingOrder.Transaction.Image.Title,
                ImageUrl = shippingOrder.Transaction.Image.Url,
                ShippingAddress = shippingOrder.ShippingAddress,
                ShippingFee = shippingOrder.ShippingFee,
                OrderAmount = shippingOrder.Transaction.Amount,
                TotalAmount = shippingOrder.Transaction.Amount + shippingOrder.ShippingFee,
                Status = shippingOrder.Status,
                ConfirmNote = shippingOrder.ConfirmNote,
                PackagingNote = shippingOrder.PackagingNote,
                ShippingNote = shippingOrder.ShippingNote,
                DeliveryNote = shippingOrder.DeliveryNote,
                DeliveryProofImageUrl = shippingOrder.DeliveryProofImageUrl,
                CreatedAt = shippingOrder.CreatedAt,
                UpdatedAt = shippingOrder.UpdatedAt
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Lỗi khi lấy chi tiết đơn hàng ship {orderId}: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    ///     Lấy tất cả đơn hàng ship (dành cho Admin)
    /// </summary>
    public async Task<IEnumerable<ShippingOrderDto>> GetAllShippingOrders()
    {
        try
        {
            var shippingOrders = await _unitOfWork.ShippingOrders
                .GetQueryable()
                .Include(so => so.Transaction)
                .ThenInclude(t => t.Image)
                .Include(so => so.Transaction.Customer)
                .ToListAsync();

            return shippingOrders.Select(so => new ShippingOrderDto
            {
                Id = so.Id,
                TransactionId = so.TransactionId,
                ImageId = so.Transaction.ImageId,
                ImageTitle = so.Transaction.Image.Title,
                ImageUrl = so.Transaction.Image.Url,
                CustomerId = so.Transaction.CustomerId,
                CustomerEmail = so.Transaction.Customer.Email,
                ShippingAddress = so.ShippingAddress,
                ShippingFee = so.ShippingFee,
                OrderAmount = so.Transaction.Amount,
                TotalAmount = so.Transaction.Amount + so.ShippingFee,
                Status = so.Status,
                ConfirmNote = so.ConfirmNote,
                PackagingNote = so.PackagingNote,
                ShippingNote = so.ShippingNote,
                DeliveryNote = so.DeliveryNote,
                DeliveryProofImageUrl = so.DeliveryProofImageUrl,
                CreatedAt = so.CreatedAt,
                UpdatedAt = so.UpdatedAt
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.Error($"Lỗi khi lấy tất cả đơn hàng ship: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    ///     Cập nhật trạng thái đơn hàng (dành cho Admin)
    /// </summary>
    public async Task<ShippingOrderDto> UpdateShippingOrderStatus(UpdateShippingStatusDto updateDto)
    {
        try
        {
            // Kiểm tra quyền Admin
            var currentUserRole = _claimService.GetCurrentUserRole();
            if (currentUserRole != "Admin")
                throw new UnauthorizedAccessException("Chỉ Admin mới có quyền cập nhật trạng thái đơn hàng");

            var shippingOrder = await _unitOfWork.ShippingOrders.GetByIdAsync(updateDto.OrderId);
            if (shippingOrder == null)
                throw new KeyNotFoundException($"Không tìm thấy đơn hàng ship với ID {updateDto.OrderId}");

            // Cập nhật trạng thái và ghi chú tương ứng
            shippingOrder.Status = updateDto.Status;

            switch (updateDto.Status)
            {
                case ShippingStatusEnum.Confirmed:
                    shippingOrder.ConfirmNote = updateDto.Note;
                    break;
                case ShippingStatusEnum.Packaging:
                    shippingOrder.PackagingNote = updateDto.Note;
                    break;
                case ShippingStatusEnum.Shipping:
                    shippingOrder.ShippingNote = updateDto.Note;
                    break;
                case ShippingStatusEnum.Delivered:
                    break;
            }

            shippingOrder.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.CompleteAsync();

            // Trả về thông tin chi tiết đơn hàng sau khi cập nhật
            return await GetShippingOrderById(shippingOrder.Id);
        }
        catch (Exception ex)
        {
            _logger.Error($"Lỗi khi cập nhật trạng thái đơn hàng ship {updateDto.OrderId}: {ex.Message}");
            throw;
        }
    }
}