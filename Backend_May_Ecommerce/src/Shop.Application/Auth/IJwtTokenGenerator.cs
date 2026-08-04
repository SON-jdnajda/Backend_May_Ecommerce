using Shop.Domain.Entities;

namespace Shop.Application.Auth;

public interface IJwtTokenGenerator
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
}
