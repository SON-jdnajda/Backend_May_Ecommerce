using Shop.Domain.Common;

namespace Shop.Domain.Repositories;

public interface IGenericRepository<T> where T : BaseEntity<Guid>, IAggregateRoot
{
    /// <summary>Tracked - use from commands that mutate the entity.</summary>
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Untracked - use from queries that only project to a DTO.</summary>
    Task<T?> GetByIdAsNoTrackingAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(T entity, CancellationToken cancellationToken = default);
    void Update(T entity);
    void Delete(T entity);
}
