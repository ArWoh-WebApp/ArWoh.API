using System.ComponentModel.DataAnnotations;
using ArWoh.API.Enums;

namespace ArWoh.API.DTOs.ShippingDTOs;

/// <summary>
///     DTO cho hình ảnh có thể đặt in và ship
/// </summary>
public class ShippableImageDto
{
    public int ImageId { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public decimal Price { get; set; }
    public string Url { get; set; }
    public DateTime PurchaseDate { get; set; }
    public int OrderId { get; set; } // Đơn hàng gốc khi mua ảnh
}

/// <summary>
///     DTO để tạo đơn hàng ship mới
/// </summary>
public class ShippingOrderCreateDto
{
    [Required] public List<int> ImageIds { get; set; } // Danh sách ID của các hình ảnh cần đặt in và ship

    [Required] public string ShippingAddress { get; set; } // Địa chỉ giao hàng
}

/// <summary>
///     DTO thông tin đơn hàng ship
/// </summary>
public class ShippingOrderDto
{
    public int OrderId { get; set; }
    public DateTime CreatedAt { get; set; }
    public decimal TotalAmount { get; set; } // Tổng tiền (giá gốc + phí ship)
    public decimal OriginalPrice { get; set; } // Giá gốc của ảnh
    public decimal ShippingFee { get; set; } // Phí ship
    public string ShippingAddress { get; set; } // Địa chỉ giao hàng
    public ShippingStatusEnum ShippingStatus { get; set; } // Trạng thái đơn hàng

    // Các ghi chú theo từng trạng thái
    public string ConfirmNote { get; set; }
    public string PackagingNote { get; set; }
    public string ShippingNote { get; set; }
    public string DeliveryNote { get; set; }
    public string CustomerName { get; set; }
    public string CustomerEmail { get; set; }
    public int CustomerId { get; set; }

    public string DeliveryProofImageUrl { get; set; } // URL hình ảnh chứng minh giao hàng
    public List<ShippingOrderDetailDto> OrderDetails { get; set; } // Chi tiết đơn hàng
}

/// <summary>
///     DTO chi tiết đơn hàng ship
/// </summary>
public class ShippingOrderDetailDto
{
    public int OrderDetailId { get; set; }
    public int ImageId { get; set; }
    public string ImageTitle { get; set; }
    public string ImageUrl { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}

/// <summary>
///     DTO cập nhật trạng thái đơn hàng ship
/// </summary>
public class ShippingStatusUpdateDto
{
    [Required] public ShippingStatusEnum Status { get; set; } // Trạng thái mới

    public string Note { get; set; } // Ghi chú khi cập nhật trạng thái
}