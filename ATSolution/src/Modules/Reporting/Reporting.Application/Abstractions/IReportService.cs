using Reporting.Application.Reports;

namespace Reporting.Application.Abstractions;

public interface IReportService
{
    Task<ReportDatasetDto> GetReportAsync(string reportId, CancellationToken cancellationToken = default);
}
