using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Spendings.Data
{
    public class EFCoreRepository<TEntity> : IRepository<TEntity>
        where TEntity : class, IEntity
    {
        private readonly SpendingsDbContext _context;
        public EFCoreRepository(SpendingsDbContext context)
        {
            _context = context;
        }
        public async Task Add(TEntity entity)
        {
            _context.Set<TEntity>().Add(entity);
            await _context.SaveChangesAsync();
        }

        public async Task Delete(TEntity entity)
        {
            var toDelete = await _context.Set<TEntity>().FindAsync(entity.Id);
            if (toDelete != null)
            {
                _context.Set<TEntity>().Remove(toDelete);
                await _context.SaveChangesAsync();
            }
        }

        public IQueryable<TEntity> Query()
        {
            return _context.Set<TEntity>();        
        }

        public async Task Update(TEntity entity)
        {
            _context.Entry(entity).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }
    }
}
