using ATSolution.Application;
using ATSolution.Application.Abstractions.Periods;
using ATSolution.Application.Abstractions.Persistence;
using ATSolution.Application.Abstractions.Validation;
using ATSolution.Application.Exceptions;
using ATSolution.SharedKernel.Constants;
using ATSolution.SharedKernel.Models;
using Microsoft.EntityFrameworkCore;
using PeriodClose.Application.Abstractions;
using PeriodClose.Application.Common;
using PeriodClose.Application.PeriodClose;
using PeriodClose.Domain.Audit;
using PeriodClose.Domain.Common;
using PeriodClose.Domain.Periods;
using PeriodClose.Domain.Settings;
using PeriodClose.Domain.Snapshots;
using PeriodClose.Domain.Summaries;
using PeriodClose.Domain.Validations;

namespace PeriodClose.Application.Services;

public sealed class PeriodCloseService : IPeriodCloseService, IBusinessPeriodGuard
{
    private readonly IRepository<BusinessPeriod, Guid> _dayPeriods;
    private readonly IRepository<MonthlyPeriod, Guid> _monthPeriods;
    private readonly IRepository<PeriodCloseSettings, Guid> _settings;
    private readonly IRepository<DayCloseValidationIssue, Guid> _dayValidations;
    private readonly IRepository<MonthlyCloseValidationIssue, Guid> _monthValidations;
    private readonly IRepository<DailyClosingSummary, Guid> _daySummaries;
    private readonly IRepository<MonthlyClosingSummary, Guid> _monthSummaries;
    private readonly IRepository<ProductionDailySnapshot, Guid> _productionDaily;
    private readonly IRepository<ProductionMonthlySnapshot, Guid> _productionMonthly;
    private readonly IRepository<InventoryDailySnapshot, Guid> _inventoryDaily;
    private readonly IRepository<InventoryMonthlySnapshot, Guid> _inventoryMonthly;
    private readonly IRepository<WorkerSessionCheckpoint, Guid> _sessionCheckpoints;
    private readonly IRepository<PeriodAuditLog, Guid> _auditLogs;
    private readonly IRepository<PeriodAdjustment, Guid> _adjustments;
    private readonly IPeriodCloseOperations _operations;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IApplicationValidator _validator;

    public PeriodCloseService(
        IRepository<BusinessPeriod, Guid> dayPeriods,
        IRepository<MonthlyPeriod, Guid> monthPeriods,
        IRepository<PeriodCloseSettings, Guid> settings,
        IRepository<DayCloseValidationIssue, Guid> dayValidations,
        IRepository<MonthlyCloseValidationIssue, Guid> monthValidations,
        IRepository<DailyClosingSummary, Guid> daySummaries,
        IRepository<MonthlyClosingSummary, Guid> monthSummaries,
        IRepository<ProductionDailySnapshot, Guid> productionDaily,
        IRepository<ProductionMonthlySnapshot, Guid> productionMonthly,
        IRepository<InventoryDailySnapshot, Guid> inventoryDaily,
        IRepository<InventoryMonthlySnapshot, Guid> inventoryMonthly,
        IRepository<WorkerSessionCheckpoint, Guid> sessionCheckpoints,
        IRepository<PeriodAuditLog, Guid> auditLogs,
        IRepository<PeriodAdjustment, Guid> adjustments,
        IPeriodCloseOperations operations,
        IUnitOfWork unitOfWork,
        IApplicationValidator validator)
    {
        _dayPeriods = dayPeriods;
        _monthPeriods = monthPeriods;
        _settings = settings;
        _dayValidations = dayValidations;
        _monthValidations = monthValidations;
        _daySummaries = daySummaries;
        _monthSummaries = monthSummaries;
        _productionDaily = productionDaily;
        _productionMonthly = productionMonthly;
        _inventoryDaily = inventoryDaily;
        _inventoryMonthly = inventoryMonthly;
        _sessionCheckpoints = sessionCheckpoints;
        _auditLogs = auditLogs;
        _adjustments = adjustments;
        _operations = operations;
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    public async Task<PeriodCloseSettingsDto> GetSettingsAsync(
        string? branchId = null,
        CancellationToken cancellationToken = default)
    {
        var settings = await EnsureSettingsAsync(NormalizeBranch(branchId), cancellationToken);
        return MapSettings(settings);
    }

    public async Task<PeriodCloseSettingsDto> UpdateSettingsAsync(
        UpdatePeriodCloseSettingsCommand command,
        CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAsync(command, cancellationToken);
        var branchId = NormalizeBranch(command.BranchId);
        var settings = await EnsureSettingsAsync(branchId, cancellationToken);
        settings.Update(
            command.WorkerSessionCloseRule,
            command.AllowNegativeStock,
            command.RequireAllDaysClosedForMonthlyClose,
            command.FiscalYearStartMonth,
            command.RequiredDailyWorkMinutes,
            command.AllowIncompleteEmployeeHoursException,
            command.CountPauseAsWorked,
            command.DoubleOvertimeAfterMinutes,
            command.RequireOvertimeApproval);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapSettings(settings);
    }

    public async Task<PaginatedResponse<BusinessPeriodDto>> ListDayPeriodsAsync(
        BusinessPeriodListQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Max(1, query.PageSize);
        var periods = _dayPeriods.Query().AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.BranchId))
            periods = periods.Where(p => p.BranchId == NormalizeBranch(query.BranchId));
        if (!string.IsNullOrWhiteSpace(query.Status))
            periods = periods.Where(p => p.Status == query.Status);
        if (!string.IsNullOrWhiteSpace(query.From))
            periods = periods.Where(p => string.Compare(p.BusinessDate, query.From) >= 0);
        if (!string.IsNullOrWhiteSpace(query.To))
            periods = periods.Where(p => string.Compare(p.BusinessDate, query.To) <= 0);

