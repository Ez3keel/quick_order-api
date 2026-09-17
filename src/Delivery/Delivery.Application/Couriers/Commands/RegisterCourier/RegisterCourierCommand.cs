using Delivery.Application.Couriers.Dtos;
using MediatR;

namespace Delivery.Application.Couriers.Commands.RegisterCourier;

public sealed record RegisterCourierCommand(string Name) : IRequest<CourierDto>;
