using System.Linq.Expressions;
using ConecsIA.Application.Interfaces;
using ConecsIA.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ConecsIA.Infrastructure.Repositories;

public class GenericRepository<T>(ConecsiaDbContext context) : IGenericRepository<T> where T : class
{
    protected readonly ConecsiaDbContext Context = context;

    public virtual void Add(T entity) => Context.Set<T>().Add(entity);
    public virtual void AddRange(IEnumerable<T> entities) => Context.Set<T>().AddRange(entities);
    public virtual IEnumerable<T> Find(Expression<Func<T, bool>> expression) => Context.Set<T>().Where(expression);
    public virtual async Task<IEnumerable<T>> GetAllAsync() => await Context.Set<T>().ToListAsync();

    public virtual async Task<(int totalRegisters, IEnumerable<T> registers)> GetAllAsync(int pageIndex, int pageSize, string? search = null)
    {
        var query = Context.Set<T>().AsQueryable();
        var totalRegisters = await query.CountAsync();
        var registers = await query.Skip(Math.Max(0, pageIndex - 1) * pageSize).Take(pageSize).ToListAsync();
        return (totalRegisters, registers);
    }

    public virtual async Task<T> GetByIdAsync(int id) =>
        await Context.Set<T>().FindAsync(id) ?? throw new KeyNotFoundException($"Entity with id {id} was not found.");

    public virtual void Remove(T entity) => Context.Set<T>().Remove(entity);
    public virtual void RemoveRange(IEnumerable<T> entities) => Context.Set<T>().RemoveRange(entities);
    public virtual void Update(T entity) => Context.Set<T>().Update(entity);
}
