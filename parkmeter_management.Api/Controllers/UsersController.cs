using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using parkmeter_management.Api.DTOs;
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



        [Authorize(Roles = "GESTIONNAIRE_SYSTEME")]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var users = await _userManager.GetAllUsers();
            var dtos = users.Select(u => new UserDto
            {
                Id = u.Id,
                Name = u.Name,
                Email = u.Email,
                Role = u.Role.ToString(),
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt
            });
            return Ok(dtos);
        }



        [Authorize(Roles = "GESTIONNAIRE_SYSTEME")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var user = await _userManager.GetById(id);
            if (user == null) return NotFound();

            return Ok(new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Role = user.Role.ToString(),
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt
            });
        }



        [Authorize(Roles = "GESTIONNAIRE_SYSTEME")]
        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateUserRequest request)
        {
            try
            {
                Enum.TryParse<Role>(request.Role, true, out var role);
                var id = await _userManager.CreateUser(
                    request.Name,
                    request.Email,
                    request.Password,
                    role);
                return Ok(new { Id = id, Message = "User created successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }



        [Authorize(Roles = "GESTIONNAIRE_SYSTEME")]
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

            var result = await _userManager
                .ChangePassword(
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



        [Authorize(Roles = "GESTIONNAIRE_SYSTEME")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(
            Guid id, [FromBody] UpdateUserRequest request)
        {
            Enum.TryParse<Role>(request.Role, true, out var role);
            var result = await _userManager.UpdateUser(
                id, request.Name, request.Email, role, request.IsActive);
            if (!result) return NotFound();
            return Ok("User updated successfully");
        }



        [Authorize(Roles = "GESTIONNAIRE_SYSTEME")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _userManager.DeleteUser(id);
            if (!result) return NotFound();
            return Ok("User deleted successfully");
        }
    }

    public class CreateUserRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }
    public class UpdateUserRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
    public class ChangePasswordRequest
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}