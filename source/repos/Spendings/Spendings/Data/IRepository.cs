using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Spendings.Data
{
    public interface IRepository<T> where T : class, IEntity
    {
        Task Add(T entity);
        IQueryable<T> Query();
        Task Delete(T entity);
        Task Update(T entity);
    }
}
