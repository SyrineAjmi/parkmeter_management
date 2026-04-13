using MediatR;
using parkmeter_management.Application.Interfaces.Repositories;
using parkmeter_management.Application.Interfaces.Services;
using parkmeter_management.Domain.Entities;

namespace parkmeter_management.Application.Commands.Users
{
    public class CreateUserCommandHandler
        : IRequestHandler<CreateUserCommand, Guid>
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;

        public CreateUserCommandHandler(IUserRepository userRepository,IPasswordHasher passwordHasher)
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
            {
                Id = Guid.NewGuid(),
                Name = command.Name,
                Email = command.Email,
                PasswordHash = passwordHash,
                Role = command.Role,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _userRepository.AddAsync(user);
            return user.Id;
        }
    }
}