using ConecsIA.Application.Interfaces.Repositories;
using ConecsIA.Domain.Entities;
using ConecsIA.Infrastructure.Data;

namespace ConecsIA.Infrastructure.Repositories;

public sealed class SedeRepository(ConecsiaDbContext db) : GenericRepository<Sede>(db), ISedeRepository
{
}
