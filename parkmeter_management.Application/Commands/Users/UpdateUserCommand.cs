using MediatR;
using parkmeter_management.Domain.Enums;

namespace parkmeter_management.Application.Commands.Users
{
    public class UpdateUserCommand : IRequest<bool>
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public Role Role { get; set; }
        public bool IsActive { get; set; }
    }
}