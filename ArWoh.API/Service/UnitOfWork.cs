using ArWoh.API.Entities;
using ArWoh.API.Interface;
using ArWoh.API.Repository;

namespace ArWoh.API.Service;

public class UnitOfWork : IUnitOfWork
{
    private readonly ArWohDbContext _context;

    public UnitOfWork(ArWohDbContext context)
    {
        _context = context;
        Users = new GenericRepository<User>(_context);
        Images = new GenericRepository<Image>(_context);
        PaymentTransactions = new GenericRepository<PaymentTransaction>(_context);
        Orders = new GenericRepository<Order>(_context);
        AdminActions = new GenericRepository<AdminAction>(_context);
        Carts = new GenericRepository<Cart>(_context);
        CartItems = new GenericRepository<CartItem>(_context);
        Payments = new GenericRepository<Payment>(_context);
        ShippingOrders = new GenericRepository<ShippingOrder>(_context);
    }

    public IGenericRepository<User> Users { get; }
    public IGenericRepository<Image> Images { get; }
    public IGenericRepository<PaymentTransaction> PaymentTransactions { get; }
    public IGenericRepository<Order> Orders { get; }
    public IGenericRepository<AdminAction> AdminActions { get; }
    public IGenericRepository<Cart> Carts { get; }
    public IGenericRepository<CartItem> CartItems { get; }
    public IGenericRepository<Payment> Payments { get; }
    public IGenericRepository<ShippingOrder> ShippingOrders { get; }

    public async Task<int> CompleteAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}