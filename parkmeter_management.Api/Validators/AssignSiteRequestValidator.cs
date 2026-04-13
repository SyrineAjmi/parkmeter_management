using FluentValidation;
using parkmeter_management.Api.Controllers;

namespace parkmeter_management.Api.Validators
{
    public class AssignSiteRequestValidator
        : AbstractValidator<AssignSiteRequest>
    {
        public AssignSiteRequestValidator()
        {
            RuleFor(x => x.SiteId)
                .NotEmpty()
                .WithMessage("Site ID is required");
        }
    }
}