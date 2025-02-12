using ArWoh.API.Entities;
using ArWoh.API.Interface;
using ArWoh.API.Repository;

namespace ArWoh.API.Service;

public class UnitOfWork : IUnitOfWork
{
    private readonly ArWohDbContext _context;

    public IGenericRepository<User> Users { get; }
    public IGenericRepository<Image> Images { get; }
    public IGenericRepository<Transaction> Transactions { get; }
    public IGenericRepository<Order> Orders { get; }
    public IGenericRepository<AdminAction> AdminActions { get; }

    public UnitOfWork(ArWohDbContext context)
    {
        _context = context;
        Users = new GenericRepository<User>(_context);
        Images = new GenericRepository<Image>(_context);
        Transactions = new GenericRepository<Transaction>(_context);
        Orders = new GenericRepository<Order>(_context);
        AdminActions = new GenericRepository<AdminAction>(_context);
    }

    public async Task<int> CompleteAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
