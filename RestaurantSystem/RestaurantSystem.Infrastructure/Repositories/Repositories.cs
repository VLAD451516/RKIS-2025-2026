using Microsoft.EntityFrameworkCore;
using RestaurantSystem.Core.Interfaces;
using RestaurantSystem.Infrastructure.Data;
using System.Linq.Expressions;

namespace RestaurantSystem.Infrastructure.Repositories;

public class Repository<T> : IRepository<T> where T : class
{
    protected readonly ApplicationDbContext _context;

    public Repository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<T?> GetByIdAsync(int id)
    {
        return await _context.Set<T>().FindAsync(id);
    }

    public async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _context.Set<T>().ToListAsync();
    }

    public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
    {
        return await _context.Set<T>().Where(predicate).ToListAsync();
    }

    public async Task AddAsync(T entity)
    {
        await _context.Set<T>().AddAsync(entity);
    }

    public void Update(T entity)
    {
        _context.Set<T>().Update(entity);
    }

    public void Remove(T entity)
    {
        _context.Set<T>().Remove(entity);
    }
}

public class RestaurantRepository : Repository<Core.Entities.Restaurant>, IRestaurantRepository
{
    public RestaurantRepository(ApplicationDbContext context) : base(context) { }

    public async Task<Core.Entities.Restaurant?> GetWithMenuItemsAsync(int id)
    {
        return await _context.Restaurants
            .Include(r => r.MenuItems)
            .FirstOrDefaultAsync(r => r.Id == id);
    }
}

public class OrderRepository : Repository<Core.Entities.Order>, IOrderRepository
{
    public OrderRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IEnumerable<Core.Entities.Order>> GetUserOrdersAsync(string userId)
    {
        return await _context.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.MenuItem)
            .Where(o => o.UserId == userId)
            .ToListAsync();
    }
}

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
        Restaurants = new RestaurantRepository(_context);
        MenuItems = new Repository<Core.Entities.MenuItem>(_context);
        Orders = new OrderRepository(_context);
    }

    public IRestaurantRepository Restaurants { get; private set; }
    public IRepository<Core.Entities.MenuItem> MenuItems { get; private set; }
    public IOrderRepository Orders { get; private set; }

    public async Task<int> CompleteAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
