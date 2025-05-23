using ArWoh.API.Entities;

namespace ArWoh.API.Interface;

public interface IUnitOfWork : IDisposable
{
    IGenericRepository<User> Users { get; }
    IGenericRepository<Image> Images { get; }
    IGenericRepository<Order> Orders { get; }
    IGenericRepository<OrderDetail> OrderDetails { get; }
    IGenericRepository<AdminAction> AdminActions { get; }
    IGenericRepository<CartItem> CartItems { get; }
    IGenericRepository<Cart> Carts { get; }
    IGenericRepository<Payment> Payments { get; }


    Task<int> CompleteAsync();
}