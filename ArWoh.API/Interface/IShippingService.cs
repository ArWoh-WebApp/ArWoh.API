using ArWoh.API.DTOs.ShippingDTOs;
using ArWoh.API.Enums;

namespace ArWoh.API.Interface;

public interface IShippingService
{
    /// <summary>
    /// Tạo đơn hàng ship mới cho các hình ảnh đã mua
    /// </summary>
    /// <param name="orderDto">Thông tin tạo đơn hàng ship</param>
    /// <param name="userId">ID của người dùng</param>
    /// <returns>Thông tin đơn hàng ship đã tạo</returns>
    Task<ShippingOrderDto> CreateShippingOrder(ShippingOrderCreateDto orderDto, int userId);

    /// <summary>
    /// Lấy danh sách tất cả đơn hàng ship của người dùng
    /// </summary>
    /// <param name="userId">ID của người dùng</param>
    /// <returns>Danh sách đơn hàng ship</returns>
    Task<IEnumerable<ShippingOrderDto>> GetUserShippingOrders(int userId);

    /// <summary>
    /// Lấy danh sách hình ảnh có thể ship của người dùng
    /// </summary>
    /// <param name="userId">ID của người dùng</param>
    /// <returns>Danh sách hình ảnh có thể ship</returns>
    Task<IEnumerable<ShippableImageDto>> GetShippableImagesByUserId(int userId);

    /// <summary>
    /// Lấy chi tiết một đơn hàng ship cụ thể của người dùng
    /// </summary>
    /// <param name="orderId">ID của đơn hàng</param>
    /// <param name="userId">ID của người dùng</param>
    /// <returns>Chi tiết đơn hàng ship</returns>
    Task<ShippingOrderDto> GetShippingOrderById(int orderId, int userId);

    /// <summary>
    /// Lấy danh sách tất cả đơn hàng ship trong hệ thống (dành cho Admin)
    /// </summary>
    /// <returns>Danh sách tất cả đơn hàng ship</returns>
    Task<IEnumerable<ShippingOrderDto>> GetAllShippingOrders();

    /// <summary>
    /// Cập nhật trạng thái của đơn hàng ship (dành cho Admin)
    /// </summary>
    /// <param name="orderId">ID của đơn hàng</param>
    /// <param name="newStatus">Trạng thái mới</param>
    /// <param name="note">Ghi chú khi cập nhật trạng thái</param>
    /// <returns>Đơn hàng ship sau khi cập nhật</returns>
    Task<ShippingOrderDto> UpdateShippingOrderStatus(int orderId, ShippingStatusEnum newStatus,
        string note);

    /// <summary>
    /// Upload hình ảnh chứng minh giao hàng (dành cho Admin)
    /// </summary>
    /// <param name="orderId">ID của đơn hàng</param>
    /// <param name="image">File hình ảnh</param>
    /// <returns>Đơn hàng ship sau khi cập nhật</returns>
    Task<ShippingOrderDto> UploadDeliveryProofImage(int orderId, IFormFile image);
}