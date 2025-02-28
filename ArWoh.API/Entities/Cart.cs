using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ArWoh.API.Entities;

public class Cart : BaseEntity
{
    [Required]
    public int UserId { get; set; } 

    [InverseProperty("Cart")]
    public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>(); 

    public decimal TotalPrice => CartItems?.Sum(item => item.Price * item.Quantity) ?? 0; 

    [ForeignKey("UserId")]
    public User User { get; set; }
}