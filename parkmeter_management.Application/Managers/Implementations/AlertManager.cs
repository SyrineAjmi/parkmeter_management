using MediatR;
using parkmeter_management.Application.Commands.Alerts;
using parkmeter_management.Application.Managers.Interfaces;
using parkmeter_management.Application.Queries.Alerts;
using parkmeter_management.Domain.Entities;

namespace parkmeter_management.Application.Managers.Implementations
{
    public class AlertManager : IAlertManager
    {
        private readonly IMediator _mediator;

        public AlertManager(IMediator mediator)
        {
            _mediator = mediator;
        }

        public async Task<Guid> RecordAlert(CreateAlertCommand command)
            => await _mediator.Send(command);

        public async Task<bool> ResolveAlert(Guid alertId)
            => await _mediator.Send(
                new ResolveAlertCommand { AlertId = alertId });

        public async Task<List<Alert>> GetAllAlerts()
            => await _mediator.Send(new GetAllAlertsQuery());

        public async Task<Alert?> GetById(Guid id)
            => await _mediator.Send(new GetAlertByIdQuery { Id = id });

        public async Task<List<Alert>> FilterByPriority(string priority)
            => await _mediator.Send(
                new GetAlertsByPriorityQuery { Priority = priority });
    }
}