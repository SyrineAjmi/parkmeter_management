using MediatR;
using parkmeter_management.Application.Interfaces.Repositories;
using parkmeter_management.Application.Interfaces.Services;
using parkmeter_management.Domain.Entities;
using parkmeter_management.Domain.Enums;

namespace parkmeter_management.Application.Commands.Users
{
    public class CreateUserCommandHandler
        : IRequestHandler<CreateUserCommand, Guid>
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;

        public CreateUserCommandHandler(IUserRepository userRepository, IPasswordHasher passwordHasher)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
        }

        public async Task<Guid> Handle(
            CreateUserCommand command, CancellationToken ct)
        {
            // Check email unique
            var existing = await _userRepository
                .GetByEmailAsync(command.Email);
            if (existing != null)
                throw new Exception(
                    $"Email '{command.Email}' already exists.");

            var passwordHash = _passwordHasher.Hash(command.Password);

            var user = new User
            (
                command.Name,
                command.Email,
                passwordHash,
                command.Role
            );

            await _userRepository.AddAsync(user);
            return user.Id;
        }
    }
}