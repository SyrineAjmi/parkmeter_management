using parkmeter_management.Application.Interfaces.Repositories;
using parkmeter_management.Application.Interfaces.Services;
using parkmeter_management.Application.Managers.Interfaces;
using parkmeter_management.Domain.Entities;

namespace parkmeter_management.Application.Managers.Implementations
{
    public class AuthManager : IAuthManager
    {
        private readonly IUserRepository _userRepository;
        private readonly IJwtService _jwtService;
        private readonly IPasswordHasher _passwordHasher;


        public AuthManager(
            IUserRepository userRepository,
            IJwtService jwtService,
            IPasswordHasher passwordHasher)
        {
            _userRepository = userRepository;
            _jwtService = jwtService;
            _passwordHasher = passwordHasher;
        }

        public async Task<string?> Login(
            string email, string password)
        {
            var user = await _userRepository
                .GetByEmailAsync(email);
            if (user == null || !user.IsActive)
                return null;

            // verify password
        var isValid = _passwordHasher
            .Verify(password, user.PasswordHash);
            if (!isValid) return null;

            return _jwtService.GenerateToken(user);
        }
    }
}