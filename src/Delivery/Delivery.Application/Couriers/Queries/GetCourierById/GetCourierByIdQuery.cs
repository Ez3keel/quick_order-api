using Delivery.Application.Couriers.Dtos;
using MediatR;

namespace Delivery.Application.Couriers.Queries.GetCourierById;

public sealed record GetCourierByIdQuery(Guid CourierId) : IRequest<CourierDto?>;
