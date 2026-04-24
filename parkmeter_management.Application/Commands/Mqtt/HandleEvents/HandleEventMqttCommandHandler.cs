using MediatR;
using parkmeter_management.Application.Commands.Simulator;
using parkmeter_management.Application.Commands.Simulator.HandleCashRemoved;
using parkmeter_management.Application.Commands.Simulator.HandleDoorClosed;
using parkmeter_management.Application.Commands.Simulator.HandlePaperRefilled;
using parkmeter_management.Application.Interfaces.Repositories;
using parkmeter_management.Application.Interfaces.Services;
using parkmeter_management.Domain.Entities;
using parkmeter_management.Domain.Enums;
using System.Text.Json;

namespace parkmeter_management.Application.Commands.Mqtt.HandleEvents
{
    public class HandleEventMqttCommandHandler
        : IRequestHandler<HandleEventMqttCommand, bool>
    {
        private readonly IMediator _mediator;
        private readonly IParkMeterEventRepository _eventRepository;
        private readonly INotificationService _notificationService;

        public HandleEventMqttCommandHandler(
            IMediator mediator,
            IParkMeterEventRepository eventRepository,
            INotificationService notificationService)
        {
            _mediator = mediator;
            _eventRepository = eventRepository;
            _notificationService = notificationService;
        }

        public async Task<bool> Handle(
            HandleEventMqttCommand command,
            CancellationToken ct)
        {
            var raw = JsonSerializer
                .Deserialize<JsonElement>(command.Payload);

            var parkmeterId = Guid.Parse(
                raw.GetProperty("ParkmeterId").GetString()!);
            var eventTypeVal = raw.GetProperty("EventType").GetInt32();
            var eventType = (ParkMeterEventType)eventTypeVal;

            // persist event record
            await _eventRepository.AddAsync(new ParkMeterEvent
            {
                Id = Guid.NewGuid(),
                ParkmeterId = parkmeterId,
                EventType = eventType,
                CreatedAt = DateTime.UtcNow
            });

            // notify frontend
            await _notificationService.NotifyNewEvent(
                parkmeterId,
                eventType.ToString());

            Console.WriteLine(
                $"[EVENT] {eventType} → parkmeter {parkmeterId}");

            // route to business logic handler where needed
            switch (eventType)
            {
                case ParkMeterEventType.CASH_COLLECTED:
                    await _mediator.Send(
                        new HandleCashRemovedCommand
                        { ParkmeterId = parkmeterId }, ct);
                    break;

                case ParkMeterEventType.PAPER_REFILLED:
                    await _mediator.Send(
                        new HandlePaperRefilledCommand
                        { ParkmeterId = parkmeterId }, ct);
                    break;

                case ParkMeterEventType.DOOR_CLOSED:
                    await _mediator.Send(
                        new HandleDoorClosedCommand
                        { ParkmeterId = parkmeterId }, ct);
                    break;

                // MACHINE_ONLINE handled by /online topic
                // MACHINE_RECONNECTED handled by /reconnected topic
                // MACHINE_OFFLINE and TRANSACTION_DONE — persisted only
                default:
                    Console.WriteLine(
                        $"[EVENT] {eventType} persisted");
                    break;
            }

            return true;
        }
    }
}