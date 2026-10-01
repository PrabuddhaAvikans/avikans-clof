using Configuration.Application.Workflows;
using FluentValidation;

namespace Configuration.Application.Workflows.Validators;

public sealed class ApplyWorkflowDraftCommandValidator : AbstractValidator<ApplyWorkflowDraftCommand>
{
    public ApplyWorkflowDraftCommandValidator()
    {
        RuleFor(x => x.Steps).NotEmpty();
        RuleForEach(x => x.Steps).ChildRules(step =>
        {
            step.RuleFor(s => s.Id).NotEmpty();
            step.RuleFor(s => s.StepName).NotEmpty().MaximumLength(200);
            step.RuleFor(s => s.ApprovalRoleId).NotEmpty();
            step.RuleFor(s => s.ApprovalRoleName).NotEmpty().MaximumLength(200);
            step.RuleFor(s => s.StepOrder).GreaterThanOrEqualTo(0);
            step.RuleFor(s => s.MinApprovals).GreaterThanOrEqualTo(1);
        });
    }
}
