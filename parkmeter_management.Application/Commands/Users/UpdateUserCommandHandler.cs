using MediatR;
using parkmeter_management.Application.Interfaces.Repositories;

namespace parkmeter_management.Application.Commands.Users
{
    public class UpdateUserCommandHandler
        : IRequestHandler<UpdateUserCommand, bool>
    {
        private readonly IUserRepository _userRepository;

        public UpdateUserCommandHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<bool> Handle(
            UpdateUserCommand command, CancellationToken ct)
        {
            var user = await _userRepository.GetByIdAsync(command.Id);
            if (user == null) return false;

            user.Update(command.Name, command.Email, command.Role, command.IsActive);

            await _userRepository.UpdateAsync(user);
            return true;
        }
    }
}