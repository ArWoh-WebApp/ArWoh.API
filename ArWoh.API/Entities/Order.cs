namespace ArWoh.API.Entities;

public class Order : BaseEntity
{
    public int TransactionId { get; set; }
    public string DownloadUrl { get; set; }
    public DateTime ExpiryDate { get; set; }

    public Transaction Transaction { get; set; }
}
