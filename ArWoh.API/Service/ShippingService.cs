using ArWoh.API.Commons;
using ArWoh.API.DTOs.ShippingDTOs;
using ArWoh.API.Entities;
using ArWoh.API.Enums;
using ArWoh.API.Interface;
using Microsoft.EntityFrameworkCore;

namespace ArWoh.API.Service;

public class ShippingService : IShippingService
{
    private readonly IBlobService _blobService;
    private readonly IImageService _imageService;
    private readonly ILoggerService _loggerService;
    private readonly IUnitOfWork _unitOfWork;

    public ShippingService(IUnitOfWork unitOfWork, ILoggerService loggerService, IBlobService blobService,
        IImageService imageService)
    {
        _unitOfWork = unitOfWork;
        _loggerService = loggerService;
        _blobService = blobService;
        _imageService = imageService;
    }

    public async Task<IEnumerable<ShippableImageDto>> GetShippableImagesByUserId(int userId)
    {
        try
        {
            _loggerService.Info($"Getting shippable images for user {userId}");

            // Lấy tất cả các đơn hàng đã hoàn thành của user này
            var completedOrders = await _unitOfWork.Orders.FindAsync(
                o => o.CustomerId == userId && o.Status == OrderStatusEnum.Completed,
                o => o.OrderDetails);

            _loggerService.Info($"Found {completedOrders?.Count() ?? 0} completed orders for user {userId}");

            if (completedOrders == null || !completedOrders.Any())
            {
                _loggerService.Warn($"No completed orders found for user {userId}");
                return Enumerable.Empty<ShippableImageDto>();
            }

            // Log chi tiết số lượng order details
            var orderDetailsCount = completedOrders.Sum(o => o.OrderDetails?.Count() ?? 0);
            _loggerService.Info($"Found {orderDetailsCount} order details in completed orders for user {userId}");

            // Kiểm tra payment status
            foreach (var order in completedOrders)
            {
                var payments = await _unitOfWork.Payments.FindAsync(p => p.OrderId == order.Id);
                var hasSuccessfulPayment = payments.Any(p => p.Status == PaymentStatusEnum.COMPLETED);
                _loggerService.Info(
                    $"Order {order.Id}: Found {payments.Count()} payments, HasSuccessfulPayment: {hasSuccessfulPayment}");
            }

            // Danh sách ID các hình ảnh cần lấy thông tin
            var imageIds = completedOrders
                .SelectMany(o => o.OrderDetails)
                .Select(od => od.ImageId)
                .Distinct()
                .ToList();

            _loggerService.Info($"Found {imageIds.Count} unique image IDs from order details");

            // Lấy thông tin chi tiết của các hình ảnh
            var images = await _unitOfWork.Images.FindAsync(img => imageIds.Contains(img.Id));

            _loggerService.Info($"Retrieved {images?.Count() ?? 0} images from database");

            // Log thông báo nếu số lượng images lấy được khác với số lượng imageIds
            if (images.Count() != imageIds.Count)
            {
                _loggerService.Warn(
                    $"Mismatch between image IDs count ({imageIds.Count}) and retrieved images count ({images.Count()})");

                // Tìm những image ID không tồn tại trong database
                var missingImageIds = imageIds.Where(id => !images.Any(img => img.Id == id)).ToList();
                if (missingImageIds.Any())
                    _loggerService.Warn($"Missing images with IDs: {string.Join(", ", missingImageIds)}");
            }

            var result = new List<ShippableImageDto>();

            // Kiểm tra các hình ảnh từ những đơn hàng đã hoàn thành
            foreach (var order in completedOrders)
            {
                _loggerService.Warn($"Processing order {order.Id} with {order.OrderDetails?.Count() ?? 0} details");

                foreach (var orderDetail in order.OrderDetails)
                {
                    // Tìm thông tin hình ảnh
                    var image = images.FirstOrDefault(img => img.Id == orderDetail.ImageId);

                    if (image == null)
                    {
                        _loggerService.Warn(
                            $"Image with ID {orderDetail.ImageId} from order detail {orderDetail.Id} not found in database");
                        continue; // Skip nếu không tìm thấy image
                    }

                    // Thêm trực tiếp vào danh sách kết quả
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

                    _loggerService.Warn($"Added image {image.Id} to shippable images result");
                }
            }

            // Thêm Distinct() để loại bỏ các hình ảnh trùng lặp
            var distinctResult = result
                .DistinctBy(img => img.ImageId)
                .ToList();

            _loggerService.Info($"Total images found: {result.Count}, Distinct images: {distinctResult.Count}");

            return distinctResult;
        }
        catch (Exception e)
        {
            _loggerService.Error($"Error in GetShippableImagesByUserId for user {userId}: {e.Message}");
            _loggerService.Error($"Stack trace: {e.StackTrace}");
            throw;
        }
    }

