using MediatR;
using parkmeter_management.Application.Interfaces.Repositories;

namespace parkmeter_management.Application.Commands.Parkmeters
{
    public class AssignSiteCommandHandler
        : IRequestHandler<AssignSiteCommand, bool>
    {
        private readonly IParkmeterRepository _parkmeterRepository;

        public AssignSiteCommandHandler(
            IParkmeterRepository parkmeterRepository)
        {
            _parkmeterRepository = parkmeterRepository;
        }

        public async Task<bool> Handle(
            AssignSiteCommand command, CancellationToken ct)
        {
            var parkmeter = await _parkmeterRepository
                .GetByIdAsync(command.ParkmeterId);
            if (parkmeter == null) return false;

            parkmeter.UpdateSite(command.SiteId);
            await _parkmeterRepository.UpdateAsync(parkmeter);
            return true;
        }
    }
}