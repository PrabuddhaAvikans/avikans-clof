namespace ATSolution.SharedKernel.Constants;

public static class PeriodClosePersistenceConstants
{
    public const string SchemaName = "period_close";
    public const string BusinessPeriodsTableName = "BusinessPeriods";
    public const string MonthlyPeriodsTableName = "MonthlyPeriods";
    public const string PeriodCloseSettingsTableName = "PeriodCloseSettings";
    public const string DayValidationsTableName = "DayCloseValidations";
    public const string MonthValidationsTableName = "MonthCloseValidations";
    public const string DailySummariesTableName = "DailyClosingSummaries";
    public const string MonthlySummariesTableName = "MonthlyClosingSummaries";
    public const string ProductionDailySnapshotsTableName = "ProductionDailySnapshots";
    public const string ProductionMonthlySnapshotsTableName = "ProductionMonthlySnapshots";
    public const string InventoryDailySnapshotsTableName = "InventoryDailySnapshots";
    public const string InventoryMonthlySnapshotsTableName = "InventoryMonthlySnapshots";
    public const string SessionCheckpointsTableName = "WorkerSessionCheckpoints";
    public const string PeriodAuditLogsTableName = "PeriodAuditLogs";
    public const string PeriodAdjustmentsTableName = "PeriodAdjustments";
}
