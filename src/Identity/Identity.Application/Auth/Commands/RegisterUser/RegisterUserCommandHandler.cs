using Identity.Application.Abstractions;
using Identity.Application.Auth.Dtos;
using Identity.Application.Auth.Exceptions;
using Identity.Domain.Users;
using MediatR;

namespace Identity.Application.Auth.Commands.RegisterUser;

public sealed class RegisterUserCommandHandler(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    TokenIssuer tokenIssuer,
    TimeProvider timeProvider) : IRequestHandler<RegisterUserCommand, AuthResultDto>
{
    public async Task<AuthResultDto> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        if (await userRepository.GetByEmailAsync(request.Email, cancellationToken) is not null)
            throw new EmailAlreadyRegisteredException(request.Email);

        var user = User.Register(
            request.Email, passwordHasher.Hash(request.Password), request.Role, timeProvider.GetUtcNow());

        userRepository.Add(user);

        var (result, _) = tokenIssuer.IssueFor(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return result;
    }
}
