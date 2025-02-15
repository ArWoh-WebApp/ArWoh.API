using ArWoh.API.Enums;

namespace ArWoh.API.Entities;

public class User : BaseEntity
{
    public string Username { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }
    public UserRole Role { get; set; }

    public ICollection<Image> Images { get; set; }
    public ICollection<Transaction> Transactions { get; set; }
}
