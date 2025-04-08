using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ArWoh.API.Entities;

public class OrderDetail : BaseEntity
{
    [Required] public int OrderId { get; set; }

    [Required] public int ImageId { get; set; }

    [Required] public int Quantity { get; set; } = 1;

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }

    public string? ImageTitle { get; set; }

    [ForeignKey("OrderId")] public Order Order { get; set; }

    [ForeignKey("ImageId")] public Image Image { get; set; }
}