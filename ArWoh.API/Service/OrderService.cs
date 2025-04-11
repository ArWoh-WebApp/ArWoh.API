using ArWoh.API.DTOs.OrderDTOs;
using ArWoh.API.DTOs.PaymentDTOs;
using ArWoh.API.Entities;
using ArWoh.API.Enums;
using ArWoh.API.Interface;

namespace ArWoh.API.Service;

public class OrderService : IOrderService
{
    private readonly ICartService _cartService;
    private readonly IClaimService _claimService;
    private readonly ILoggerService _loggerService;
    private readonly IUnitOfWork _unitOfWork;

    public OrderService(ILoggerService loggerService, IClaimService claimService, IUnitOfWork unitOfWork,
        ICartService cartService)
    {
        _loggerService = loggerService;
        _claimService = claimService;
        _unitOfWork = unitOfWork;
        _cartService = cartService;
    }

    public async Task<RevenueDto> GetPhotographerRevenue(int photographerId)
    {
        try
        {
            // PHASE 1: Get all images by the photographer
            var photographerImages = await _unitOfWork.Images.FindAsync(
                img => img.PhotographerId == photographerId && !img.IsDeleted);

            if (!photographerImages.Any())
                return new RevenueDto
                {
                    TotalRevenue = 0,
                    TotalImagesSold = 0,
                    ImageSales = new List<ImageSalesDetail>()
                };

            var imageIds = photographerImages.Select(img => img.Id).ToList();

            // PHASE 2: Get all order details containing these images
            var allOrderDetails = await _unitOfWork.OrderDetails.FindAsync(
                od => imageIds.Contains(od.ImageId),
                od => od.Order,
                od => od.Image);

            // PHASE 3: Filter to only include completed orders with successful payments
            var validOrderDetails = new List<OrderDetail>();
            foreach (var detail in allOrderDetails)
            {
                // Get the order with payments
                var order = await _unitOfWork.Orders.GetByIdAsync(detail.OrderId, o => o.Payments);

                // Check if order is completed and has successful payment
                if (order != null &&
                    order.Status == OrderStatusEnum.Completed &&
                    order.Payments.Any(p => p.Status == PaymentStatusEnum.COMPLETED))
                    validOrderDetails.Add(detail);
            }

            // PHASE 4: Calculate the total revenue and build the result
            var result = new RevenueDto
            {
                TotalRevenue = validOrderDetails.Sum(od => od.Price * od.Quantity),
                TotalImagesSold = validOrderDetails.Sum(od => od.Quantity)
            };

            // PHASE 5: Group by image to get per-image statistics
            var imageSalesDetails = validOrderDetails
                .GroupBy(od => od.ImageId)
                .Select(group =>
                {
                    var image = group.First().Image;
                    return new ImageSalesDetail
                    {
                        ImageId = image.Id,
                        ImageTitle = image.Title ?? "Untitled",
                        ImageUrl = image.Url ?? "",
                        SalesCount = group.Sum(od => od.Quantity),
                        TotalAmount = group.Sum(od => od.Price * od.Quantity)
                    };
                })
                .OrderByDescending(i => i.TotalAmount)
                .ToList();

            result.ImageSales = imageSalesDetails;

            return result;
        }
        catch (Exception ex)
        {
            _loggerService.Error($"Error getting revenue for photographer {photographerId}: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    ///     Lấy thông tin đơn hàng
    /// </summary>
    public async Task<OrderDto> GetOrderById(int orderId)
    {
        try
        {
            // Lấy order kèm các quan hệ
            var order = await _unitOfWork.Orders.GetByIdAsync(
                orderId,
                o => o.Customer,
                o => o.OrderDetails,
                o => o.Payments
            );

            if (order == null)
                throw new InvalidOperationException($"Không tìm thấy đơn hàng với ID: {orderId}");

            // Lấy payment liên quan đến order
            var payment = await _unitOfWork.Payments.FirstOrDefaultAsync(p => p.OrderId == orderId);
            if (payment == null)
                throw new InvalidOperationException($"Không tìm thấy thông tin thanh toán cho đơn hàng: {orderId}");

            _loggerService.Info($"Retrieved order {orderId} successfully");

            // Sử dụng helper method có sẵn để chuyển sang DTO
            return MapToOrderDto(order, payment);
        }
        catch (Exception e)
        {
            _loggerService.Error($"Error retrieving order {orderId}: {e.Message}");
            throw;
        }
    }


    public async Task<OrderDto> CreateOrderFromCart(int userId, CreateOrderDto createOrderDto)
    {
        try
        {
            // PHASE 1: KIỂM TRA GIỎ HÀNG
            // Lấy giỏ hàng và kiểm tra tính hợp lệ
            _loggerService.Info($"Creating order for user {userId}");
            var cart = await _cartService.GetCartByUserId(userId);

            if (cart.CartItems == null || !cart.CartItems.Any())
                throw new InvalidOperationException("Cart is empty. Cannot create order.");

            // PHASE 2: TẠO ĐƠN HÀNG
            // Tạo Order với thông tin cơ bản
            var order = new Order
            {
                CustomerId = userId,
                Status = OrderStatusEnum.Pending,
                IsPhysicalPrint = createOrderDto.IsPhysicalPrint,
                ShippingAddress = createOrderDto.ShippingAddress,
                ShippingFee = createOrderDto.IsPhysicalPrint ? createOrderDto.ShippingFee : null,
                ShippingStatus = createOrderDto.IsPhysicalPrint ? ShippingStatusEnum.Pending : null,
                OrderDetails = new List<OrderDetail>()
            };

            // PHASE 3: CHUYỂN ĐỔI CART ITEMS THÀNH ORDER DETAILS
            // Tạo chi tiết đơn hàng từ giỏ hàng
            foreach (var cartItem in cart.CartItems)
            {
                var orderDetail = new OrderDetail
                {
                    ImageId = cartItem.ImageId,
                    Quantity = cartItem.Quantity,
                    Price = cartItem.Price,
                    ImageTitle = cartItem.ImageTitle
                };

                order.OrderDetails.Add(orderDetail);
            }

            // PHASE 4: TÍNH TỔNG GIÁ TRỊ ĐƠN HÀNG
            // Tính tổng tiền sản phẩm + phí vận chuyển (nếu có)
            order.TotalAmount = order.OrderDetails.Sum(od => od.Price * od.Quantity);
            if (createOrderDto.IsPhysicalPrint && createOrderDto.ShippingFee.HasValue)
                order.TotalAmount += createOrderDto.ShippingFee.Value;

            // PHASE 5: LƯU ĐƠN HÀNG VÀO DATABASE
            await _unitOfWork.Orders.AddAsync(order);
            await _unitOfWork.CompleteAsync();

            // BỎ PHASE 6 (TẠO PAYMENT)

            // PHASE 7: XÓA GIỎ HÀNG 
            await _cartService.ClearCartItems(userId);

            _loggerService.Success($"Successfully created order {order.Id} for user {userId}");

            // PHASE 8: MAPPING KẾT QUẢ THÀNH DTO
            // Trả về DTO không có thông tin payment
            return MapToOrderDtoWithoutPayment(order);
        }
        catch (Exception ex)
        {
            _loggerService.Error($"Error creating order: {ex.Message}");
            throw new Exception("An error occurred while creating the order.", ex);
        }
    }


    /// <summary>
    ///     Cập nhật trạng thái đơn hàng
    /// </summary>
    public async Task<OrderDto> UpdateOrderStatus(int orderId, OrderStatusEnum status)
    {
        try
        {
            var order = await _unitOfWork.Orders.GetByIdAsync(
                orderId,
                o => o.OrderDetails,
                o => o.Payments
            );

            if (order == null)
                throw new InvalidOperationException($"Không tìm thấy đơn hàng với ID: {orderId}");

            // Cập nhật trạng thái
            order.Status = status;
            order.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Orders.Update(order);
            await _unitOfWork.CompleteAsync();

            _loggerService.Info($"Updated order {orderId} status to {status}");

            // Lấy payment mới nhất của order
            var payment = await _unitOfWork.Payments.FirstOrDefaultAsync(p => p.OrderId == orderId);

            return MapToOrderDto(order, payment);
        }
        catch (Exception e)
        {
            _loggerService.Error($"Error updating order {orderId} status: {e.Message}");
            throw;
        }
    }

    // Helper method để mapping từ model sang DTO
    private OrderDto MapToOrderDto(Order order, Payment payment)
    {
        return new OrderDto
        {
            Id = order.Id,
            CustomerId = order.CustomerId,
            TotalAmount = order.TotalAmount,
            Status = order.Status.ToString(),
            IsPhysicalPrint = order.IsPhysicalPrint,
            ShippingAddress = order.ShippingAddress,
            ShippingStatus = order.ShippingStatus?.ToString(),
            ShippingFee = order.ShippingFee,
            OrderDetails = order.OrderDetails.Select(od => new OrderDetailDto
            {
                Id = od.Id,
                OrderId = od.OrderId,
                ImageId = od.ImageId,
                Quantity = od.Quantity,
                Price = od.Price,
                ImageTitle = od.ImageTitle
            }).ToList(),
            PaymentInfo = new PaymentInfoDto
            {
                Id = payment.Id,
                OrderId = payment.OrderId,
                Amount = payment.Amount,
                PaymentGateway = payment.PaymentGateway.ToString(),
                Status = payment.Status.ToString(),
                PaymentUrl = payment.PaymentUrl
            }
        };
    }

    // Thêm phương thức mapping mới không cần payment
    private OrderDto MapToOrderDtoWithoutPayment(Order order)
    {
        return new OrderDto
        {
            Id = order.Id,
            CustomerId = order.CustomerId,
            TotalAmount = order.TotalAmount,
            Status = order.Status.ToString(),
            IsPhysicalPrint = order.IsPhysicalPrint,
            ShippingAddress = order.ShippingAddress,
            ShippingStatus = order.ShippingStatus?.ToString(),
            ShippingFee = order.ShippingFee,
            OrderDetails = order.OrderDetails.Select(od => new OrderDetailDto
            {
                Id = od.Id,
                OrderId = od.OrderId,
                ImageId = od.ImageId,
                Quantity = od.Quantity,
                Price = od.Price,
                ImageTitle = od.ImageTitle
            }).ToList(),
            PaymentInfo = null // Không có thông tin payment
        };
    }
}