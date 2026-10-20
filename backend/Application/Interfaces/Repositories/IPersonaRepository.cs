using ConecsIA.Domain.Entities;

namespace ConecsIA.Application.Interfaces.Repositories;

public interface IPersonaRepository
{
    Task<Persona?> FindByDocumentAsync(int organizationId, int documentTypeId, string normalizedDocument, CancellationToken cancellationToken = default);
    Task AddAsync(Persona persona, CancellationToken cancellationToken = default);
}
