using FluentValidation;
using parkmeter_management.Api.Controllers;

namespace parkmeter_management.Api.Validators
{
    public class ChangePasswordRequestValidator
        : AbstractValidator<ChangePasswordRequest>
    {
        public ChangePasswordRequestValidator()
        {
            RuleFor(x => x.CurrentPassword)
                .NotEmpty()
                .WithMessage("Mot de passe actuel requis");

            RuleFor(x => x.NewPassword)
                .NotEmpty()
                .WithMessage("Nouveau mot de passe requis")
                .MinimumLength(6)
                .WithMessage("Minimum 6 caractères")
                .NotEqual(x => x.CurrentPassword)
                .WithMessage(
                    "Nouveau mot de passe doit être différent");

            RuleFor(x => x.ConfirmPassword)
                .Equal(x => x.NewPassword)
                .WithMessage("Les mots de passe ne correspondent pas");
        }
    }
}