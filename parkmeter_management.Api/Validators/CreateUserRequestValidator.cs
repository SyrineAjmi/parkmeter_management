using FluentValidation;
using parkmeter_management.Api.Requests.User;

namespace parkmeter_management.Api.Validators
{
    public class CreateUserRequestValidator
        : AbstractValidator<CreateUserRequest>
    {
        public CreateUserRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Name is required")
                .MaximumLength(100)
                .WithMessage("Name cannot exceed 100 characters");

            RuleFor(x => x.Email)
                .NotEmpty()
                .WithMessage("Email is required")
                .EmailAddress()
                .WithMessage("Invalid email format");

            RuleFor(x => x.Password)
                .NotEmpty()
                .WithMessage("Password is required")
                .MinimumLength(8)
                .WithMessage("Password must be at least 8 characters");

            RuleFor(x => x.Role)
                .NotEmpty()
                .WithMessage("Role is required")
                .Must(r => new[]
                {
                    "AGENT_INSTALLATION", 
                    "GESTIONNAIRE_SYSTEME",
                    "SUPERVISEUR",
                    "RESPONSABLE_FINANCIER",
                    "OPERATEUR"
                }.Contains(r))
                .WithMessage("Invalid role value"); 
        }
    }
}