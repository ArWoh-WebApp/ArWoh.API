using ArWoh.API.Entities;
using ArWoh.API.Interface;
using Microsoft.EntityFrameworkCore;

namespace ArWoh.API.Repository;

public class GenericRepository<T> : IGenericRepository<T> where T : BaseEntity
{
    protected readonly ArWohDbContext _context;
    private readonly DbSet<T> _dbSet;

    public GenericRepository(ArWohDbContext context)
    {
        _context = context;
        _dbSet = _context.Set<T>();
    }

    public async Task<T> GetByIdAsync(int id)
    {
        return await _dbSet.FindAsync(id);
    }

    public async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    public async Task AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
    }

    public void Update(T entity)
    {
        _dbSet.Update(entity);
    }

    public void Delete(T entity)
    {
        _dbSet.Remove(entity);
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}
