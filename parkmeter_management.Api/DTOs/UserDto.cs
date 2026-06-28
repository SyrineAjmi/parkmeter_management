using parkmeter_management.Domain.Entities;

namespace parkmeter_management.Api.DTOs
{
    public class UserDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public Guid? SiteId { get; set; }

        public DateTime CreatedAt { get; set; }
    
    }

}