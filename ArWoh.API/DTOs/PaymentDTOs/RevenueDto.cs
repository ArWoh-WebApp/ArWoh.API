using ArWoh.API.Enums;

namespace ArWoh.API.DTOs.PaymentDTOs;

public class RevenueDto
{
    public decimal TotalRevenue { get; set; }
    public int TotalImagesSold { get; set; }
    public List<ImageSalesDetail> ImageSales { get; set; } = new List<ImageSalesDetail>();
}

public class ImageSalesDetail
{
    public int ImageId { get; set; }
    public string ImageTitle { get; set; }
    public string ImageUrl { get; set; }
    public int SalesCount { get; set; }
    public decimal TotalAmount { get; set; }
}

public class TransactionDetail
{
    public int TransactionId { get; set; }
    public DateTime PurchaseDate { get; set; }
    public decimal Amount { get; set; }
    public bool IsPhysicalPrint { get; set; }
    public PaymentTransactionStatusEnum Status { get; set; }
}