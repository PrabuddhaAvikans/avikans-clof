using Configuration.Application.Workflows;
using FluentValidation;

namespace Configuration.Application.Workflows.Validators;

public sealed class UpsertWorkflowDefinitionCommandValidator : AbstractValidator<UpsertWorkflowDefinitionCommand>
{
    public UpsertWorkflowDefinitionCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.Module).NotEmpty().MaximumLength(50);
        RuleForEach(x => x.Steps!).ChildRules(step =>
        {
            step.RuleFor(s => s.Id).NotEmpty();
            step.RuleFor(s => s.StepName).NotEmpty().MaximumLength(200);
            step.RuleFor(s => s.ApprovalRoleId).NotEmpty();
            step.RuleFor(s => s.ApprovalRoleName).NotEmpty();
            step.RuleFor(s => s.StepOrder).GreaterThanOrEqualTo(0);
            step.RuleFor(s => s.MinApprovals).GreaterThanOrEqualTo(1);
        }).When(x => x.Steps is not null);

        RuleForEach(x => x.Rules!).SetValidator(new UpsertWorkflowRuleCommandValidator())
            .When(x => x.Rules is not null);
    }
}

public sealed class UpsertWorkflowRuleCommandValidator : AbstractValidator<UpsertWorkflowRuleCommand>
{
    public UpsertWorkflowRuleCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Priority).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Conditions).NotNull();
    }
}
