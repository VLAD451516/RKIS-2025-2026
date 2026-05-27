using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace RestaurantSystem.API.Repositories
{
    public interface IRepository<T> where T : class
    {
        Task<T?> GetByIdAsync(int id);
        Task<IEnumerable<T>> GetAllAsync();
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
        Task AddAsync(T entity);
        void Update(T entity);
        void Remove(T entity);
        Task<bool> SaveChangesAsync();
        IQueryable<T> GetQueryable();
    }

    public class GenericRepository<T> : IRepository<T> where T : class
    {
        protected readonly Data.ApplicationDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public GenericRepository(Data.ApplicationDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public async Task<T?> GetByIdAsync(int id) => await _dbSet.FindAsync(id);

        public async Task<IEnumerable<T>> GetAllAsync() => await _dbSet.ToListAsync();

        public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate) =>
            await _dbSet.Where(predicate).ToListAsync();

        public async Task AddAsync(T entity) => await _dbSet.AddAsync(entity);

        public void Update(T entity) => _dbSet.Update(entity);

        public void Remove(T entity) => _dbSet.Remove(entity);

        public async Task<bool> SaveChangesAsync() => await _context.SaveChangesAsync() > 0;

        public IQueryable<T> GetQueryable() => _dbSet.AsQueryable();
    }
}
