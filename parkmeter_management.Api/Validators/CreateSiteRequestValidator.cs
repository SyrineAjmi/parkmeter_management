using FluentValidation;
using parkmeter_management.Api.Controllers;

namespace parkmeter_management.Api.Validators
{
    public class CreateSiteRequestValidator
        : AbstractValidator<CreateSiteRequest>
    {
        public CreateSiteRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Site name is required")
                .MaximumLength(100)
                .WithMessage("Name cannot exceed 100 characters");

            RuleFor(x => x.Address)
                .NotEmpty()
                .WithMessage("Address is required")
                .MaximumLength(200)
                .WithMessage("Address cannot exceed 200 characters");
        }
    }
}