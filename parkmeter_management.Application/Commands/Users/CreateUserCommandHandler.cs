using MediatR;
using parkmeter_management.Application.Interfaces.Repositories;
using parkmeter_management.Domain.Entities;

namespace parkmeter_management.Application.Commands.Users
{
    public class CreateUserCommandHandler
        : IRequestHandler<CreateUserCommand, Guid>
    {
        private readonly IUserRepository _userRepository;

        public CreateUserCommandHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
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

            // Simple hash for now — BCrypt when JWT added
            var passwordHash = Convert.ToBase64String(
                System.Text.Encoding.UTF8.GetBytes(command.Password));

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