using ArWoh.API.DTOs.OrderDTOs;
using ArWoh.API.Entities;
using ArWoh.API.Enums;
using ArWoh.API.Interface;

namespace ArWoh.API.Service;

public class OrderService : IOrderService
{
    private readonly ILoggerService _loggerService;
    private readonly IClaimService _claimService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICartService _cartService;

    public OrderService(ILoggerService loggerService, IClaimService claimService, IUnitOfWork unitOfWork,
        ICartService cartService)
    {
        _loggerService = loggerService;
        _claimService = claimService;
        _unitOfWork = unitOfWork;
        _cartService = cartService;
    }

    public async Task<OrderDto> CreateOrderFromCart(CreateOrderDto createOrderDto)
    {
        try
        {
            // Lấy ID của người dùng hiện tại
            var currentUserId = _claimService.GetCurrentUserId();
            _loggerService.Info($"Creating order for user {currentUserId}");

            // Lấy giỏ hàng của người dùng
            var cart = await _cartService.GetCartByUserId(currentUserId);

            if (cart.CartItems == null || !cart.CartItems.Any())
            {
                throw new InvalidOperationException("Cart is empty. Cannot create order.");
            }

            // Tạo order mới
            var order = new Order
            {
                CustomerId = currentUserId,
                Status = OrderStatusEnum.Pending,
                IsPhysicalPrint = createOrderDto.IsPhysicalPrint,
                ShippingAddress = createOrderDto.ShippingAddress,
                ShippingFee = createOrderDto.IsPhysicalPrint ? createOrderDto.ShippingFee : null,
                ShippingStatus = createOrderDto.IsPhysicalPrint ? ShippingStatusEnum.Pending : null,
                OrderDetails = new List<OrderDetail>()
            };

            // Tạo OrderDetail từ CartItem
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

            // Tính TotalAmount (tổng giá trị các sản phẩm + phí vận chuyển nếu có)
            order.TotalAmount = order.OrderDetails.Sum(od => od.Price * od.Quantity);
            if (createOrderDto.IsPhysicalPrint && createOrderDto.ShippingFee.HasValue)
            {
                order.TotalAmount += createOrderDto.ShippingFee.Value;
            }

            // Lưu Order vào database
            await _unitOfWork.Orders.AddAsync(order);
            await _unitOfWork.CompleteAsync();

            // Tạo Payment với trạng thái PENDING
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

            // Xóa các CartItem đã được chuyển thành OrderDetail
            var cartItems = await _unitOfWork.CartItems.FindAsync(ci => ci.CartId == cart.UserId);
            _unitOfWork.CartItems.DeleteRange(cartItems);
            await _unitOfWork.CompleteAsync();

            _loggerService.Success($"Successfully created order {order.Id} for user {currentUserId}");

            // Trả về OrderDto
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
        catch (Exception ex)
        {
            _loggerService.Error($"Error creating order: {ex.Message}");
            throw new Exception("An error occurred while creating the order.", ex);
        }
    }
}