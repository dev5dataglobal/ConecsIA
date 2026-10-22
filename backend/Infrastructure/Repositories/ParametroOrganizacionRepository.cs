using ConecsIA.Application.Interfaces.Repositories;
using ConecsIA.Domain.Entities;
using ConecsIA.Infrastructure.Data;

namespace ConecsIA.Infrastructure.Repositories;

public sealed class ParametroOrganizacionRepository(ConecsiaDbContext db) : GenericRepository<ParametroOrganizacion>(db), IParametroOrganizacionRepository
{
}
