namespace parkmeter_management.Application.Managers.Interfaces
{
    public interface IAuthManager
    {
        Task<string?> Login(string email, string password);
    }
}