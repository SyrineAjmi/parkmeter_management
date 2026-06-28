using parkmeter_management.Domain.Entities;
using parkmeter_management.Domain.Enums;

namespace parkmeter_management.Application.Managers.Interfaces
{
    public interface IUserManager
    {
        Task<Guid> CreateUser(string name, string email,
            string password, Role role, Guid? siteId);
        Task<bool> UpdateUser(Guid id, string name,
            string email, Role role, bool isActive);
        Task<bool> ChangePassword(
            Guid userId,
            string currentPassword,
            string newPassword);

        Task<bool> DeleteUser(Guid id);
        Task<List<User>> GetAllUsers();
        Task<User?> GetById(Guid id);
    }
}