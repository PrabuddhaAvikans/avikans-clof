using ATSolution.SharedKernel.Models;

namespace Notifications.Application.Notifications;

public sealed record MarkAllReadCommand(string RecipientId);
