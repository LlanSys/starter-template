using FluentValidation;
using ST.SharedKernel.Features.Shared.Dashboard;

namespace ST.Application.Features.Shared.WorkTasks;

public sealed class AssignDashboardTaskCommandValidator : AbstractValidator<AssignDashboardTaskCommand>
{
    public AssignDashboardTaskCommandValidator()
    {
        RuleFor(command => command.Request).NotNull();
        When(command => command.Request is not null, () =>
        {
            RuleFor(command => command.Request.Task).NotNull();
            When(command => command.Request.Task is not null, () =>
            {
                RuleFor(command => command.Request.Task.Kind).Must(kind => kind is not null && DashboardTaskCatalog.Definitions.ContainsKey(kind));
                RuleFor(command => command.Request.Task.SubjectId).NotEmpty();
                RuleFor(command => command.Request.Task.CycleAt).Must(at => at.Offset == TimeSpan.Zero);
            });

            RuleFor(command => command.Request.AssigneeUserId).MaximumLength(128)
                .Must(id => id is null || !string.IsNullOrWhiteSpace(id));

            RuleFor(command => command.Request.ExpectedRevision).Must(revision => revision is null || revision != Guid.Empty);
        });
    }
}
