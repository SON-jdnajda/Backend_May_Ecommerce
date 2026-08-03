using Shop.Domain.Common;

namespace Shop.Domain.Entities;

public sealed class User : BaseEntity<Guid>, IAggregateRoot
{
    public string FirstName { get; private set; } = default!;
    public string LastName { get; private set; } = default!;
    public string Email { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public string RefreshToken { get; private set; } = default!;
    public DateTime? RefreshTokenExpiryTime { get; private set; } = default!;

    private User() { }
    public User(string firstName, string lastName, string email, string passwordHash, string refreshToken, DateTime? refreshTokenExpiryTime)
    {

    }
}
