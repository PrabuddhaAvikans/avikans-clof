using ATSolution.SharedKernel.Models;

namespace PeriodClose.Application.PeriodClose;

public sealed record CloseDayOptions(
    bool SupervisorConfirmed = false,
    bool IncompleteHoursExceptionConfirmed = false,
    bool OvertimeApproved = false);
