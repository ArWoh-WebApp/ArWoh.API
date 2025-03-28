namespace ArWoh.API.DTOs.PaymentDTOs;

public class ImageSaleDetail
{
    public int ImageId { get; set; }
    public string Title { get; set; }
    public string FileName { get; set; }
    public string Url { get; set; }
    public int SalesCount { get; set; }
    public decimal TotalAmount { get; set; }
    public bool HasPhysicalPrints { get; set; }
    public int PhysicalPrintCount { get; set; }
    public int DigitalDownloadCount { get; set; }
}