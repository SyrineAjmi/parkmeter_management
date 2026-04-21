using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using parkmeter_management.Application.Managers.Interfaces;

namespace parkmeter_management.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthManager _authManager;

        public AuthController(IAuthManager authManager)
        {
            _authManager = authManager;
        }


        //----------------------------------------------------------

        [HttpPost("login")]
        public async Task<IActionResult> Login(
            [FromBody] LoginRequest request)
        {
            var token = await _authManager
                .Login(request.Email, request.Password);

            if (token == null)
                return Unauthorized(
                    new { message = "Email ou mot de passe incorrect" });

            return Ok(new
            {
                Token = token,
                Message = "Connexion réussie"
            });
        }
    }

}