    public async Task<ShippingOrderDto> CreateShippingOrder(ShippingOrderCreateDto orderDto, int userId)
    {
        try
        {
            _loggerService.Info(
                $"Creating shipping order for user {userId} with {orderDto?.ImageIds?.Count ?? 0} images");

            // Kiểm tra dữ liệu đầu vào
            if (orderDto == null || orderDto.ImageIds == null || !orderDto.ImageIds.Any())
            {
                _loggerService.Warn("No images selected for shipping order");
                throw new ArgumentException("No images selected for shipping order");
            }

            if (string.IsNullOrWhiteSpace(orderDto.ShippingAddress))
            {
                _loggerService.Warn("Shipping address is required");
                throw new ArgumentException("Shipping address is required");
            }

            // Lấy thông tin chi tiết của các hình ảnh được chọn
            var selectedImages = await _unitOfWork.Images.FindAsync(img => orderDto.ImageIds.Contains(img.Id));

            _loggerService.Info($"Found {selectedImages.Count()} images from {orderDto.ImageIds.Count} requested IDs");

            // Kiểm tra xem tất cả các hình ảnh có tồn tại không
            if (selectedImages.Count() != orderDto.ImageIds.Count)
            {
                var missingImageIds = orderDto.ImageIds.Where(id => !selectedImages.Any(img => img.Id == id));
                _loggerService.Warn($"Missing images with IDs: {string.Join(", ", missingImageIds)}");
                throw new ArgumentException("One or more images do not exist");
            }

            // Kiểm tra xem user có quyền đặt ship các hình ảnh này không (đã mua)
            foreach (var imageId in orderDto.ImageIds)
            {
                // Kiểm tra xem user đã mua ảnh này chưa
                var hasPurchased = await _unitOfWork.Orders.ExistsAsync(o => o.CustomerId == userId
                                                                             && o.Status == OrderStatusEnum.Completed
                                                                             && o.OrderDetails.Any(od =>
                                                                                 od.ImageId == imageId));

                _loggerService.Info($"Image {imageId}: User has purchased = {hasPurchased}");

                if (!hasPurchased)
                {
                    _loggerService.Warn($"User {userId} has not purchased image with ID {imageId}");
                    throw new UnauthorizedAccessException($"You have not purchased image with ID {imageId}");
                }

                // Đã loại bỏ việc kiểm tra xem ảnh đã được đặt ship trước đó chưa
            }

            // Tính toán giá trị đơn hàng
            var originalPrice = selectedImages.Sum(img => img.Price);
            var shippingFee = Math.Round(originalPrice * 0.2m, 2); // Phí ship = 20% giá gốc
            var totalAmount = originalPrice + shippingFee;

            _loggerService.Info(
                $"Order calculation: Original price = {originalPrice}, Shipping fee = {shippingFee}, Total = {totalAmount}");

            // Tạo đơn hàng mới
            var newOrder = new Order
            {
                CustomerId = userId,
                TotalAmount = totalAmount,
                Status = OrderStatusEnum.Pending, // Trạng thái ban đầu là Pending
                IsPhysicalPrint = true, // Đánh dấu đây là đơn hàng in ảnh vật lý
                ShippingAddress = orderDto.ShippingAddress,
                ShippingStatus = ShippingStatusEnum.Pending,
                ShippingFee = shippingFee,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Thêm đơn hàng vào database
            await _unitOfWork.Orders.AddAsync(newOrder);
            await _unitOfWork.CompleteAsync();

            _loggerService.Info($"Created new shipping order with ID {newOrder.Id}");

            // Tạo chi tiết đơn hàng cho từng hình ảnh
            var orderDetails = new List<OrderDetail>();
            foreach (var image in selectedImages)
            {
                var orderDetail = new OrderDetail
                {
                    OrderId = newOrder.Id,
                    ImageId = image.Id,
                    Quantity = 1, // Mặc định số lượng là 1
                    Price = image.Price,
                    ImageTitle = image.Title,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                orderDetails.Add(orderDetail);
                _loggerService.Warn($"Added order detail for image {image.Id} ({image.Title ?? "unnamed"})");
            }

            // Thêm chi tiết đơn hàng vào database
            await _unitOfWork.OrderDetails.AddRangeAsync(orderDetails);
            await _unitOfWork.CompleteAsync();

            _loggerService.Info($"Added {orderDetails.Count} order details to order {newOrder.Id}");

            // Chuẩn bị dữ liệu trả về
            var orderDetailsDto = orderDetails.Select(od =>
            {
                var img = selectedImages.FirstOrDefault(i => i.Id == od.ImageId);
                return new ShippingOrderDetailDto
                {
                    OrderDetailId = od.Id,
                    ImageId = od.ImageId,
                    ImageTitle = od.ImageTitle,
                    ImageUrl = img?.Url,
                    Price = od.Price,
                    Quantity = od.Quantity
                };
            }).ToList();

            // Tạo DTO kết quả
            var result = new ShippingOrderDto
            {
                OrderId = newOrder.Id,
                CreatedAt = newOrder.CreatedAt,
                TotalAmount = totalAmount,
                OriginalPrice = originalPrice,
                ShippingFee = shippingFee,
                ShippingAddress = newOrder.ShippingAddress,
                ShippingStatus = ShippingStatusEnum.Pending,
                OrderDetails = orderDetailsDto
            };

            _loggerService.Info(
                $"Successfully created shipping order {newOrder.Id} with {orderDetailsDto.Count} items");

            return result;
        }
        catch (Exception e)
        {
            _loggerService.Error($"Error in CreateShippingOrder: {e.Message}");
            _loggerService.Error($"Stack trace: {e.StackTrace}");
            throw;
        }
    }


    /// <summary>
    ///     Lấy danh sách đơn hàng ship của user có phân trang và lọc
    /// </summary>
    public async Task<Pagination<ShippingOrderDto>> GetUserShippingOrders(
        int userId,
        PaginationParameter paginationParams,
        ShippingOrderFilterDto filter)
    {
        try
        {
            // Bắt đầu với truy vấn cơ bản cho đơn hàng ship của user
            var query = _unitOfWork.Orders.GetQueryable(o => o.OrderDetails)
                .Where(o => o.CustomerId == userId && o.IsPhysicalPrint == true);

            // Áp dụng các bộ lọc nếu có
            if (filter != null)
            {
                // Lọc theo trạng thái vận chuyển
                if (filter.ShippingStatus.HasValue)
                    query = query.Where(o => o.ShippingStatus == filter.ShippingStatus.Value);

                // Lọc theo khoảng thời gian
                if (filter.StartDate.HasValue) query = query.Where(o => o.CreatedAt >= filter.StartDate.Value);

                if (filter.EndDate.HasValue)
                {
                    // Thêm 1 ngày để bao gồm cả ngày kết thúc
                    var endDatePlusOne = filter.EndDate.Value.AddDays(1);
                    query = query.Where(o => o.CreatedAt < endDatePlusOne);
                }

                // Lọc theo giá trị đơn hàng
                if (filter.MinAmount.HasValue) query = query.Where(o => o.TotalAmount >= filter.MinAmount.Value);

                if (filter.MaxAmount.HasValue) query = query.Where(o => o.TotalAmount <= filter.MaxAmount.Value);
            }

            // Lấy tổng số lượng đơn hàng phù hợp với điều kiện lọc
            var totalCount = await query.CountAsync();

            // Phân trang
            var orders = await query
                .OrderByDescending(o => o.CreatedAt) // Sắp xếp mới nhất lên đầu
                .Skip((paginationParams.PageIndex - 1) * paginationParams.PageSize)
                .Take(paginationParams.PageSize)
                .ToListAsync();

            if (orders == null || !orders.Any())
                return new Pagination<ShippingOrderDto>(new List<ShippingOrderDto>(), 0,
                    paginationParams.PageIndex, paginationParams.PageSize);

            // Lấy danh sách ID của tất cả hình ảnh
            var imageIds = orders
                .SelectMany(o => o.OrderDetails)
                .Select(od => od.ImageId)
                .Distinct()
                .ToList();

            // Lấy thông tin chi tiết của các hình ảnh
            var images = await _unitOfWork.Images.FindAsync(img => imageIds.Contains(img.Id));

            // Dictionary để map image ID -> Image object cho truy cập nhanh
            var imageDict = images.ToDictionary(img => img.Id);

            // Chuyển đổi dữ liệu sang DTO
            var result = new List<ShippingOrderDto>();
            foreach (var order in orders)
            {
                var orderDetailsDto = new List<ShippingOrderDetailDto>();

                foreach (var detail in order.OrderDetails)
                {
                    // Lấy thông tin hình ảnh từ dictionary
                    imageDict.TryGetValue(detail.ImageId, out var image);

                    orderDetailsDto.Add(new ShippingOrderDetailDto
                    {
                        OrderDetailId = detail.Id,
                        ImageId = detail.ImageId,
                        ImageTitle = detail.ImageTitle ?? image?.Title,
                        ImageUrl = image?.Url,
                        Price = detail.Price,
                        Quantity = detail.Quantity
                    });
                }

                // Tính toán giá gốc từ chi tiết đơn hàng
                var originalPrice = orderDetailsDto.Sum(d => d.Price * d.Quantity);

                result.Add(new ShippingOrderDto
                {
                    OrderId = order.Id,
                    CreatedAt = order.CreatedAt,
                    TotalAmount = order.TotalAmount,
                    OriginalPrice = originalPrice,
                    ShippingFee = order.ShippingFee ?? 0,
                    ShippingAddress = order.ShippingAddress,
                    ShippingStatus = order.ShippingStatus ?? ShippingStatusEnum.Pending,
                    ConfirmNote = order.ConfirmNote,
                    PackagingNote = order.PackagingNote,
                    ShippingNote = order.ShippingNote,
                    DeliveryNote = order.DeliveryNote,
                    DeliveryProofImageUrl = order.DeliveryProofImageUrl,
                    OrderDetails = orderDetailsDto
                });
            }

            // Trả về kết quả có phân trang
            return new Pagination<ShippingOrderDto>(result, totalCount,
                paginationParams.PageIndex, paginationParams.PageSize);
        }
        catch (Exception e)
        {
            _loggerService.Error($"Error in GetUserShippingOrders: {e.Message}");
            throw;
        }
    }

    /// <summary>
    ///     Lấy chi tiết đơn hàng ship theo ID
    /// </summary>
    public async Task<ShippingOrderDto> GetShippingOrderById(int orderId, int userId)
    {
        try
        {
            // Lấy đơn hàng ship cụ thể của user
            var order = await _unitOfWork.Orders.FirstOrDefaultAsync(
                o => o.Id == orderId && o.CustomerId == userId && o.IsPhysicalPrint == true,
                o => o.OrderDetails);

            if (order == null)
                throw new KeyNotFoundException($"Shipping order with ID {orderId} not found for this user");

            // Lấy danh sách ID của các hình ảnh trong đơn hàng
            var imageIds = order.OrderDetails
                .Select(od => od.ImageId)
                .Distinct()
                .ToList();

            // Lấy thông tin chi tiết của các hình ảnh
            var images = await _unitOfWork.Images.FindAsync(img => imageIds.Contains(img.Id));

            // Dictionary để map image ID -> Image object cho truy cập nhanh
            var imageDict = images.ToDictionary(img => img.Id);

            // Chuyển đổi chi tiết đơn hàng sang DTO
            var orderDetailsDto = new List<ShippingOrderDetailDto>();
            foreach (var detail in order.OrderDetails)
            {
                // Lấy thông tin hình ảnh từ dictionary
                imageDict.TryGetValue(detail.ImageId, out var image);

                orderDetailsDto.Add(new ShippingOrderDetailDto
                {
                    OrderDetailId = detail.Id,
                    ImageId = detail.ImageId,
                    ImageTitle = detail.ImageTitle ?? image?.Title,
                    ImageUrl = image?.Url,
                    Price = detail.Price,
                    Quantity = detail.Quantity
                });
            }

            // Tính toán giá gốc từ chi tiết đơn hàng
            var originalPrice = orderDetailsDto.Sum(d => d.Price * d.Quantity);

            // Tạo và trả về DTO kết quả
            return new ShippingOrderDto
            {
                OrderId = order.Id,
                CreatedAt = order.CreatedAt,
                TotalAmount = order.TotalAmount,
                OriginalPrice = originalPrice,
                ShippingFee = order.ShippingFee ?? 0,
                ShippingAddress = order.ShippingAddress,
                ShippingStatus = order.ShippingStatus ?? ShippingStatusEnum.Pending,
                ConfirmNote = order.ConfirmNote,
                PackagingNote = order.PackagingNote,
                ShippingNote = order.ShippingNote,
                DeliveryNote = order.DeliveryNote,
                DeliveryProofImageUrl = order.DeliveryProofImageUrl,
                OrderDetails = orderDetailsDto
            };
        }
        catch (KeyNotFoundException)
        {
            // Truyền lại exception này để controller xử lý trả về 404
            throw;
        }
        catch (Exception e)
        {
            _loggerService.Error($"Error in GetShippingOrderById: {e.Message}");
            throw;
        }
    }

    /// <summary>
    ///     Lấy tất cả đơn hàng ship trong hệ thống (dành cho Admin)
    /// </summary>
    public async Task<IEnumerable<ShippingOrderDto>> GetAllShippingOrders()
    {
        try
        {
            // Lấy tất cả đơn hàng ship trong hệ thống
            var orders = await _unitOfWork.Orders.FindAsync(
                o => o.IsPhysicalPrint == true,
                o => o.OrderDetails,
                o => o.Customer);

            if (orders == null || !orders.Any()) return Enumerable.Empty<ShippingOrderDto>();

            // Lấy danh sách ID của tất cả hình ảnh
            var imageIds = orders
                .SelectMany(o => o.OrderDetails)
                .Select(od => od.ImageId)
                .Distinct()
                .ToList();

            // Lấy thông tin chi tiết của các hình ảnh
            var images = await _unitOfWork.Images.FindAsync(img => imageIds.Contains(img.Id));

            // Dictionary để map image ID -> Image object cho truy cập nhanh
            var imageDict = images.ToDictionary(img => img.Id);

            // Chuyển đổi dữ liệu sang DTO
            var result = new List<ShippingOrderDto>();
            foreach (var order in orders)
            {
                var orderDetailsDto = new List<ShippingOrderDetailDto>();

                foreach (var detail in order.OrderDetails)
                {
                    // Lấy thông tin hình ảnh từ dictionary
                    imageDict.TryGetValue(detail.ImageId, out var image);

                    orderDetailsDto.Add(new ShippingOrderDetailDto
                    {
                        OrderDetailId = detail.Id,
                        ImageId = detail.ImageId,
                        ImageTitle = detail.ImageTitle ?? image?.Title,
                        ImageUrl = image?.Url,
                        Price = detail.Price,
                        Quantity = detail.Quantity
                    });
                }

                // Tính toán giá gốc từ chi tiết đơn hàng
                var originalPrice = orderDetailsDto.Sum(d => d.Price * d.Quantity);

                // Tạo DTO đơn hàng với thông tin khách hàng (cho admin)
                var shippingOrderDto = new ShippingOrderDto
                {
                    OrderId = order.Id,
                    CreatedAt = order.CreatedAt,
                    TotalAmount = order.TotalAmount,
                    OriginalPrice = originalPrice,
                    ShippingFee = order.ShippingFee ?? 0,
                    ShippingAddress = order.ShippingAddress,
                    ShippingStatus = order.ShippingStatus ?? ShippingStatusEnum.Pending,
                    ConfirmNote = order.ConfirmNote,
                    PackagingNote = order.PackagingNote,
                    ShippingNote = order.ShippingNote,
                    DeliveryNote = order.DeliveryNote,
                    DeliveryProofImageUrl = order.DeliveryProofImageUrl,
                    OrderDetails = orderDetailsDto,
                    // Thêm thông tin khách hàng cho admin
                    CustomerName = order.Customer?.Username,
                    CustomerEmail = order.Customer?.Email,
                    CustomerId = order.CustomerId
                };

                result.Add(shippingOrderDto);
            }

            // Sắp xếp theo thời gian tạo giảm dần (mới nhất lên đầu)
            return result.OrderByDescending(o => o.CreatedAt);
        }
        catch (Exception e)
        {
            _loggerService.Error($"Error in GetAllShippingOrders: {e.Message}");
            throw;
        }
    }

    /// <summary>
    ///     Cập nhật trạng thái đơn hàng ship
    /// </summary>
    public async Task<ShippingOrderDto> UpdateShippingOrderStatus(int orderId, ShippingStatusEnum newStatus,
        string note)
    {
        try
        {
            // Lấy đơn hàng cần cập nhật
            var order = await _unitOfWork.Orders.GetByIdAsync(orderId);

            if (order == null || !order.IsPhysicalPrint)
                throw new KeyNotFoundException($"Shipping order with ID {orderId} not found");

            // Kiểm tra trạng thái hiện tại và mới
            var currentStatus = order.ShippingStatus ?? ShippingStatusEnum.Pending;

            // Đảm bảo việc cập nhật trạng thái theo đúng quy trình
            // Chỉ được cập nhật từ trạng thái hiện tại lên trạng thái kế tiếp
            // hoặc giữ nguyên trạng thái để cập nhật ghi chú
            if (newStatus != currentStatus && !IsValidStatusTransition(currentStatus, newStatus))
                throw new InvalidOperationException($"Invalid status transition from {currentStatus} to {newStatus}");

            // Cập nhật trạng thái và ghi chú tương ứng
            order.ShippingStatus = newStatus;
            order.UpdatedAt = DateTime.UtcNow;

            // Cập nhật ghi chú tương ứng với trạng thái
            switch (newStatus)
            {
                case ShippingStatusEnum.Confirmed:
                    order.ConfirmNote = note;
                    break;
                case ShippingStatusEnum.Packaging:
                    order.PackagingNote = note;
                    break;
                case ShippingStatusEnum.Shipping:
                    order.ShippingNote = note;
                    break;
                case ShippingStatusEnum.Delivered:
                    order.DeliveryNote = note;
                    break;
            }

            // Cập nhật đơn hàng trong database
            _unitOfWork.Orders.Update(order);
            await _unitOfWork.CompleteAsync();

            // Lấy thông tin chi tiết đơn hàng sau khi cập nhật
            return await GetShippingOrderDetails(orderId);
        }
        catch (Exception e)
        {
            _loggerService.Error($"Error in UpdateShippingOrderStatus: {e.Message}");
            throw;
        }
    }

    /// <summary>
    ///     Upload hình ảnh chứng minh giao hàng
    /// </summary>
    public async Task<ShippingOrderDto> UploadDeliveryProofImage(int orderId, IFormFile image)
    {
        try
        {
            if (image == null || image.Length == 0) throw new ArgumentException("No image file provided");

            // Kiểm tra định dạng file
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var fileExtension = Path.GetExtension(image.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(fileExtension))
                throw new ArgumentException("Invalid file format. Only JPG, JPEG, PNG, and GIF are supported.");

            // Lấy đơn hàng cần cập nhật
            var order = await _unitOfWork.Orders.GetByIdAsync(orderId);

            if (order == null || !order.IsPhysicalPrint)
                throw new KeyNotFoundException($"Shipping order with ID {orderId} not found");

            // Kiểm tra trạng thái đơn hàng
            if (order.ShippingStatus != ShippingStatusEnum.Delivered &&
                order.ShippingStatus != ShippingStatusEnum.Shipping)
                throw new InvalidOperationException(
                    "Delivery proof image can only be uploaded for orders in Shipping or Delivered or SuperSAyan 3 status");

            // Tạo tên file duy nhất
            var fileName = $"delivery_proof_{orderId}_{DateTime.UtcNow.Ticks}{fileExtension}";

            // Upload file lên blob storage
            using (var stream = image.OpenReadStream())
            {
                await _blobService.UploadFileAsync(fileName, stream);
            }

            // Tạo URL cho hình ảnh (giả định rằng BlobService trả về URL dựa trên tên file)
            // Nếu bạn cần URL đầy đủ, bạn cần điều chỉnh theo logic phù hợp của hệ thống
            var imageUrl = $"/storage/delivery-proofs/{fileName}";

            // Cập nhật URL hình ảnh trong đơn hàng
            order.DeliveryProofImageUrl = imageUrl;
            order.UpdatedAt = DateTime.UtcNow;

            // Cập nhật đơn hàng trong database
            _unitOfWork.Orders.Update(order);
            await _unitOfWork.CompleteAsync();

            // Lấy thông tin chi tiết đơn hàng sau khi cập nhật
            return await GetShippingOrderDetails(orderId);
        }
        catch (Exception e)
        {
            _loggerService.Error($"Error in UploadDeliveryProofImage: {e.Message}");
            throw;
        }
    }

    #region Helper Methods

    /// <summary>
    ///     Kiểm tra xem việc chuyển đổi trạng thái có hợp lệ không
    /// </summary>
    private bool IsValidStatusTransition(ShippingStatusEnum currentStatus, ShippingStatusEnum newStatus)
    {
        // Quy tắc chuyển đổi trạng thái:
        // Pending -> Confirmed -> Packaging -> Shipping -> Delivered
        switch (currentStatus)
        {
            case ShippingStatusEnum.Pending:
                return newStatus == ShippingStatusEnum.Confirmed;
            case ShippingStatusEnum.Confirmed:
                return newStatus == ShippingStatusEnum.Packaging;
            case ShippingStatusEnum.Packaging:
                return newStatus == ShippingStatusEnum.Shipping;
            case ShippingStatusEnum.Shipping:
                return newStatus == ShippingStatusEnum.Delivered;
            case ShippingStatusEnum.Delivered:
                return false; // Không thể chuyển từ Delivered sang trạng thái khác
            default:
                return false;
        }
    }

    /// <summary>
    ///     Lấy thông tin chi tiết đơn hàng ship (helper method)
    /// </summary>
    private async Task<ShippingOrderDto> GetShippingOrderDetails(int orderId)
    {
        // Lấy đơn hàng với tất cả các chi tiết và thông tin khách hàng
        var order = await _unitOfWork.Orders.FirstOrDefaultAsync(
            o => o.Id == orderId && o.IsPhysicalPrint == true,
            o => o.OrderDetails,
            o => o.Customer);

        if (order == null) throw new KeyNotFoundException($"Shipping order with ID {orderId} not found");

        // Lấy danh sách ID của các hình ảnh trong đơn hàng
        var imageIds = order.OrderDetails
            .Select(od => od.ImageId)
            .Distinct()
            .ToList();

        // Lấy thông tin chi tiết của các hình ảnh
        var images = await _unitOfWork.Images.FindAsync(img => imageIds.Contains(img.Id));

        // Dictionary để map image ID -> Image object cho truy cập nhanh
        var imageDict = images.ToDictionary(img => img.Id);

        // Chuyển đổi chi tiết đơn hàng sang DTO
        var orderDetailsDto = new List<ShippingOrderDetailDto>();
        foreach (var detail in order.OrderDetails)
        {
            // Lấy thông tin hình ảnh từ dictionary
            imageDict.TryGetValue(detail.ImageId, out var image);

            orderDetailsDto.Add(new ShippingOrderDetailDto
            {
                OrderDetailId = detail.Id,
                ImageId = detail.ImageId,
                ImageTitle = detail.ImageTitle ?? image?.Title,
                ImageUrl = image?.Url,
                Price = detail.Price,
                Quantity = detail.Quantity
            });
        }

        // Tính toán giá gốc từ chi tiết đơn hàng
        var originalPrice = orderDetailsDto.Sum(d => d.Price * d.Quantity);

        // Tạo và trả về DTO kết quả
        return new ShippingOrderDto
        {
            OrderId = order.Id,
            CreatedAt = order.CreatedAt,
            TotalAmount = order.TotalAmount,
            OriginalPrice = originalPrice,
            ShippingFee = order.ShippingFee ?? 0,
            ShippingAddress = order.ShippingAddress,
            ShippingStatus = order.ShippingStatus ?? ShippingStatusEnum.Pending,
            ConfirmNote = order.ConfirmNote,
            PackagingNote = order.PackagingNote,
            ShippingNote = order.ShippingNote,
            DeliveryNote = order.DeliveryNote,
            DeliveryProofImageUrl = order.DeliveryProofImageUrl,
            OrderDetails = orderDetailsDto,
            // Thêm thông tin khách hàng cho admin
            CustomerName = order.Customer?.Username,
            CustomerEmail = order.Customer?.Email,
            CustomerId = order.CustomerId
        };
    }

    #endregion
}