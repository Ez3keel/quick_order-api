using MediatR;

namespace Identity.Application.Auth.Commands.RevokeToken;

public sealed record RevokeTokenCommand(string RawRefreshToken) : IRequest;
