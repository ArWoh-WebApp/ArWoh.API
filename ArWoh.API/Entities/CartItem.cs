using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ArWoh.API.Entities;

public class CartItem : BaseEntity
{
    public int ImageId { get; set; } // Ảnh được thêm vào giỏ


    public int Quantity { get; set; } = 1; // Số lượng (mặc định là 1)

    public string? ImageTitle { get; set; }

    public decimal Price { get; set; } // Giá của ảnh tại thời điểm thêm vào giỏ

    [Required] public int CartId { get; set; } // Liên kết với giỏ hàng

    public Cart Cart { get; set; }

    public Image Image { get; set; }
}