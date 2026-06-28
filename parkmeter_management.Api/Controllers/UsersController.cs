using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using parkmeter_management.Api.Mappers;
using parkmeter_management.Api.Requests.User;
using parkmeter_management.Application.Managers.Interfaces;
using parkmeter_management.Domain.Enums;

namespace parkmeter_management.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserManager _userManager;

        public UsersController(IUserManager userManager)
        {
            _userManager = userManager;
        }

        //----------------------------------------------------------

        [Authorize(Roles = "GESTIONNAIRE_SYSTEME")]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var users = await _userManager.GetAllUsers();
            return Ok(users.ToDtos());

        }

        //----------------------------------------------------------

        [Authorize(Roles = "GESTIONNAIRE_SYSTEME")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var user = await _userManager.GetById(id);
            if (user == null) return NotFound();

            return Ok(user.ToDto());

        }

        //----------------------------------------------------------

        [Authorize(Roles = "GESTIONNAIRE_SYSTEME")]
        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateUserRequest request)
        {
            if (!Enum.TryParse<Role>(
                request.Role, true, out var role))
                return BadRequest("Invalid role value");
            try
            {
                var id = await _userManager.CreateUser(
                    request.Name,
                    request.Email,
                    request.Password,
                    role,
                    request.SiteId);
                return Ok(new
                {
                    Id = id,
                    Message = "User created successfully"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        //----------------------------------------------------------

        [Authorize]
        [HttpPut("{id}/change-password")]
        public async Task<IActionResult> ChangePassword(
            Guid id,
            [FromBody] ChangePasswordRequest request)
        {
            if (request.NewPassword != request.ConfirmPassword)
                return BadRequest(new
                {
                    message = "Les mots de passe ne correspondent pas"
                });

            var result = await _userManager.ChangePassword(
                id,
                request.CurrentPassword,
                request.NewPassword);

            if (!result)
                return BadRequest(new
                {
                    message = "Mot de passe actuel incorrect"
                });

            return Ok(new
            {
                message = "Mot de passe modifié avec succès"
            });
        }

        //----------------------------------------------------------

        [Authorize(Roles = "SUPERVISEUR")]
        [HttpGet("operators")]
        public async Task<IActionResult> GetOperators()
        {
            var users = await _userManager.GetAllUsers();
            var operators = users.Where(u => u.Role == Role.OPERATEUR && u.IsActive);
            return Ok(operators.ToDtos());
        }

        //----------------------------------------------------------

        [Authorize(Roles = "GESTIONNAIRE_SYSTEME")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(
            Guid id, [FromBody] UpdateUserRequest request)
        {
            if (!Enum.TryParse<Role>(
                request.Role, true, out var role))
                return BadRequest("Invalid role value");

            var result = await _userManager.UpdateUser(
                id,
                request.Name,
                request.Email,
                role,
                request.IsActive);
            if (!result) return NotFound();
            return Ok("User updated successfully");
        }

        //----------------------------------------------------------

        [Authorize(Roles = "GESTIONNAIRE_SYSTEME")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _userManager.DeleteUser(id);
            if (!result) return NotFound();
            return Ok("User deleted successfully");
        }
    }

}