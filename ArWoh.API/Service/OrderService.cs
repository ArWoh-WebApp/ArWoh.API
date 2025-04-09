using ArWoh.API.DTOs.OrderDTOs;
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

            // PHASE 6: TẠO PAYMENT CHO ĐƠN HÀNG
            // Tạo bản ghi thanh toán với trạng thái chờ
            var payment = new Payment
            {
                OrderId = order.Id,
                Amount = order.TotalAmount,
                PaymentGateway = createOrderDto.PaymentGateway,
                Status = PaymentStatusEnum.PENDING,
                RedirectUrl = createOrderDto.RedirectUrl
            };

            await _unitOfWork.Payments.AddAsync(payment);
            await _unitOfWork.CompleteAsync();

            // PHASE 7: XÓA GIỎ HÀNG SAU KHI TẠO ĐƠN HÀNG
            // Xóa các item trong giỏ hàng đã được chuyển đổi
            await _cartService.ClearCartItems(userId);

            _loggerService.Success($"Successfully created order {order.Id} for user {userId}");

            // PHASE 8: MAPPING KẾT QUẢ THÀNH DTO
            // Chuyển đổi dữ liệu sang DTO để trả về client
            return MapToOrderDto(order, payment);
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
}