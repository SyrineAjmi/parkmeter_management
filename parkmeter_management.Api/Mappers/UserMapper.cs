using parkmeter_management.Api.DTOs;
using parkmeter_management.Domain.Entities;

namespace parkmeter_management.Api.Mappers
{
    public static class UserMapper
    {
        public static UserDto ToDto(this User user)
            => new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Role = user.Role.ToString(),
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt
            };

        public static IEnumerable<UserDto> ToDtos(
            this IEnumerable<User> users)
            => users.Select(u => u.ToDto());
    }
}