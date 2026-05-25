using System.Linq.Expressions;

namespace RestaurantSystem.Core.Interfaces;

public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
    Task AddAsync(T entity);
    void Update(T entity);
    void Remove(T entity);
}

public interface IRestaurantRepository : IRepository<Entities.Restaurant>
{
    Task<Entities.Restaurant?> GetWithMenuItemsAsync(int id);
}

public interface IOrderRepository : IRepository<Entities.Order>
{
    Task<IEnumerable<Entities.Order>> GetUserOrdersAsync(string userId);
}

public interface IUnitOfWork : IDisposable
{
    IRestaurantRepository Restaurants { get; }
    IRepository<Entities.MenuItem> MenuItems { get; }
    IOrderRepository Orders { get; }
    Task<int> CompleteAsync();
}
