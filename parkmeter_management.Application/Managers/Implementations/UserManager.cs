using MediatR;
using parkmeter_management.Application.Commands.Users;
using parkmeter_management.Application.Interfaces.Repositories;
using parkmeter_management.Application.Interfaces.Services;
using parkmeter_management.Application.Managers.Interfaces;
using parkmeter_management.Application.Queries.Users;
using parkmeter_management.Domain.Entities;
using parkmeter_management.Domain.Enums;

namespace parkmeter_management.Application.Managers.Implementations
{
    public class UserManager : IUserManager
    {
        private readonly IMediator _mediator;
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;


        public UserManager(
            IMediator mediator,
            IUserRepository userRepository,
            IPasswordHasher passwordHasher)
        {
            _mediator = mediator;
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
        }

        public async Task<Guid> CreateUser(
            string name, string email,
            string password, Role role)
            => await _mediator.Send(new CreateUserCommand
            {
                Name = name,
                Email = email,
                Password = password,
                Role = role
            });

        public async Task<bool> UpdateUser(
            Guid id, string name, string email,
            Role role, bool isActive)
            => await _mediator.Send(new UpdateUserCommand
            {
                Id = id,
                Name = name,
                Email = email,
                Role = role,
                IsActive = isActive
            });

        public async Task<bool> DeleteUser(Guid id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null) return false;
            await _userRepository.DeleteAsync(id);
            return true;
        }

        public async Task<List<User>> GetAllUsers()
            => await _mediator.Send(new GetAllUsersQuery());

        public async Task<User?> GetById(Guid id)
            => await _userRepository.GetByIdAsync(id);

        public async Task<bool> ChangePassword(
            Guid userId,
            string currentPassword,
            string newPassword)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return false;

            // verify current password
            var isValid = _passwordHasher
                .Verify(currentPassword, user.PasswordHash);
            if (!isValid) return false;

            // hash new password
            user.UpdatePassword(_passwordHasher.Hash(newPassword));
            await _userRepository.UpdateAsync(user);
            return true;
        }

    }

   
}