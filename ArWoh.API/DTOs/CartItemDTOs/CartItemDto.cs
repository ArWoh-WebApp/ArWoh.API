namespace ArWoh.API.DTOs.CartItemDTOs;

public class CartItemDto
{
    public int CartItemId { get; set; }
    public int ImageId { get; set; }
    public string Title { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}