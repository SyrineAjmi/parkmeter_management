using FluentValidation;
using parkmeter_management.Api.Controllers;

namespace parkmeter_management.Api.Validators
{
    public class UpdateUserRequestValidator
        : AbstractValidator<UpdateUserRequest>
    {
        public UpdateUserRequestValidator()
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

            RuleFor(x => x.Role)
                .NotEmpty()
                .WithMessage("Role is required")
                .Must(r => new[]
                {
                    "AGENT_INSTALLATION",
                    "GESTIONNAIRE_SYSTEME",
                    "SUPERVISEUR",
                    "RESPONSABLE_FINANCIER"
                }.Contains(r))
                .WithMessage("Invalid role value");
        }
    }
}