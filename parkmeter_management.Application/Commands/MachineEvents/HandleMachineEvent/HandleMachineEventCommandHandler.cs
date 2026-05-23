using MediatR;
using parkmeter_management.Application.Commands.MachineEvents.HandleCashRemoved;
using parkmeter_management.Application.Commands.MachineEvents.HandleDoorClosed;
using parkmeter_management.Application.Commands.MachineEvents.HandleHardwareFixed;
using parkmeter_management.Application.Commands.MachineEvents.HandlePaperRefilled;
using parkmeter_management.Application.Commands.MachineEvents.HandleParkmeterOffline;
using parkmeter_management.Application.Commands.MachineEvents.HandleParkmeterOnline;
using parkmeter_management.Application.Commands.MachineEvents.HandleReconnected;
using parkmeter_management.Application.Interfaces.Repositories;
using parkmeter_management.Domain.Entities;
using parkmeter_management.Domain.Enums;

namespace parkmeter_management.Application.Commands.MachineEvents.HandleMachineEvent
{
    public class HandleMachineEventCommandHandler
        : IRequestHandler<HandleMachineEventCommand, bool>
    {
        private readonly IMachineEventRepository _machineEventRepository;
        private readonly IMediator _mediator;

        public HandleMachineEventCommandHandler(
            IMachineEventRepository machineEventRepository,
            IMediator mediator)
        {
            _machineEventRepository = machineEventRepository;
            _mediator = mediator;
        }

        public async Task<bool> Handle(
            HandleMachineEventCommand command,
            CancellationToken ct)
        {
            var combinedEvent = (MachineEventType)command.EventType;

            foreach (MachineEventType type in
                Enum.GetValues<MachineEventType>())
            {
                if (type == MachineEventType.NONE) continue;
                if (!combinedEvent.HasFlag(type)) continue;

                // persist audit log
                await _machineEventRepository.AddAsync(new MachineEvent
                {
                    Id = Guid.NewGuid(),
                    ParkmeterId = command.ParkmeterId,
                    EventType = type,
                    Description = type.ToString(),
                    CreatedAt = command.CreatedAt
                });

                // dispatch to individual handler
                IRequest<bool>? subCommand = type switch
                {
                    MachineEventType.MACHINE_ONLINE =>
                        new HandleParkmeterOnlineCommand
                        { ParkmeterId = command.ParkmeterId },

                    MachineEventType.MACHINE_RECONNECTED =>
                        new HandleReconnectedCommand
                        { ParkmeterId = command.ParkmeterId },

                    MachineEventType.CASH_COLLECTED =>
                        new HandleCashRemovedCommand
                        { ParkmeterId = command.ParkmeterId },

                    MachineEventType.PAPER_REFILLED =>
                        new HandlePaperRefilledCommand
                        { ParkmeterId = command.ParkmeterId },

                    MachineEventType.DOOR_CLOSED =>
                        new HandleDoorClosedCommand
                        { ParkmeterId = command.ParkmeterId },

                    MachineEventType.HARDWARE_FIXED =>
                        new HandleHardwareFixedCommand
                        {
                            ParkmeterId = command.ParkmeterId,
                            CreatedAt = command.CreatedAt
                        },

                    _ => null
                };

                if (subCommand != null)
                    await _mediator.Send(subCommand, ct);
            }

            return true;
        }
    }
}