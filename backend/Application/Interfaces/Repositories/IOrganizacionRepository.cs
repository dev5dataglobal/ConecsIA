using ConecsIA.Application.Interfaces;
using ConecsIA.Domain.Entities;

namespace ConecsIA.Application.Interfaces.Repositories;

public interface IOrganizacionRepository : IGenericRepository<Organizacion>
{
    Task AddAsync(Organizacion organizacion, CancellationToken cancellationToken = default);
}