        periods = periods.OrderByDescending(p => p.BusinessDate);
        var totalCount = await periods.CountAsync(cancellationToken);
        var items = await periods.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return PaginatedResponse<BusinessPeriodDto>.Create(items.Select(MapDay).ToList(), totalCount, page, pageSize);
    }

    public async Task<PaginatedResponse<MonthlyPeriodDto>> ListMonthlyPeriodsAsync(
        MonthlyPeriodListQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Max(1, query.PageSize);
        var periods = _monthPeriods.Query().AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.BranchId))
            periods = periods.Where(p => p.BranchId == NormalizeBranch(query.BranchId));
        if (!string.IsNullOrWhiteSpace(query.Status))
            periods = periods.Where(p => p.Status == query.Status);
        if (query.Year.HasValue)
            periods = periods.Where(p => p.Year == query.Year.Value);

        periods = periods.OrderByDescending(p => p.Year).ThenByDescending(p => p.Month);
        var totalCount = await periods.CountAsync(cancellationToken);
        var items = await periods.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return PaginatedResponse<MonthlyPeriodDto>.Create(items.Select(MapMonth).ToList(), totalCount, page, pageSize);
    }

    public async Task<DayCloseWorkspaceDto> GetCurrentDayAsync(
        string? branchId = null,
        string? actorUserId = null,
        string? actorUserName = null,
        CancellationToken cancellationToken = default)
    {
        var branch = NormalizeBranch(branchId);
        var (userId, userName) = ResolveActor(actorUserId, actorUserName);
        var period = await EnsureCurrentDayAsync(branch, userId, userName, cancellationToken);
        return await BuildDayWorkspaceAsync(period, cancellationToken);
    }

    public async Task<DayCloseWorkspaceDto> GetDayWorkspaceAsync(
        Guid periodId,
        CancellationToken cancellationToken = default)
    {
        var period = await RequireDayAsync(periodId, cancellationToken);
        return await BuildDayWorkspaceAsync(period, cancellationToken);
    }

    public async Task<MonthlyCloseWorkspaceDto> GetCurrentMonthAsync(
        string? branchId = null,
        string? actorUserId = null,
        string? actorUserName = null,
        CancellationToken cancellationToken = default)
    {
        var branch = NormalizeBranch(branchId);
        var (userId, userName) = ResolveActor(actorUserId, actorUserName);
        var period = await EnsureCurrentMonthAsync(branch, userId, userName, cancellationToken);
        return await BuildMonthWorkspaceAsync(period, cancellationToken);
    }

    public async Task<MonthlyCloseWorkspaceDto> GetMonthWorkspaceAsync(
        Guid periodId,
        CancellationToken cancellationToken = default)
    {
        var period = await RequireMonthAsync(periodId, cancellationToken);
        return await BuildMonthWorkspaceAsync(period, cancellationToken);
    }

    public async Task<DayCloseWorkspaceDto> RunDayValidationAsync(
        Guid periodId,
        CloseDayOptions? options = null,
        string? actorUserId = null,
        string? actorUserName = null,
        CancellationToken cancellationToken = default)
    {
        var period = await RequireDayAsync(periodId, cancellationToken, track: true);
        var (userId, userName) = ResolveActor(actorUserId, actorUserName);
        var settings = await EnsureSettingsAsync(period.BranchId, cancellationToken);
        var picture = await _operations.LoadDayAsync(period.BusinessDate, ToPolicy(settings), cancellationToken);
        await PersistDayValidationsAsync(period, options, picture, cancellationToken);
        await AddAuditAsync(PeriodTypes.Day, period.Id, "validation_run", userId, userName, cancellationToken: cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await BuildDayWorkspaceAsync(period, cancellationToken);
    }

    public async Task<CloseDayResultDto> CloseDayAsync(
        Guid periodId,
        CloseDayOptions? options = null,
        string? actorUserId = null,
        string? actorUserName = null,
        CancellationToken cancellationToken = default)
    {
        var period = await RequireDayAsync(periodId, cancellationToken, track: true);
        var (userId, userName) = ResolveActor(actorUserId, actorUserName);
        options ??= new CloseDayOptions();

        if (!PeriodStatuses.CanClose(period.Status))
        {
            throw new ApplicationValidationException(
            [
                new ValidationError("status", $"Cannot close a day in status \"{period.Status}\".", PeriodCloseErrorCodes.InvalidState),
            ]);
        }

        var (year, month, _) = ParseBusinessDate(period.BusinessDate);
        var parentMonth = await _monthPeriods.Query()
            .FirstOrDefaultAsync(
                m => m.BranchId == period.BranchId && m.Year == year && m.Month == month,
                cancellationToken);
        if (parentMonth is not null && PeriodStatuses.IsLocked(parentMonth.Status))
        {
            throw new ApplicationValidationException(
            [
                new ValidationError(
                    "businessDate",
                    $"Cannot close day {period.BusinessDate} because the month is already closed.",
                    PeriodCloseErrorCodes.MonthLocked),
            ]);
        }

        var settings = await EnsureSettingsAsync(period.BranchId, cancellationToken);
        var picture = await _operations.LoadDayAsync(period.BusinessDate, ToPolicy(settings), cancellationToken);
        var issues = await PersistDayValidationsAsync(period, options, picture, cancellationToken);
        if (issues.Any(i => i.IsBlocking))
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            throw new ApplicationValidationException(
            [
                new ValidationError(
                    "validations",
                    "Day Close blocked by validation errors. Fix blocking issues and retry.",
                    PeriodCloseErrorCodes.ValidationFailed),
            ]);
        }

        var now = DateTimeOffset.UtcNow;
        await AddAuditAsync(PeriodTypes.Day, period.Id, "closing_started", userId, userName, cancellationToken: cancellationToken);

        IReadOnlyList<SessionCheckpointSeed> checkpoints = [];
        if (settings.WorkerSessionCloseRule != WorkerSessionCloseRules.AllowCrossDate)
        {
            checkpoints = await _operations.PauseOpenWorkAsync(
                period.BusinessDate,
                $"Day Close checkpoint for {period.BusinessDate}",
                cancellationToken);
        }

        await ReplaceDailySnapshotsAsync(period, picture, checkpoints, now, cancellationToken);

        var summary = DailyClosingSummary.Create(
            period.Id,
            period.BusinessDate,
            period.BranchId,
            JsonColumn.Serialize(picture.Figures.TransactionRefs),
            now);
        summary.Apply(
            picture.Figures.OrdersCreated,
            picture.Figures.ProductionJobs,
            picture.Figures.CompletedProductionQty,
            picture.Figures.PartialProductionQty,
            picture.Figures.Invoices,
            picture.Figures.InvoiceTotal,
            picture.Figures.Payments,
            picture.Figures.PaymentTotal,
            picture.Figures.Deliveries,
            picture.Figures.MaterialIssues,
            picture.Figures.MaterialReturns,
            picture.Figures.InventoryMovementCount,
            picture.Figures.QuotationValue,
            picture.Figures.SalesOrderValue,
            picture.Figures.CreditNoteTotal,
            picture.Figures.PaymentTotal,
            0,
            0,
            0,
            0,
            picture.Figures.OpeningReceivable,
            picture.Figures.ClosingReceivable,
            picture.Figures.OutstandingAmount,
            JsonColumn.Serialize(picture.Figures.TransactionRefs));

        var existingSummaries = await _daySummaries.Query()
            .Where(s => s.BusinessPeriodId == period.Id)
            .ToListAsync(cancellationToken);
        foreach (var existing in existingSummaries)
            _daySummaries.Remove(existing);
        await _daySummaries.AddAsync(summary, cancellationToken);

        await AddAuditAsync(
            PeriodTypes.Day,
            period.Id,
            "snapshots_generated",
            userId,
            userName,
            detailsJson: JsonColumn.Serialize(new
            {
                production = picture.Production.Count,
                inventory = picture.Inventory.Count,
            }),
            cancellationToken: cancellationToken);
        await AddAuditAsync(
            PeriodTypes.Day,
            period.Id,
            "sessions_checkpointed",
            userId,
            userName,
            detailsJson: JsonColumn.Serialize(new
            {
                count = checkpoints.Count,
                rule = settings.WorkerSessionCloseRule,
            }),
            cancellationToken: cancellationToken);

        period.Close(userId, userName, now);
        await AddAuditAsync(PeriodTypes.Day, period.Id, "closed", userId, userName, cancellationToken: cancellationToken);

        var nextDate = AddBusinessDays(period.BusinessDate, 1);
        var nextPeriod = await _dayPeriods.Query()
            .FirstOrDefaultAsync(
                p => p.BranchId == period.BranchId && p.BusinessDate == nextDate,
                cancellationToken);
        if (nextPeriod is null)
        {
            nextPeriod = BusinessPeriod.Open(period.BranchId, nextDate, userId, userName, now);
            await _dayPeriods.AddAsync(nextPeriod, cancellationToken);
            await AddAuditAsync(PeriodTypes.Day, nextPeriod.Id, "opened", userId, userName, cancellationToken: cancellationToken);
            await AddAuditAsync(
                PeriodTypes.Day,
                period.Id,
                "next_period_opened",
                userId,
                userName,
                detailsJson: JsonColumn.Serialize(new { nextBusinessDate = nextDate }),
                cancellationToken: cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CloseDayResultDto(
            MapDay(period),
            MapDay(nextPeriod),
            MapDailySummary(summary),
            issues.Select(MapDayIssue).ToList(),
            picture.Production,
            picture.Inventory,
            checkpoints.Select(seed => MapCheckpointSeed(period.Id, period.BusinessDate, seed, settings.WorkerSessionCloseRule, now)).ToList());
    }

    public async Task<BusinessPeriodDto> ReopenDayAsync(
        Guid periodId,
        ReopenPeriodCommand command,
        string? actorUserId = null,
        string? actorUserName = null,
        CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAsync(command, cancellationToken);
        var period = await RequireDayAsync(periodId, cancellationToken, track: true);
        var (userId, userName) = ResolveActor(actorUserId, actorUserName);

        if (period.Status != PeriodStatuses.Closed)
        {
            throw new ApplicationValidationException(
            [
                new ValidationError("status", $"Cannot reopen a day in status \"{period.Status}\".", PeriodCloseErrorCodes.InvalidState),
            ]);
        }

        var now = DateTimeOffset.UtcNow;
        period.Reopen(userId, userName, command.Reason.Trim(), now);
        await AddAuditAsync(
            PeriodTypes.Day,
            period.Id,
            "reopened",
            userId,
            userName,
            command.Reason.Trim(),
            JsonColumn.Serialize(new
            {
                originalClosedAt = period.OriginalClosedAt,
                originalClosedBy = period.OriginalClosedBy,
                originalClosedByName = period.OriginalClosedByName,
            }),
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapDay(period);
    }

    public async Task<MonthlyCloseWorkspaceDto> RunMonthValidationAsync(
        Guid periodId,
        string? actorUserId = null,
        string? actorUserName = null,
        CancellationToken cancellationToken = default)
    {
        var period = await RequireMonthAsync(periodId, cancellationToken, track: true);
        var (userId, userName) = ResolveActor(actorUserId, actorUserName);
        var settings = await EnsureSettingsAsync(period.BranchId, cancellationToken);
        var picture = await _operations.LoadMonthAsync(period.Year, period.Month, settings.AllowNegativeStock, cancellationToken);
        await PersistMonthValidationsAsync(period, picture, cancellationToken);
        await AddAuditAsync(PeriodTypes.Month, period.Id, "validation_run", userId, userName, cancellationToken: cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await BuildMonthWorkspaceAsync(period, cancellationToken);
    }

    public async Task<CloseMonthResultDto> CloseMonthAsync(
        Guid periodId,
        string? actorUserId = null,
        string? actorUserName = null,
        CancellationToken cancellationToken = default)
    {
        var period = await RequireMonthAsync(periodId, cancellationToken, track: true);
        var (userId, userName) = ResolveActor(actorUserId, actorUserName);

        if (!PeriodStatuses.CanClose(period.Status))
        {
            throw new ApplicationValidationException(
            [
                new ValidationError("status", $"Cannot close a month in status \"{period.Status}\".", PeriodCloseErrorCodes.InvalidState),
            ]);
        }

        var settings = await EnsureSettingsAsync(period.BranchId, cancellationToken);
        var picture = await _operations.LoadMonthAsync(period.Year, period.Month, settings.AllowNegativeStock, cancellationToken);
        var issues = await PersistMonthValidationsAsync(period, picture, cancellationToken);
        if (issues.Any(i => i.IsBlocking))
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            throw new ApplicationValidationException(
            [
                new ValidationError(
                    "validations",
                    "Monthly Close blocked by validation errors. Fix blocking issues and retry.",
                    PeriodCloseErrorCodes.ValidationFailed),
            ]);
        }

        var now = DateTimeOffset.UtcNow;
        await AddAuditAsync(PeriodTypes.Month, period.Id, "closing_started", userId, userName, cancellationToken: cancellationToken);
        await ReplaceMonthlySnapshotsAsync(period, picture, now, cancellationToken);

        var summary = MonthlyClosingSummary.Create(
            period.Id,
            period.Year,
            period.Month,
            period.BranchId,
            JsonColumn.Serialize(picture.Figures.TransactionRefs),
            now);
        var figures = picture.Figures;
        summary.Apply(
            figures.SalesTotal,
            figures.PurchaseTotal,
            figures.PaymentTotal,
            figures.ExpenseTotal,
            figures.InventoryValue,
            figures.WipValue,
            figures.CostOfGoodsSold,
            figures.GrossProfit,
            figures.RawMaterials,
            figures.Labour,
            figures.Production,
            figures.Waste,
            figures.ReusableWaste,
            figures.Overhead,
            figures.CreditNotes,
            figures.NetMargin,
            JsonColumn.Serialize(figures.TransactionRefs));

        var existingSummaries = await _monthSummaries.Query()
            .Where(s => s.MonthlyPeriodId == period.Id)
            .ToListAsync(cancellationToken);
        foreach (var existing in existingSummaries)
            _monthSummaries.Remove(existing);
        await _monthSummaries.AddAsync(summary, cancellationToken);

        await AddAuditAsync(
            PeriodTypes.Month,
            period.Id,
            "snapshots_generated",
            userId,
            userName,
            detailsJson: JsonColumn.Serialize(new
            {
                production = picture.Production.Count,
                inventory = picture.Inventory.Count,
            }),
            cancellationToken: cancellationToken);

        period.Close(userId, userName, now);
        await AddAuditAsync(PeriodTypes.Month, period.Id, "closed", userId, userName, cancellationToken: cancellationToken);

        var (nextYear, nextMonth) = NextCalendarMonth(period.Year, period.Month);
        var nextPeriod = await _monthPeriods.Query()
            .FirstOrDefaultAsync(
                m => m.BranchId == period.BranchId && m.Year == nextYear && m.Month == nextMonth,
                cancellationToken);
        if (nextPeriod is null)
        {
            nextPeriod = MonthlyPeriod.Open(period.BranchId, nextYear, nextMonth, userId, userName, now);
            await _monthPeriods.AddAsync(nextPeriod, cancellationToken);
            await AddAuditAsync(PeriodTypes.Month, nextPeriod.Id, "opened", userId, userName, cancellationToken: cancellationToken);
            await AddAuditAsync(
                PeriodTypes.Month,
                period.Id,
                "next_period_opened",
                userId,
                userName,
                detailsJson: JsonColumn.Serialize(new { nextYear, nextMonth }),
                cancellationToken: cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CloseMonthResultDto(
            MapMonth(period),
            MapMonth(nextPeriod),
            MapMonthlySummary(summary),
            issues.Select(MapMonthIssue).ToList(),
            picture.Production,
            picture.Inventory);
    }

    public async Task<MonthlyPeriodDto> ReopenMonthAsync(
        Guid periodId,
        ReopenPeriodCommand command,
        string? actorUserId = null,
        string? actorUserName = null,
        CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAsync(command, cancellationToken);
        var period = await RequireMonthAsync(periodId, cancellationToken, track: true);
        var (userId, userName) = ResolveActor(actorUserId, actorUserName);

        if (period.Status != PeriodStatuses.Closed)
        {
            throw new ApplicationValidationException(
            [
                new ValidationError("status", $"Cannot reopen a month in status \"{period.Status}\".", PeriodCloseErrorCodes.InvalidState),
            ]);
        }

        var now = DateTimeOffset.UtcNow;
        period.Reopen(userId, userName, command.Reason.Trim(), now);
        await AddAuditAsync(
            PeriodTypes.Month,
            period.Id,
            "reopened",
            userId,
            userName,
            command.Reason.Trim(),
            cancellationToken: cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapMonth(period);
    }

    public async Task<IReadOnlyList<PeriodAuditLogDto>> ListDayAuditAsync(
        Guid periodId,
        CancellationToken cancellationToken = default)
    {
        await RequireDayAsync(periodId, cancellationToken);
        var logs = await _auditLogs.Query().AsNoTracking()
            .Where(a => a.PeriodType == PeriodTypes.Day && a.PeriodId == periodId)
            .OrderByDescending(a => a.PerformedAt)
            .ToListAsync(cancellationToken);
        return logs.Select(MapAudit).ToList();
    }

    public async Task<IReadOnlyList<PeriodAuditLogDto>> ListMonthAuditAsync(
        Guid periodId,
        CancellationToken cancellationToken = default)
    {
        await RequireMonthAsync(periodId, cancellationToken);
        var logs = await _auditLogs.Query().AsNoTracking()
            .Where(a => a.PeriodType == PeriodTypes.Month && a.PeriodId == periodId)
            .OrderByDescending(a => a.PerformedAt)
            .ToListAsync(cancellationToken);
        return logs.Select(MapAudit).ToList();
    }

    public async Task<DailyClosingSummaryDto?> GetDailySummaryAsync(
        Guid periodId,
        CancellationToken cancellationToken = default)
    {
        var summary = await _daySummaries.Query().AsNoTracking()
            .FirstOrDefaultAsync(s => s.BusinessPeriodId == periodId, cancellationToken);
        return summary is null ? null : MapDailySummary(summary);
    }

    public async Task<MonthlyClosingSummaryDto?> GetMonthlySummaryAsync(
        Guid periodId,
        CancellationToken cancellationToken = default)
    {
        var summary = await _monthSummaries.Query().AsNoTracking()
            .FirstOrDefaultAsync(s => s.MonthlyPeriodId == periodId, cancellationToken);
        return summary is null ? null : MapMonthlySummary(summary);
    }

    public async Task<IReadOnlyList<ProductionDailySnapshotDto>> ListProductionDailySnapshotsAsync(
        Guid periodId,
        CancellationToken cancellationToken = default)
    {
        var items = await _productionDaily.Query().AsNoTracking()
            .Where(s => s.BusinessPeriodId == periodId)
            .OrderBy(s => s.ProductionOrderNumber)
            .ToListAsync(cancellationToken);
        return items.Select(MapProductionDaily).ToList();
    }

    public async Task<IReadOnlyList<ProductionMonthlySnapshotDto>> ListProductionMonthlySnapshotsAsync(
        Guid periodId,
        CancellationToken cancellationToken = default)
    {
        var items = await _productionMonthly.Query().AsNoTracking()
            .Where(s => s.MonthlyPeriodId == periodId)
            .OrderBy(s => s.ProductionOrderNumber)
            .ToListAsync(cancellationToken);
        return items.Select(MapProductionMonthly).ToList();
    }

    public async Task<IReadOnlyList<InventoryDailySnapshotDto>> ListInventoryDailySnapshotsAsync(
        Guid periodId,
        CancellationToken cancellationToken = default)
    {
        var items = await _inventoryDaily.Query().AsNoTracking()
            .Where(s => s.BusinessPeriodId == periodId)
            .OrderBy(s => s.Sku)
            .ToListAsync(cancellationToken);
        return items.Select(MapInventoryDaily).ToList();
    }

    public async Task<IReadOnlyList<InventoryMonthlySnapshotDto>> ListInventoryMonthlySnapshotsAsync(
        Guid periodId,
        CancellationToken cancellationToken = default)
    {
        var items = await _inventoryMonthly.Query().AsNoTracking()
            .Where(s => s.MonthlyPeriodId == periodId)
            .OrderBy(s => s.Sku)
            .ToListAsync(cancellationToken);
        return items.Select(MapInventoryMonthly).ToList();
    }

    public async Task<IReadOnlyList<WorkerSessionCheckpointDto>> ListSessionCheckpointsAsync(
        Guid periodId,
        CancellationToken cancellationToken = default)
    {
        var items = await _sessionCheckpoints.Query().AsNoTracking()
            .Where(s => s.BusinessPeriodId == periodId)
            .OrderByDescending(s => s.CheckpointAt)
            .ToListAsync(cancellationToken);
        return items.Select(MapCheckpoint).ToList();
    }

    public async Task<PeriodAdjustmentDto> CreateAdjustmentAsync(
        CreatePeriodAdjustmentCommand command,
        string? actorUserId = null,
        string? actorUserName = null,
        CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAsync(command, cancellationToken);
        var (userId, userName) = ResolveActor(actorUserId, actorUserName);
        var branchId = NormalizeBranch(command.BranchId);

        await AssertWritableAsync(branchId, command.PostingBusinessDate, cancellationToken);

        var (year, month, _) = ParseBusinessDate(command.PostingBusinessDate);
        var monthly = await _monthPeriods.Query().AsNoTracking()
            .FirstOrDefaultAsync(
                m => m.BranchId == branchId && m.Year == year && m.Month == month,
                cancellationToken);

        var adjustment = PeriodAdjustment.Create(
            branchId,
            command.PostingBusinessDate,
            monthly?.Id,
            command.EntityType,
            command.EntityId,
            command.OriginalBusinessDate,
            command.OriginalTransactionId,
            command.AdjustmentType,
            command.QuantityDelta,
            command.AmountDelta,
            command.Reason.Trim(),
            userId,
            userName);

        await _adjustments.AddAsync(adjustment, cancellationToken);

        var openDay = await _dayPeriods.Query()
            .Where(p => p.BranchId == branchId && PeriodStatuses.IsWritable(p.Status))
            .OrderByDescending(p => p.BusinessDate)
            .FirstOrDefaultAsync(cancellationToken);
        if (openDay is not null)
        {
            await AddAuditAsync(
                PeriodTypes.Day,
                openDay.Id,
                "adjustment_posted",
                userId,
                userName,
                command.Reason.Trim(),
                JsonColumn.Serialize(new { adjustmentId = adjustment.Id }),
                cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapAdjustment(adjustment);
    }

    public async Task<IReadOnlyList<PeriodAdjustmentDto>> ListAdjustmentsAsync(
        string? branchId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _adjustments.Query().AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(branchId))
            query = query.Where(a => a.BranchId == NormalizeBranch(branchId));

        var items = await query.OrderByDescending(a => a.CreatedAt).ToListAsync(cancellationToken);
        return items.Select(MapAdjustment).ToList();
    }

    public Task EnsureWritableAsync(DateTimeOffset occurredAtUtc, CancellationToken cancellationToken = default) =>
        AssertWritableAsync(
            PeriodCloseDefaults.DefaultBranchId,
            occurredAtUtc.UtcDateTime.ToString("yyyy-MM-dd"),
            cancellationToken);

    public async Task AssertWritableAsync(
        string branchId,
        string businessDate,
        CancellationToken cancellationToken = default)
    {
        var branch = NormalizeBranch(branchId);
        var (year, month, _) = ParseBusinessDate(businessDate);

        var monthly = await _monthPeriods.Query().AsNoTracking()
            .FirstOrDefaultAsync(
                m => m.BranchId == branch && m.Year == year && m.Month == month,
                cancellationToken);
        if (monthly is not null && PeriodStatuses.IsLocked(monthly.Status))
        {
            throw new ApplicationValidationException(
            [
                new ValidationError(
                    "businessDate",
                    $"{year}-{month:D2} has already been closed. Please use the current open accounting period or request an authorized period reopen.",
                    PeriodCloseErrorCodes.MonthLocked),
            ]);
        }

        var day = await _dayPeriods.Query().AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.BranchId == branch && p.BusinessDate == businessDate,
                cancellationToken);
        if (day is null)
            return;

        if (day.Status == PeriodStatuses.Closing)
        {
            throw new ApplicationValidationException(
            [
                new ValidationError(
                    "businessDate",
                    $"Business date {businessDate} is currently being closed. Wait for the close to finish or contact an authorized user.",
                    PeriodCloseErrorCodes.PeriodClosing),
            ]);
        }

        if (day.Status == PeriodStatuses.Closed)
        {
            throw new ApplicationValidationException(
            [
                new ValidationError(
                    "businessDate",
                    $"Business date {businessDate} is closed. Post an adjustment in the current open period, or request an authorized reopen.",
                    PeriodCloseErrorCodes.PeriodLocked),
            ]);
        }
    }

    private async Task<DayCloseWorkspaceDto> BuildDayWorkspaceAsync(
        BusinessPeriod period,
        CancellationToken cancellationToken)
    {
        var settings = await EnsureSettingsAsync(period.BranchId, cancellationToken);
        var summary = await _daySummaries.Query().AsNoTracking()
            .FirstOrDefaultAsync(s => s.BusinessPeriodId == period.Id, cancellationToken);
        var validations = await _dayValidations.Query().AsNoTracking()
            .Where(v => v.BusinessPeriodId == period.Id)
            .OrderByDescending(v => v.IsBlocking)
            .ThenBy(v => v.ValidationCode)
            .ToListAsync(cancellationToken);
        var production = await _productionDaily.Query().AsNoTracking()
            .Where(s => s.BusinessPeriodId == period.Id)
            .ToListAsync(cancellationToken);
        var inventory = await _inventoryDaily.Query().AsNoTracking()
            .Where(s => s.BusinessPeriodId == period.Id)
            .ToListAsync(cancellationToken);
        var checkpoints = await _sessionCheckpoints.Query().AsNoTracking()
            .Where(s => s.BusinessPeriodId == period.Id)
            .ToListAsync(cancellationToken);
        var audit = await _auditLogs.Query().AsNoTracking()
            .Where(a => a.PeriodType == PeriodTypes.Day && a.PeriodId == period.Id)
            .OrderByDescending(a => a.PerformedAt)
            .ToListAsync(cancellationToken);

        var live = await _operations.LoadDayAsync(period.BusinessDate, ToPolicy(settings), cancellationToken);
        var writable = PeriodStatuses.IsWritable(period.Status);
        var liveIssues = writable
            ? BuildDayIssues(period, settings, null, live).Select(MapDayIssue).ToList()
            : validations.Select(MapDayIssue).ToList();
        var employees = live.Employees;
        var incomplete = employees.Count(e => !e.CanClose);
        var overtimeEmployees = employees.Where(e => e.OvertimeMinutes > 0).ToList();
        var productionRows = writable ? live.Production : production.Select(MapProductionDaily).ToList();
        var inventoryRows = writable ? live.Inventory : inventory.Select(MapInventoryDaily).ToList();
        var activeSessions = writable
            ? live.ActiveSessionCount
            : checkpoints.Count(c => c.Status is "paused" or "awaiting_confirm");
        var canClose = PeriodStatuses.CanClose(period.Status) && !liveIssues.Any(v => v.IsBlocking);
        var canReopen = period.Status == PeriodStatuses.Closed;

        return new DayCloseWorkspaceDto(
            MapDay(period),
            summary is null ? null : MapDailySummary(summary),
            liveIssues,
            productionRows,
            inventoryRows,
            checkpoints.Select(MapCheckpoint).ToList(),
            employees,
            audit.Select(MapAudit).ToList(),
            canClose,
            canReopen,
            activeSessions,
            settings.WorkerSessionCloseRule,
            settings.RequiredDailyWorkMinutes,
            settings.AllowIncompleteEmployeeHoursException,
            incomplete,
            overtimeEmployees.Count,
            overtimeEmployees.Sum(e => e.OvertimeMinutes),
            settings.RequireOvertimeApproval);
    }

    private async Task<MonthlyCloseWorkspaceDto> BuildMonthWorkspaceAsync(
        MonthlyPeriod period,
        CancellationToken cancellationToken)
    {
        var settings = await EnsureSettingsAsync(period.BranchId, cancellationToken);
        var prefix = $"{period.Year}-{period.Month:D2}";
        var dayPeriods = await _dayPeriods.Query().AsNoTracking()
            .Where(p => p.BranchId == period.BranchId && p.BusinessDate.StartsWith(prefix))
            .OrderBy(p => p.BusinessDate)
            .ToListAsync(cancellationToken);

        var summary = await _monthSummaries.Query().AsNoTracking()
            .FirstOrDefaultAsync(s => s.MonthlyPeriodId == period.Id, cancellationToken);
        var validations = await _monthValidations.Query().AsNoTracking()
            .Where(v => v.MonthlyPeriodId == period.Id)
            .OrderByDescending(v => v.IsBlocking)
            .ThenBy(v => v.ValidationCode)
            .ToListAsync(cancellationToken);
        var production = await _productionMonthly.Query().AsNoTracking()
            .Where(s => s.MonthlyPeriodId == period.Id)
            .ToListAsync(cancellationToken);
        var inventory = await _inventoryMonthly.Query().AsNoTracking()
            .Where(s => s.MonthlyPeriodId == period.Id)
            .ToListAsync(cancellationToken);
        var audit = await _auditLogs.Query().AsNoTracking()
            .Where(a => a.PeriodType == PeriodTypes.Month && a.PeriodId == period.Id)
            .OrderByDescending(a => a.PerformedAt)
            .ToListAsync(cancellationToken);

        var requiredDates = BusinessDatesInMonth(period.Year, period.Month);
        var byDate = dayPeriods.ToDictionary(d => d.BusinessDate, d => d);
        var closedDayCount = settings.RequireAllDaysClosedForMonthlyClose
            ? requiredDates.Count(date => byDate.TryGetValue(date, out var day) && day.Status == PeriodStatuses.Closed)
            : dayPeriods.Count(d => d.Status == PeriodStatuses.Closed);
        var openDayCount = settings.RequireAllDaysClosedForMonthlyClose
            ? requiredDates.Count - closedDayCount
            : dayPeriods.Count(d => PeriodStatuses.IsWritable(d.Status) || d.Status == PeriodStatuses.Closing);
        var live = PeriodStatuses.IsWritable(period.Status)
            ? await _operations.LoadMonthAsync(period.Year, period.Month, settings.AllowNegativeStock, cancellationToken)
            : null;
        var liveIssues = live is null
            ? validations.Select(MapMonthIssue).ToList()
            : BuildMonthIssues(period, settings, dayPeriods, live).Select(MapMonthIssue).ToList();
        var canClose = PeriodStatuses.CanClose(period.Status) && !liveIssues.Any(v => v.IsBlocking);

        return new MonthlyCloseWorkspaceDto(
            MapMonth(period),
            summary is null ? null : MapMonthlySummary(summary),
            liveIssues,
            live?.Production ?? production.Select(MapProductionMonthly).ToList(),
            live?.Inventory ?? inventory.Select(MapInventoryMonthly).ToList(),
            dayPeriods.Select(MapDay).ToList(),
            audit.Select(MapAudit).ToList(),
            canClose,
            period.Status == PeriodStatuses.Closed,
            openDayCount,
            closedDayCount);
    }

    private async Task<IReadOnlyList<DayCloseValidationIssue>> PersistDayValidationsAsync(
        BusinessPeriod period,
        CloseDayOptions? options,
        DayOperationalPicture picture,
        CancellationToken cancellationToken)
    {
        var existing = await _dayValidations.Query()
            .Where(v => v.BusinessPeriodId == period.Id)
            .ToListAsync(cancellationToken);
        foreach (var item in existing)
            _dayValidations.Remove(item);

        var settings = await EnsureSettingsAsync(period.BranchId, cancellationToken);
        var issues = BuildDayIssues(period, settings, options, picture);
        foreach (var issue in issues)
            await _dayValidations.AddAsync(issue, cancellationToken);

        return issues;
    }

    private static List<DayCloseValidationIssue> BuildDayIssues(
        BusinessPeriod period,
        PeriodCloseSettings settings,
        CloseDayOptions? options,
        DayOperationalPicture picture)
    {
        var issues = new List<DayCloseValidationIssue>();
        foreach (var issue in picture.Issues)
        {
            issues.Add(DayCloseValidationIssue.Create(
                period.Id,
                issue.Code,
                issue.Type,
                issue.Message,
                issue.IsBlocking,
                issue.EntityType,
                issue.EntityId));
        }

        if (settings.WorkerSessionCloseRule == WorkerSessionCloseRules.RequireSupervisorConfirm
            && picture.ActiveSessionCount > 0
            && options?.SupervisorConfirmed != true)
        {
            issues.Add(DayCloseValidationIssue.Create(
                period.Id,
                "ACTIVE_SESSIONS_NEED_CONFIRM",
                "production",
                $"{picture.ActiveSessionCount} active worker session(s) require supervisor confirmation before Day Close.",
                isBlocking: true));
        }

        foreach (var employee in picture.Employees.Where(e => !e.CanClose))
        {
            var workedH = (employee.WorkedMinutes / 60d).ToString("0.0");
            var requiredH = (employee.RequiredMinutes / 60d).ToString("0");
            var remainingH = (employee.RemainingMinutes / 60d).ToString("0.0");
            if (settings.AllowIncompleteEmployeeHoursException && options?.IncompleteHoursExceptionConfirmed == true)
            {
                issues.Add(DayCloseValidationIssue.Create(
                    period.Id,
                    "EMPLOYEE_HOURS_INCOMPLETE_EXCEPTION",
                    "employee_hours",
                    $"{employee.EmployeeName}: worked {workedH}h of {requiredH}h required ({remainingH}h remaining) — closing under approved exception.",
                    isBlocking: false,
                    "employee",
                    employee.EmployeeId));
            }
            else
            {
                var needsConfirm = settings.AllowIncompleteEmployeeHoursException;
                issues.Add(DayCloseValidationIssue.Create(
                    period.Id,
                    needsConfirm ? "EMPLOYEE_HOURS_NEED_EXCEPTION" : "EMPLOYEE_HOURS_INCOMPLETE",
                    "employee_hours",
                    needsConfirm
                        ? $"{employee.EmployeeName}: worked {workedH}h of {requiredH}h ({remainingH}h remaining). Supervisor exception required to close."
                        : $"{employee.EmployeeName}: worked {workedH}h of {requiredH}h required ({remainingH}h remaining). Employee day is incomplete.",
                    isBlocking: true,
                    "employee",
                    employee.EmployeeId));
            }
        }

        var withOt = picture.Employees.Where(e => e.OvertimeMinutes > 0).ToList();
        if (withOt.Count > 0)
        {
            var totalOtH = (withOt.Sum(e => e.OvertimeMinutes) / 60d).ToString("0.0");
            var names = string.Join(", ", withOt.Select(e => e.EmployeeName));
            if (settings.RequireOvertimeApproval && options?.OvertimeApproved != true)
            {
                issues.Add(DayCloseValidationIssue.Create(
                    period.Id,
                    "EMPLOYEE_OT_NEED_APPROVAL",
                    "employee_hours",
                    $"{withOt.Count} employee(s) have {totalOtH}h overtime ({names}). Approve overtime before Day Close.",
                    isBlocking: true));
            }
            else
            {
                issues.Add(DayCloseValidationIssue.Create(
                    period.Id,
                    options?.OvertimeApproved == true ? "EMPLOYEE_OT_APPROVED" : "EMPLOYEE_OT_RECORDED",
                    "employee_hours",
                    options?.OvertimeApproved == true
                        ? $"Overtime approved: {totalOtH}h across {withOt.Count} employee(s) ({names})."
                        : $"Overtime recorded: {totalOtH}h across {withOt.Count} employee(s) ({names}).",
                    isBlocking: false));
            }
        }

        return issues;
    }

    private async Task<IReadOnlyList<MonthlyCloseValidationIssue>> PersistMonthValidationsAsync(
        MonthlyPeriod period,
        MonthOperationalPicture picture,
        CancellationToken cancellationToken)
    {
        var existing = await _monthValidations.Query()
            .Where(v => v.MonthlyPeriodId == period.Id)
            .ToListAsync(cancellationToken);
        foreach (var item in existing)
            _monthValidations.Remove(item);

        var settings = await EnsureSettingsAsync(period.BranchId, cancellationToken);
        var prefix = $"{period.Year}-{period.Month:D2}";
        var dayPeriods = await _dayPeriods.Query().AsNoTracking()
            .Where(p => p.BranchId == period.BranchId && p.BusinessDate.StartsWith(prefix))
            .ToListAsync(cancellationToken);
        var issues = BuildMonthIssues(period, settings, dayPeriods, picture);
        foreach (var issue in issues)
            await _monthValidations.AddAsync(issue, cancellationToken);

        return issues;
    }

    private static List<MonthlyCloseValidationIssue> BuildMonthIssues(
        MonthlyPeriod period,
        PeriodCloseSettings settings,
        IReadOnlyList<BusinessPeriod> dayPeriods,
        MonthOperationalPicture picture)
    {
        var issues = new List<MonthlyCloseValidationIssue>();
        if (settings.RequireAllDaysClosedForMonthlyClose)
        {
            var requiredDates = BusinessDatesInMonth(period.Year, period.Month);
            var byDate = dayPeriods.ToDictionary(p => p.BusinessDate, p => p);
            var missingOrOpen = requiredDates
                .Where(date => !byDate.TryGetValue(date, out var day) || day.Status != PeriodStatuses.Closed)
                .ToList();
            if (missingOrOpen.Count > 0)
            {
                issues.Add(MonthlyCloseValidationIssue.Create(
                    period.Id,
                    "DAYS_NOT_CLOSED",
                    "period",
                    $"{missingOrOpen.Count} business day(s) in this month are not closed.",
                    isBlocking: true));
            }
        }

        foreach (var issue in picture.Issues)
        {
            issues.Add(MonthlyCloseValidationIssue.Create(
                period.Id,
                issue.Code,
                issue.Type,
                issue.Message,
                issue.IsBlocking,
                issue.EntityType,
                issue.EntityId));
        }

        return issues;
    }

    private async Task<PeriodCloseSettings> EnsureSettingsAsync(string branchId, CancellationToken cancellationToken)
    {
        var settings = await _settings.Query()
            .FirstOrDefaultAsync(s => s.BranchId == branchId, cancellationToken);
        if (settings is not null)
            return settings;

        settings = PeriodCloseSettings.CreateDefault(branchId);
        await _settings.AddAsync(settings, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return settings;
    }

    private async Task<BusinessPeriod> EnsureCurrentDayAsync(
        string branchId,
        string userId,
        string userName,
        CancellationToken cancellationToken)
    {
        var open = await _dayPeriods.Query()
            .Where(p => p.BranchId == branchId && PeriodStatuses.IsWritable(p.Status))
            .OrderByDescending(p => p.BusinessDate)
            .FirstOrDefaultAsync(cancellationToken);
        if (open is not null)
            return open;

        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var latest = await _dayPeriods.Query()
            .Where(p => p.BranchId == branchId)
            .OrderByDescending(p => p.BusinessDate)
            .FirstOrDefaultAsync(cancellationToken);
        var date = latest is null ? today : AddBusinessDays(latest.BusinessDate, 1);
        if (string.Compare(date, today, StringComparison.Ordinal) < 0)
            date = today;

        var existing = await _dayPeriods.Query()
            .FirstOrDefaultAsync(p => p.BranchId == branchId && p.BusinessDate == date, cancellationToken);
        if (existing is not null && PeriodStatuses.IsWritable(existing.Status))
            return existing;
        if (existing is not null)
            date = AddBusinessDays(existing.BusinessDate, 1);

        var period = BusinessPeriod.Open(branchId, date, userId, userName);
        await _dayPeriods.AddAsync(period, cancellationToken);
        await AddAuditAsync(PeriodTypes.Day, period.Id, "opened", userId, userName, cancellationToken: cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return period;
    }

    private async Task<MonthlyPeriod> EnsureCurrentMonthAsync(
        string branchId,
        string userId,
        string userName,
        CancellationToken cancellationToken)
    {
        var open = await _monthPeriods.Query()
            .Where(p => p.BranchId == branchId && PeriodStatuses.IsWritable(p.Status))
            .OrderByDescending(p => p.Year)
            .ThenByDescending(p => p.Month)
            .FirstOrDefaultAsync(cancellationToken);
        if (open is not null)
            return open;

        var now = DateTime.UtcNow;
        var latest = await _monthPeriods.Query()
            .Where(p => p.BranchId == branchId)
            .OrderByDescending(p => p.Year)
            .ThenByDescending(p => p.Month)
            .FirstOrDefaultAsync(cancellationToken);
        var (year, month) = latest is null
            ? (now.Year, now.Month)
            : NextCalendarMonth(latest.Year, latest.Month);
        if (year < now.Year || (year == now.Year && month < now.Month))
            (year, month) = (now.Year, now.Month);

        var existing = await _monthPeriods.Query()
            .FirstOrDefaultAsync(
                p => p.BranchId == branchId && p.Year == year && p.Month == month,
                cancellationToken);
        if (existing is not null && PeriodStatuses.IsWritable(existing.Status))
            return existing;
        if (existing is not null)
            (year, month) = NextCalendarMonth(existing.Year, existing.Month);

        var period = MonthlyPeriod.Open(branchId, year, month, userId, userName);
        await _monthPeriods.AddAsync(period, cancellationToken);
        await AddAuditAsync(PeriodTypes.Month, period.Id, "opened", userId, userName, cancellationToken: cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return period;
    }

    private async Task<BusinessPeriod> RequireDayAsync(
        Guid periodId,
        CancellationToken cancellationToken,
        bool track = false)
    {
        var query = track ? _dayPeriods.Query() : _dayPeriods.Query().AsNoTracking();
        return await query.FirstOrDefaultAsync(p => p.Id == periodId, cancellationToken)
            ?? throw new NotFoundException($"Business period '{periodId}' was not found.");
    }

    private async Task<MonthlyPeriod> RequireMonthAsync(
        Guid periodId,
        CancellationToken cancellationToken,
        bool track = false)
    {
        var query = track ? _monthPeriods.Query() : _monthPeriods.Query().AsNoTracking();
        return await query.FirstOrDefaultAsync(p => p.Id == periodId, cancellationToken)
            ?? throw new NotFoundException($"Monthly period '{periodId}' was not found.");
    }

    private async Task AddAuditAsync(
        string periodType,
        Guid periodId,
        string action,
        string userId,
        string userName,
        string? reason = null,
        string? detailsJson = null,
        CancellationToken cancellationToken = default)
    {
        await _auditLogs.AddAsync(
            PeriodAuditLog.Create(periodType, periodId, action, userId, userName, reason, detailsJson),
            cancellationToken);
    }

    private static (string UserId, string UserName) ResolveActor(string? userId, string? userName) =>
        (
            string.IsNullOrWhiteSpace(userId) ? PeriodCloseDefaults.SystemUserId : userId.Trim(),
            string.IsNullOrWhiteSpace(userName) ? PeriodCloseDefaults.SystemUserName : userName.Trim());

    private static string NormalizeBranch(string? branchId) =>
        string.IsNullOrWhiteSpace(branchId)
            ? PeriodCloseDefaults.DefaultBranchId
            : branchId.Trim();

    private static (int Year, int Month, int Day) ParseBusinessDate(string businessDate)
    {
        var parts = businessDate.Split('-');
        if (parts.Length != 3
            || !int.TryParse(parts[0], out var year)
            || !int.TryParse(parts[1], out var month)
            || !int.TryParse(parts[2], out var day))
        {
            throw new ApplicationValidationException(
            [
                new ValidationError(nameof(businessDate), "Business date must be yyyy-MM-dd.", ValidationErrorCodes.Validation),
            ]);
        }

        return (year, month, day);
    }

    private static string AddBusinessDays(string businessDate, int days)
    {
        var (year, month, day) = ParseBusinessDate(businessDate);
        var date = new DateOnly(year, month, day).AddDays(days);
        return date.ToString("yyyy-MM-dd");
    }

    private static (int Year, int Month) NextCalendarMonth(int year, int month) =>
        month >= 12 ? (year + 1, 1) : (year, month + 1);

    private static IReadOnlyList<string> BusinessDatesInMonth(int year, int month)
    {
        var days = DateTime.DaysInMonth(year, month);
        return Enumerable.Range(1, days)
            .Select(day => $"{year}-{month:D2}-{day:D2}")
            .ToList();
    }

    private static DailyTransactionRefsDto EmptyDailyRefs() =>
        new([], [], [], [], [], []);

    private static MonthlyTransactionRefsDto EmptyMonthlyRefs() =>
        new([], [], [], [], [], []);

    private static PeriodClosePolicy ToPolicy(PeriodCloseSettings settings) =>
        new(
            settings.RequiredDailyWorkMinutes,
            settings.CountPauseAsWorked,
            settings.DoubleOvertimeAfterMinutes,
            500m);

    private async Task ReplaceDailySnapshotsAsync(
        BusinessPeriod period,
        DayOperationalPicture picture,
        IReadOnlyList<SessionCheckpointSeed> checkpoints,
        DateTimeOffset recordedAt,
        CancellationToken cancellationToken)
    {
        var existingProduction = await _productionDaily.Query()
            .Where(s => s.BusinessPeriodId == period.Id)
            .ToListAsync(cancellationToken);
        foreach (var item in existingProduction)
            _productionDaily.Remove(item);
        foreach (var row in picture.Production)
        {
            await _productionDaily.AddAsync(ProductionDailySnapshot.Capture(
                period.Id,
                period.BusinessDate,
                row.ProductionOrderId,
                row.ProductionOrderNumber,
                row.OperationId,
                row.OperationName,
                row.WorkerId,
                row.WorkerName,
                row.TotalQty,
                row.CompletedQty,
                row.PartialQty,
                row.ProgressPercentage,
                row.WorkedMinutes,
                row.ProducedQty,
                row.RejectedQty,
                row.JobStatus,
                row.TaskStatus,
                recordedAt), cancellationToken);
        }

        var existingInventory = await _inventoryDaily.Query()
            .Where(s => s.BusinessPeriodId == period.Id)
            .ToListAsync(cancellationToken);
        foreach (var item in existingInventory)
            _inventoryDaily.Remove(item);
        foreach (var row in picture.Inventory)
        {
            await _inventoryDaily.AddAsync(InventoryDailySnapshot.Capture(
                period.Id,
                period.BusinessDate,
                row.InventoryItemId,
                row.Sku,
                row.Name,
                row.Unit,
                row.OpeningQty,
                row.Receipts,
                row.Returns,
                row.ProductionOutput,
                row.Issues,
                row.Consumption,
                row.Deliveries,
                row.Adjustments,
                row.ClosingQty,
                JsonColumn.Serialize(row.MovementIds),
                recordedAt), cancellationToken);
        }

        var existingCheckpoints = await _sessionCheckpoints.Query()
            .Where(s => s.BusinessPeriodId == period.Id)
            .ToListAsync(cancellationToken);
        foreach (var item in existingCheckpoints)
            _sessionCheckpoints.Remove(item);
        foreach (var seed in checkpoints)
        {
            await _sessionCheckpoints.AddAsync(WorkerSessionCheckpoint.Create(
                period.Id,
                period.BusinessDate,
                seed.SessionId,
                seed.WorkerId,
                seed.WorkerName,
                seed.ProductionOrderId,
                seed.OperationId,
                seed.OperationName,
                seed.ProgressPercentage,
                seed.StartedAt,
                recordedAt,
                WorkerSessionCloseRules.PauseAndCheckpoint,
                seed.Status), cancellationToken);
        }
    }

    private async Task ReplaceMonthlySnapshotsAsync(
        MonthlyPeriod period,
        MonthOperationalPicture picture,
        DateTimeOffset recordedAt,
        CancellationToken cancellationToken)
    {
        var existingProduction = await _productionMonthly.Query()
            .Where(s => s.MonthlyPeriodId == period.Id)
            .ToListAsync(cancellationToken);
        foreach (var item in existingProduction)
            _productionMonthly.Remove(item);
        foreach (var row in picture.Production)
        {
            await _productionMonthly.AddAsync(ProductionMonthlySnapshot.Capture(
                period.Id,
                period.Year,
                period.Month,
                row.ProductionOrderId,
                row.ProductionOrderNumber,
                row.OperationId,
                row.OperationName,
                row.TotalQty,
                row.CompletedQty,
                row.WorkInProgressQty,
                row.ProgressPercentage,
                row.MaterialConsumed,
                row.LaborHours,
                row.EstimatedCost,
                row.ActualCostToDate,
                row.WipCost,
                recordedAt), cancellationToken);
        }

        var existingInventory = await _inventoryMonthly.Query()
            .Where(s => s.MonthlyPeriodId == period.Id)
            .ToListAsync(cancellationToken);
        foreach (var item in existingInventory)
            _inventoryMonthly.Remove(item);
        foreach (var row in picture.Inventory)
        {
            await _inventoryMonthly.AddAsync(InventoryMonthlySnapshot.Capture(
                period.Id,
                period.Year,
                period.Month,
                row.InventoryItemId,
                row.Sku,
                row.Name,
                row.Unit,
                row.OpeningQty,
                row.OpeningValue,
                row.ReceivedQty,
                row.ReceivedValue,
                row.ConsumedQty,
                row.ConsumedValue,
                row.AdjustmentQty,
                row.AdjustmentValue,
                row.ClosingQty,
                row.ClosingValue,
                recordedAt), cancellationToken);
        }
    }

    private static WorkerSessionCheckpointDto MapCheckpointSeed(
        Guid periodId,
        string businessDate,
        SessionCheckpointSeed seed,
        string rule,
        DateTimeOffset checkpointAt) =>
        new(
            Guid.NewGuid(),
            periodId,
            businessDate,
            seed.SessionId,
            seed.WorkerId,
            seed.WorkerName,
            seed.ProductionOrderId,
            seed.OperationId,
            seed.OperationName,
            seed.ProgressPercentage,
            seed.StartedAt,
            checkpointAt,
            rule,
            null,
            seed.Status);

    private static PeriodCloseSettingsDto MapSettings(PeriodCloseSettings s) =>
        new(
            s.BranchId,
            s.WorkerSessionCloseRule,
            s.AllowNegativeStock,
            s.RequireAllDaysClosedForMonthlyClose,
            s.FiscalYearStartMonth,
            s.RequiredDailyWorkMinutes,
            s.AllowIncompleteEmployeeHoursException,
            s.CountPauseAsWorked,
            s.DoubleOvertimeAfterMinutes,
            s.RequireOvertimeApproval);

    private static BusinessPeriodDto MapDay(BusinessPeriod p) =>
        new(
            p.Id,
            p.BranchId,
            p.BusinessDate,
            p.Status,
            p.OpenedAt,
            p.OpenedBy,
            p.OpenedByName,
            p.ClosedAt,
            p.ClosedBy,
            p.ClosedByName,
            p.ReopenedAt,
            p.ReopenedBy,
            p.ReopenedByName,
            p.ReopenReason,
            p.OriginalClosedAt,
            p.OriginalClosedBy,
            p.OriginalClosedByName,
            p.CloseCount);

    private static MonthlyPeriodDto MapMonth(MonthlyPeriod p) =>
        new(
            p.Id,
            p.BranchId,
            p.Year,
            p.Month,
            p.Status,
            p.StartedAt,
            p.StartedBy,
            p.StartedByName,
            p.ClosedAt,
            p.ClosedBy,
            p.ClosedByName,
            p.ReopenedAt,
            p.ReopenedBy,
            p.ReopenedByName,
            p.ReopenReason,
            p.OriginalClosedAt,
            p.OriginalClosedBy,
            p.OriginalClosedByName,
            p.CloseCount);

    private static DayCloseValidationIssueDto MapDayIssue(DayCloseValidationIssue i) =>
        new(i.Id, i.BusinessPeriodId, i.ValidationCode, i.ValidationType, i.Message, i.EntityType, i.EntityId, i.IsBlocking);

    private static MonthlyCloseValidationIssueDto MapMonthIssue(MonthlyCloseValidationIssue i) =>
        new(i.Id, i.MonthlyPeriodId, i.ValidationCode, i.ValidationType, i.Message, i.EntityType, i.EntityId, i.IsBlocking);

    private static DailyClosingSummaryDto MapDailySummary(DailyClosingSummary s) =>
        new(
            s.Id,
            s.BusinessPeriodId,
            s.BusinessDate,
            s.BranchId,
            s.OrdersCreated,
            s.ProductionJobs,
            s.CompletedProductionQty,
            s.PartialProductionQty,
            s.Invoices,
            s.InvoiceTotal,
            s.Payments,
            s.PaymentTotal,
            s.Deliveries,
            s.MaterialIssues,
            s.MaterialReturns,
            s.InventoryMovementCount,
            s.QuotationValue,
            s.SalesOrderValue,
            s.CreditNoteTotal,
            s.CashPayments,
            s.CardPayments,
            s.BankPayments,
            s.AdvancePayments,
            s.Refunds,
            s.OpeningReceivable,
            s.ClosingReceivable,
            s.OutstandingAmount,
            JsonColumn.Deserialize(s.TransactionRefsJson, EmptyDailyRefs()),
            s.CreatedAt);

    private static MonthlyClosingSummaryDto MapMonthlySummary(MonthlyClosingSummary s) =>
        new(
            s.Id,
            s.MonthlyPeriodId,
            s.Year,
            s.Month,
            s.BranchId,
            s.SalesTotal,
            s.PurchaseTotal,
            s.PaymentTotal,
            s.ExpenseTotal,
            s.InventoryValue,
            s.WipValue,
            s.CostOfGoodsSold,
            s.GrossProfit,
            s.RawMaterials,
            s.Labour,
            s.Production,
            s.Waste,
            s.ReusableWaste,
            s.Overhead,
            s.CreditNotes,
            s.NetMargin,
            JsonColumn.Deserialize(s.TransactionRefsJson, EmptyMonthlyRefs()),
            s.CreatedAt);

    private static ProductionDailySnapshotDto MapProductionDaily(ProductionDailySnapshot s) =>
        new(
            s.Id,
            s.BusinessPeriodId,
            s.BusinessDate,
            s.ProductionOrderId,
            s.ProductionOrderNumber,
            s.OperationId,
            s.OperationName,
            s.WorkerId,
            s.WorkerName,
            s.TotalQty,
            s.CompletedQty,
            s.PartialQty,
            s.ProgressPercentage,
            s.WorkedMinutes,
            s.ProducedQty,
            s.RejectedQty,
            s.JobStatus,
            s.TaskStatus,
            s.RecordedAt);

    private static ProductionMonthlySnapshotDto MapProductionMonthly(ProductionMonthlySnapshot s) =>
        new(
            s.Id,
            s.MonthlyPeriodId,
            s.Year,
            s.Month,
            s.ProductionOrderId,
            s.ProductionOrderNumber,
            s.OperationId,
            s.OperationName,
            s.TotalQty,
            s.CompletedQty,
            s.WorkInProgressQty,
            s.ProgressPercentage,
            s.MaterialConsumed,
            s.LaborHours,
            s.EstimatedCost,
            s.ActualCostToDate,
            s.WipCost,
            s.RecordedAt);

    private static InventoryDailySnapshotDto MapInventoryDaily(InventoryDailySnapshot s) =>
        new(
            s.Id,
            s.BusinessPeriodId,
            s.BusinessDate,
            s.InventoryItemId,
            s.Sku,
            s.Name,
            s.Unit,
            s.OpeningQty,
            s.Receipts,
            s.Returns,
            s.ProductionOutput,
            s.Issues,
            s.Consumption,
            s.Deliveries,
            s.Adjustments,
            s.ClosingQty,
            JsonColumn.Deserialize(s.MovementIdsJson, Array.Empty<string>()),
            s.RecordedAt);

    private static InventoryMonthlySnapshotDto MapInventoryMonthly(InventoryMonthlySnapshot s) =>
        new(
            s.Id,
            s.MonthlyPeriodId,
            s.Year,
            s.Month,
            s.InventoryItemId,
            s.Sku,
            s.Name,
            s.Unit,
            s.OpeningQty,
            s.OpeningValue,
            s.ReceivedQty,
            s.ReceivedValue,
            s.ConsumedQty,
            s.ConsumedValue,
            s.AdjustmentQty,
            s.AdjustmentValue,
            s.ClosingQty,
            s.ClosingValue,
            s.RecordedAt);

    private static WorkerSessionCheckpointDto MapCheckpoint(WorkerSessionCheckpoint s) =>
        new(
            s.Id,
            s.BusinessPeriodId,
            s.BusinessDate,
            s.SessionId,
            s.WorkerId,
            s.WorkerName,
            s.ProductionOrderId,
            s.OperationId,
            s.OperationName,
            s.ProgressPercentage,
            s.StartedAt,
            s.CheckpointAt,
            s.RuleApplied,
            s.ResumedAt,
            s.Status);

    private static PeriodAuditLogDto MapAudit(PeriodAuditLog a) =>
        new(
            a.Id,
            a.PeriodType,
            a.PeriodId,
            a.Action,
            a.UserId,
            a.UserName,
            a.Reason,
            string.IsNullOrWhiteSpace(a.DetailsJson)
                ? null
                : JsonColumn.Deserialize<Dictionary<string, object?>>(a.DetailsJson, new Dictionary<string, object?>()),
            a.PerformedAt);

    private static PeriodAdjustmentDto MapAdjustment(PeriodAdjustment a) =>
        new(
            a.Id,
            a.BranchId,
            a.PostingBusinessDate,
            a.PostingMonthlyPeriodId,
            a.EntityType,
            a.EntityId,
            a.OriginalBusinessDate,
            a.OriginalTransactionId,
            a.AdjustmentType,
            a.QuantityDelta,
            a.AmountDelta,
            a.Reason,
            a.CreatedBy,
            a.CreatedByName,
            a.CreatedAt);
}
