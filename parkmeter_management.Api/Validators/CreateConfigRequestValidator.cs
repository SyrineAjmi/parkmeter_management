using FluentValidation;
using parkmeter_management.Api.Requests.Configuration;
using parkmeter_management.Domain.Enums;

public class CreateConfigRequestValidator
    : AbstractValidator<CreateConfigRequest>
{
    public CreateConfigRequestValidator()
    {
        RuleFor(x => x.MaxDuration)
            .GreaterThan(0);

        When(x => x.StrategyType == PricingStrategyType.FLAT_RATE, () =>
        {
            RuleFor(x => x.PricePerHour)
                .NotNull()
                .GreaterThan(0)
                .WithMessage("PricePerHour is required for FLAT_RATE");
        });

        When(x => x.StrategyType == PricingStrategyType.TIME_OF_DAY, () =>
        {
            RuleFor(x => x.PeakRate)
                .NotNull()
                .GreaterThan(0)
                .WithMessage("PeakRate is required for TIME_OF_DAY");

            RuleFor(x => x.OffPeakRate)
                .NotNull()
                .GreaterThan(0)
                .WithMessage("OffPeakRate is required for TIME_OF_DAY");
        });

        When(x => x.StrategyType == PricingStrategyType.WEEKDAY_WEEKEND, () =>
        {
            RuleFor(x => x.WeekdayRate)
                .NotNull()
                .GreaterThan(0)
                .WithMessage("WeekdayRate is required for WEEKDAY_WEEKEND");

            RuleFor(x => x.WeekendRate)
                .NotNull()
                .GreaterThan(0)
                .WithMessage("WeekendRate is required for WEEKDAY_WEEKEND");
        });
    }
}