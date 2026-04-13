using FluentValidation;
using parkmeter_management.Api.Controllers;

namespace parkmeter_management.Api.Validators
{
    public class UpdateConfigRequestValidator
        : AbstractValidator<UpdateConfigRequest>
    {
        public UpdateConfigRequestValidator()
        {
            RuleFor(x => x.ParkmeterId)
                            .NotEmpty()
                            .WithMessage("Parkmeter ID is required");

            RuleFor(x => x.PricePerHour)
                .GreaterThan(0)
                .WithMessage("Price per hour must be greater than 0")
                .LessThanOrEqualTo(100)
                .WithMessage("Price per hour cannot exceed 100 DT");

            RuleFor(x => x.MaxDuration)
                .InclusiveBetween(15, 240)
                .WithMessage(
                    "Max duration must be between 15 and 360 minutes");//6H
        }
    }
}