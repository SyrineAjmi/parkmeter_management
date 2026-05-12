using FluentValidation;
using parkmeter_management.Api.Requests.InterventionOrder;

public class CreateInterventionOrderRequestValidator
    : AbstractValidator<CreateInterventionOrderRequest>
{
    public CreateInterventionOrderRequestValidator()
    {
        RuleFor(x => x.ParkmeterId)
            .NotEmpty()
            .WithMessage("Parkmeter ID is required");

        RuleFor(x => x.OperatorId)
            .NotEmpty()
            .WithMessage("Operator ID is required");
    }
}