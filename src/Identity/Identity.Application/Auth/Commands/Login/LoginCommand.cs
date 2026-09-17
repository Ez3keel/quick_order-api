using Identity.Application.Auth.Dtos;
using MediatR;

namespace Identity.Application.Auth.Commands.Login;

public sealed record LoginCommand(string Email, string Password) : IRequest<AuthResultDto>;
