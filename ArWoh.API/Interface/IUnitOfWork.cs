using ArWoh.API.Entities;

namespace ArWoh.API.Interface;

public interface IUnitOfWork : IDisposable
{
    IGenericRepository<User> Users { get; }
    IGenericRepository<Image> Images { get; }
    IGenericRepository<PaymentTransaction> Transactions { get; }
    IGenericRepository<Order> Orders { get; }
    IGenericRepository<AdminAction> AdminActions { get; }
    IGenericRepository<CartItem> CartItems { get; }
    IGenericRepository<Cart> Carts { get; }
    IGenericRepository<Payment> Payments { get; }
    IGenericRepository<PaymentTransaction> PaymentTransactions { get; }
    Task<int> CompleteAsync();
}