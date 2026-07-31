namespace Shop.Domain.Exceptions;

/// <summary>
/// Raised when a write lost an optimistic-concurrency race. Infrastructure
/// translates EF Core's DbUpdateConcurrencyException into this so the
/// Application layer never has to reference a persistence library.
/// </summary>
public class ConcurrencyConflictException : DomainException
{
    public ConcurrencyConflictException(string message)
        : base(message) { }

    public ConcurrencyConflictException(string message, Exception innerException)
        : base(message, innerException) { }
}
