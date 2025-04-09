namespace ArWoh.API.DTOs.OrderDTOs;

public class OrderDetailDto
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int ImageId { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public string? ImageTitle { get; set; }
    public ImageBasicInfoDto Image { get; set; }
}