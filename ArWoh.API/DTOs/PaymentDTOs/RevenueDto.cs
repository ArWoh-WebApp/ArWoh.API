namespace ArWoh.API.DTOs.PaymentDTOs;

public class RevenueDto
{
    public decimal TotalRevenue { get; set; }
    public int TotalImagesSold { get; set; }
    public int TotalTransactions { get; set; }
    public List<ImageSaleDetail> ImageSaleDetails { get; set; } = new List<ImageSaleDetail>();
}