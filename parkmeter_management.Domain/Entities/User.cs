using parkmeter_management.Domain.Enums;

namespace parkmeter_management.Domain.Entities
{
    public class User
    {

        private User() { }

        public User(string name, string email, string passwordHash, Role role)
        {
            Id = Guid.NewGuid();
            Name = name;
            Email = email;
            PasswordHash = passwordHash;
            Role = role;
            IsActive = true;
            CreatedAt = DateTime.UtcNow;
        }

        public Guid Id { get; private set; } = Guid.NewGuid();
        public string Name { get; private set; } = string.Empty;
        public string Email { get; private set; } = string.Empty;
        public string PasswordHash { get; private set; } = string.Empty;
        public Role Role { get; private set; }
        public bool IsActive { get; private set; } = true;
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

        public void Deactivate() => IsActive = false;
        public void Activate() => IsActive = true;
        public void UpdatePassword(string hash) => PasswordHash = hash;
        public void Update(string name, string email, Role role, bool isActive)
        {
            Name = name;
            Email = email;
            Role = role;
            IsActive = isActive;
        }

    }
}
