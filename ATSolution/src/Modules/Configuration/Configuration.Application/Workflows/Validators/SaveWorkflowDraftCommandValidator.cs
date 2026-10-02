using FluentValidation;

namespace Configuration.Application.Workflows.Validators;

public sealed class SaveWorkflowDraftCommandValidator : AbstractValidator<SaveWorkflowDraftCommand>
{
    public SaveWorkflowDraftCommandValidator()
    {
        RuleFor(x => x.Steps).NotNull();
        RuleForEach(x => x.Steps).ChildRules(step =>
        {
            step.RuleFor(s => s.Id).NotEmpty();
            step.RuleFor(s => s.StepName).NotEmpty().MaximumLength(200);
            step.RuleFor(s => s.StepOrder).GreaterThanOrEqualTo(0);
            step.RuleFor(s => s.MinApprovals).GreaterThanOrEqualTo(1);
            step.RuleFor(s => s.NodeType).NotEmpty().MaximumLength(50);
        });
    }
}
