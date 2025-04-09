namespace ArWoh.API.Entities;

public class Cart : BaseEntity
{
    public int UserId { get; set; }
    public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    public decimal TotalPrice => CartItems?.Sum(item => item.Price * item.Quantity) ?? 0;

    public User User { get; set; }
}