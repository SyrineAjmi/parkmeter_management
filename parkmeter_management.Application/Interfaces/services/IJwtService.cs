using parkmeter_management.Domain.Entities;

namespace parkmeter_management.Application.Interfaces.Services
{
    public interface IJwtService
    {
        string GenerateToken(User user);
    }
}