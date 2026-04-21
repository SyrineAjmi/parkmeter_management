using System.Security.Claims;
using parkmeter_management.Application.Interfaces.Repositories;

namespace parkmeter_management.Api.Middleware
{
    public class ActiveUserMiddleware //prevent desactivated user from using token 
    {
        private readonly RequestDelegate _next;

        public ActiveUserMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(
            HttpContext context,
            IServiceProvider serviceProvider)
        {
            // only check authentif requests
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = context.User
                    .FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (userIdClaim != null &&
                    Guid.TryParse(userIdClaim, out var userId))
                {
                    using var scope = serviceProvider
                        .CreateScope();
                    var userRepo = scope.ServiceProvider
                        .GetRequiredService<IUserRepository>();

                    var user = await userRepo.GetByIdAsync(userId);

                    if (user == null || !user.IsActive)
                    {
                        context.Response.StatusCode = 401;
                        await context.Response.WriteAsJsonAsync(new
                        {
                            message = "Account deactivated"
                        });
                        return;
                    }
                }
            }

            await _next(context);
        }
    }
}