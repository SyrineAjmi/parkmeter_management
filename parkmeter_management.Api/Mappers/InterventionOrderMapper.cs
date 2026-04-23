using parkmeter_management.Api.DTOs;
using parkmeter_management.Domain.Entities;

namespace parkmeter_management.Api.Mappers
{
    public static class InterventionOrderMapper
    {
        public static InterventionOrderDto ToDto(
            this InterventionOrder order)
            => new InterventionOrderDto
            {
                Id = order.Id,
                ParkmeterId = order.ParkmeterId,
                OperatorId = order.OperatorId,
                OperatorName = order.Operator?.Name
                    ?? string.Empty,
                Type = order.Type.ToString(), 
                Status = order.Status.ToString(), 
                CreatedAt = order.CreatedAt,
                CompletedAt = order.CompletedAt
            };

        public static IEnumerable<InterventionOrderDto> ToDtos(
            this IEnumerable<InterventionOrder> orders)
            => orders.Select(o => o.ToDto());
    }
}