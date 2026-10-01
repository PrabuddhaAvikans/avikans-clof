using Configuration.Application.Workflows;
using FluentValidation;

namespace Configuration.Application.Workflows.Validators;

public sealed class UpsertWorkflowRuleCommandValidator : AbstractValidator<UpsertWorkflowRuleCommand>
{
    public UpsertWorkflowRuleCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Priority).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Conditions).NotNull();
    }
}
