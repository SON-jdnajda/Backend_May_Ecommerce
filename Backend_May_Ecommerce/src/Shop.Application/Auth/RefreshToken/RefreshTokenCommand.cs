using MediatR;

namespace Shop.Application.Auth.RefreshToken;

public record RefreshTokenCommand(
    Guid UserId,
    string refreshToken
) : IRequest<AuthResult>;
