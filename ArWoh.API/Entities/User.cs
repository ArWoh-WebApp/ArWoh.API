namespace ArWoh.API.Entities;

public class User : BaseEntity
{
    public string Username { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }
    public string Role { get; set; } = "User"; // "User", "Admin"

    public ICollection<Image> Images { get; set; }
    public ICollection<Transaction> Transactions { get; set; }
}
