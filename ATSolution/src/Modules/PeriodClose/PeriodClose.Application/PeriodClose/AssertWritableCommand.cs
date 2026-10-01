using ATSolution.SharedKernel.Models;

namespace PeriodClose.Application.PeriodClose;

public sealed record AssertWritableCommand(string BranchId, string BusinessDate);
