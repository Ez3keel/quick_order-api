using Identity.Application.Auth.Dtos;
using Identity.Domain.Users;
using MediatR;

namespace Identity.Application.Auth.Commands.RegisterUser;

public sealed record RegisterUserCommand(string Email, string Password, UserRole Role) : IRequest<AuthResultDto>;
