using FluentValidation;
using parkmeter_management.Api.Requests.Parkmeter;

namespace parkmeter_management.Api.Validators
{
    public class ChangeStatusRequestValidator
    : AbstractValidator<ChangeStatusRequest>
    {
        public ChangeStatusRequestValidator()
        {
            RuleFor(x => x.Status)
                .NotEmpty()
                .WithMessage("Status is required")
                .Must(s => new[]
                {
                "IN_SERVICE",
                "OUT_OF_SERVICE"
                }.Contains(s))
                .WithMessage(
                    "Statut invalide. " +
                    "Valeurs autorisées: IN_SERVICE, " +
                    "OUT_OF_SERVICE. " +
                    "TEMPORARILY_UNAVAILABLE est géré " +
                    "automatiquement par la machine.");
        }
    }
}