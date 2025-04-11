namespace ArWoh.API.DTOs.PaymentDTOs;

public class RevenueDto
{
    public decimal TotalRevenue { get; set; }
    public int TotalImagesSold { get; set; }
    public List<ImageSalesDetail> ImageSales { get; set; } = new();
}

public class ImageSalesDetail
{
    public int ImageId { get; set; }
    public string ImageTitle { get; set; }
    public string ImageUrl { get; set; }
    public int SalesCount { get; set; }
    public decimal TotalAmount { get; set; }
}