using MediatR;
using Shop.Domain.Repositories;

namespace Shop.Application.Auth.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResult>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IUnitOfWork _unitOfWork;

    public LoginCommandHandler(IUserRepository userRepository, IPasswordHasher passwordHasher, IJwtTokenGenerator jwtTokenGenerator, IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _unitOfWork = unitOfWork;
    }

    public async Task<AuthResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);

        if(user is null)
            throw new UnauthorizedAccessException("Invalid email or password.");

        var isPasswordValid = _passwordHasher.VerifyPassword(request.Password, user.PasswordHash);
        if(!isPasswordValid)
            throw new UnauthorizedAccessException("Invalid credentials.");

        var accessToken = _jwtTokenGenerator.GenerateAccessToken(user);
        var refreshToken = _jwtTokenGenerator.GenerateRefreshToken();

        user.SetRefreshToken(refreshToken, DateTime.UtcNow.AddDays(7));

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResult(accessToken, $"{user.FirstName} {user.LastName}", user.Email, refreshToken);

    }
}
