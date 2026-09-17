using MediatR;

namespace Delivery.Application.Assignments.Commands.AssignCourier;

/// <summary>
/// Not exposed over HTTP — only the queue consumer (Fase 4) calls this, in response
/// to an OrderReadyForAssignmentIntegrationEvent. Relies on the consumer processing
/// one message at a time (see docs, item 7 and 23): the courier picked from Redis and
/// reserved here is never contended by a second concurrent call from this service.
/// </summary>
public sealed record AssignCourierCommand(Guid OrderId, Guid RestaurantId) : IRequest<Guid>;
