using MediatR;
using Ordering.Application.Orders.Dtos;

namespace Ordering.Application.Orders.Queries.GetOrderById;

public sealed record GetOrderByIdQuery(Guid OrderId) : IRequest<OrderDto?>;
