using Delivery.Application.Abstractions;
using Delivery.Application.Couriers.Dtos;
using Delivery.Domain.Couriers;
using MediatR;

namespace Delivery.Application.Couriers.Queries.GetCourierById;

public sealed class GetCourierByIdQueryHandler(ICourierRepository repository)
    : IRequestHandler<GetCourierByIdQuery, CourierDto?>
{
    public async Task<CourierDto?> Handle(GetCourierByIdQuery request, CancellationToken cancellationToken)
    {
        var courier = await repository.GetByIdAsync(CourierId.From(request.CourierId), cancellationToken);
        return courier?.ToDto();
    }
}
