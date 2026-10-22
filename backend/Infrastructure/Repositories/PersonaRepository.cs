using ConecsIA.Application.Interfaces.Repositories;
using ConecsIA.Domain.Entities;
using ConecsIA.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ConecsIA.Infrastructure.Repositories;

public sealed class PersonaRepository : GenericRepository<Persona>, IPersonaRepository
{
    private readonly ConecsiaDbContext _db;

    public PersonaRepository(ConecsiaDbContext db) : base(db) => _db = db;

    public Task<Persona?> FindByDocumentAsync(int organizationId, int documentTypeId, string normalizedDocument, CancellationToken cancellationToken = default) =>
        _db.Personas.AsNoTracking().SingleOrDefaultAsync(x => x.IdOrganizacion == organizationId && x.IdTipoDocumento == documentTypeId && x.NumeroDocumentoNormalizado == normalizedDocument, cancellationToken);

    public Task AddAsync(Persona persona, CancellationToken cancellationToken = default) => _db.Personas.AddAsync(persona, cancellationToken).AsTask();
}
