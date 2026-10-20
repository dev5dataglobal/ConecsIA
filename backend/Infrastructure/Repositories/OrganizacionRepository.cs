using ConecsIA.Application.Interfaces.Repositories;
using ConecsIA.Domain.Entities;
using ConecsIA.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ConecsIA.Infrastructure.Repositories;

public sealed class OrganizacionRepository(ConecsiaDbContext db) : IOrganizacionRepository
{
    public Task<Organizacion?> GetByIdAsync(int id, CancellationToken cancellationToken = default) => db.Organizaciones.AsNoTracking().SingleOrDefaultAsync(x => x.IdOrganizacion == id, cancellationToken);
    public Task AddAsync(Organizacion organizacion, CancellationToken cancellationToken = default) => db.Organizaciones.AddAsync(organizacion, cancellationToken).AsTask();
}
