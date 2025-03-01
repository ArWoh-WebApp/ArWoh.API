using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ArWoh.API.Entities;

public class CartItem : BaseEntity
{
    [Required] public int ImageId { get; set; } // Ảnh được thêm vào giỏ

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
    public int Quantity { get; set; } = 1; // Số lượng (mặc định là 1)

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; } // Giá của ảnh tại thời điểm thêm vào giỏ

    [Required] public int CartId { get; set; } // Liên kết với giỏ hàng

    [ForeignKey("CartId")] public Cart Cart { get; set; }

    [ForeignKey("ImageId")] public Image Image { get; set; }
}