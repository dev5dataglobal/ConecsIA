using ConecsIA.Domain.Entities;

namespace ConecsIA.Application.Interfaces.Repositories;

public interface IOrganizacionRepository
{
    Task<Organizacion?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task AddAsync(Organizacion organizacion, CancellationToken cancellationToken = default);
}
