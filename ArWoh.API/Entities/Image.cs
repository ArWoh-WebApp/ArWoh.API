namespace ArWoh.API.Entities;

public class Image : BaseEntity
{
    public string Title { get; set; }
    public string Description { get; set; }
    public string Url { get; set; }
    public decimal Price { get; set; }
    public int UserId { get; set; } // Foreign key

    public User User { get; set; }
    public ICollection<Transaction> Transactions { get; set; }
}
