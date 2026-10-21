using ConecsIA.Application.Interfaces;
using ConecsIA.Domain.Entities;

namespace ConecsIA.Application.Interfaces.Repositories;

public interface IOutboxRepository : IGenericRepository<Outbox>
{
}
