using ConecsIA.Application.Interfaces.Repositories;
using ConecsIA.Domain.Entities;
using ConecsIA.Infrastructure.Data;

namespace ConecsIA.Infrastructure.Repositories;

public sealed class OrganizacionRepository : GenericRepository<Organizacion>, IOrganizacionRepository
{
    private readonly ConecsiaDbContext _db;

    public OrganizacionRepository(ConecsiaDbContext db) : base(db) => _db = db;

    public Task AddAsync(Organizacion organizacion, CancellationToken cancellationToken = default) => _db.Organizaciones.AddAsync(organizacion, cancellationToken).AsTask();
}
