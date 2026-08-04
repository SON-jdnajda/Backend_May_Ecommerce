using MediatR;

namespace Shop.Application.Auth.Login;

public record LoginCommand(
    string Email,
    string Password
) : IRequest<AuthResult>;
