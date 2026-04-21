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
                    "OUT_OF_SERVICE",
                    "MAINTENANCE",
                    "TEMPORARILY_UNAVAILABLE"
                }.Contains(s))
                .WithMessage("Status must be IN_SERVICE, OUT_OF_SERVICE or MAINTENANCE or TEMPORARILY_UNAVAILABLE");
        }
    }
}