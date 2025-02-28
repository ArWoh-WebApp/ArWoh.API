using ArWoh.API.Entities;

namespace ArWoh.API.Interface;

public interface IUnitOfWork : IDisposable
{
    IGenericRepository<User> Users { get; }
    IGenericRepository<Image> Images { get; }
    IGenericRepository<Transaction> Transactions { get; }
    IGenericRepository<Order> Orders { get; }
    IGenericRepository<AdminAction> AdminActions { get; }
    IGenericRepository<CartItem> CartItems { get; }
    IGenericRepository<Cart> Carts { get; }
    Task<int> CompleteAsync();
}
