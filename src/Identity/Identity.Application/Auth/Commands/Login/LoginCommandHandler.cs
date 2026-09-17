using Identity.Application.Abstractions;
using Identity.Application.Auth.Dtos;
using Identity.Application.Auth.Exceptions;
using MediatR;

namespace Identity.Application.Auth.Commands.Login;

public sealed class LoginCommandHandler(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    TokenIssuer tokenIssuer) : IRequestHandler<LoginCommand, AuthResultDto>
{
    public async Task<AuthResultDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByEmailAsync(request.Email, cancellationToken);

        if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new InvalidCredentialsException();

        var (result, _) = tokenIssuer.IssueFor(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return result;
    }
}
