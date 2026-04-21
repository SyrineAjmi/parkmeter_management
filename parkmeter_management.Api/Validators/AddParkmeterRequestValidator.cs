using FluentValidation;
using parkmeter_management.Api.Requests.Parkmeter;

namespace parkmeter_management.Api.Validators
{
    public class AddParkmeterRequestValidator
        : AbstractValidator<AddParkmeterRequest>
    {
        public AddParkmeterRequestValidator()
        {
            RuleFor(x => x.SerialNumber)
                .NotEmpty()
                .WithMessage("Serial number is required")
                .MaximumLength(50)
                .WithMessage("Serial number cannot exceed 50 characters")
                .Matches(@"^[a-zA-Z0-9\-]+$")
                .WithMessage("Serial number can only contain letters, numbers and hyphens");

            RuleFor(x => x.SiteId)
                .NotEmpty()
                .WithMessage("Site is required");
        }
    }
}