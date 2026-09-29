using ATSolution.Application.Abstractions.Persistence;
using ATSolution.Application.Abstractions.Validation;
using Configuration.Application.Abstractions;
using Configuration.Application.Settings;
using Configuration.Domain.Settings;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Configuration.Application.Services;

public sealed class SystemSettingsService : ISystemSettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private static readonly SystemSettingsDto Defaults = new(
        CompanyName: "AVIKANS SOLUTION",
        Tagline: "Premium Lighting & Manufacturing",
        Email: "info@avikans.lk",
        Phone: "+94 11 000 0000",
        Website: "www.avikans.lk",
        Address: "Colombo, Sri Lanka",
        TaxRegistration: "VAT 123456789",
        LogoUrl: "",
        AppSubtitle: "Custom Lighting Product Management",
        Country: "LK",
        TaxRate: 18,
        QuotationValidityDays: 30,
        PaymentTermsDays: 30,
        PaymentTerms: "30% advance, 60% on delivery, 10% on installation",
        PricesIncludeTax: false,
        AutoExpireQuotations: true,
        QuotationPrefix: "QT",
        SalesOrderPrefix: "SO",
        JobPrefix: "PJ",
        DeliveryPrefix: "DL",
        AllowConcurrentWork: false,
        MaxConcurrentTasks: 1);

    private readonly IRepository<SystemSettingsRecord, Guid> _settings;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IApplicationValidator _validator;

    public SystemSettingsService(
        IRepository<SystemSettingsRecord, Guid> settings,
        IUnitOfWork unitOfWork,
        IApplicationValidator validator)
    {
        _settings = settings;
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    public async Task<SystemSettingsDto> GetAsync(CancellationToken cancellationToken = default)
    {
        var record = await _settings.Query().AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        return record is null ? Defaults : Deserialize(record.SettingsJson);
    }

    public async Task<SystemSettingsDto> UpdateAsync(
        UpdateSystemSettingsCommand command,
        CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAsync(command, cancellationToken);

        var record = await _settings.Query().FirstOrDefaultAsync(cancellationToken);
        var current = record is null ? Defaults : Deserialize(record.SettingsJson);
        var merged = Merge(current, command);
        var json = JsonSerializer.Serialize(merged, JsonOptions);

        if (record is null)
        {
            record = SystemSettingsRecord.Create(json);
            await _settings.AddAsync(record, cancellationToken);
        }
        else
        {
            record.UpdateSettings(json);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return merged;
    }

    private static SystemSettingsDto Merge(SystemSettingsDto current, UpdateSystemSettingsCommand command) =>
        current with
        {
            CompanyName = command.CompanyName ?? current.CompanyName,
            Tagline = command.Tagline ?? current.Tagline,
            Email = command.Email ?? current.Email,
            Phone = command.Phone ?? current.Phone,
            Website = command.Website ?? current.Website,
            Address = command.Address ?? current.Address,
            TaxRegistration = command.TaxRegistration ?? current.TaxRegistration,
            LogoUrl = command.LogoUrl ?? current.LogoUrl,
            AppSubtitle = command.AppSubtitle ?? current.AppSubtitle,
            Country = command.Country ?? current.Country,
            TaxRate = command.TaxRate ?? current.TaxRate,
            QuotationValidityDays = command.QuotationValidityDays ?? current.QuotationValidityDays,
            PaymentTermsDays = command.PaymentTermsDays ?? current.PaymentTermsDays,
            PaymentTerms = command.PaymentTerms ?? current.PaymentTerms,
            PricesIncludeTax = command.PricesIncludeTax ?? current.PricesIncludeTax,
            AutoExpireQuotations = command.AutoExpireQuotations ?? current.AutoExpireQuotations,
            QuotationPrefix = command.QuotationPrefix ?? current.QuotationPrefix,
            SalesOrderPrefix = command.SalesOrderPrefix ?? current.SalesOrderPrefix,
            JobPrefix = command.JobPrefix ?? current.JobPrefix,
            DeliveryPrefix = command.DeliveryPrefix ?? current.DeliveryPrefix,
            AllowConcurrentWork = command.AllowConcurrentWork ?? current.AllowConcurrentWork,
            MaxConcurrentTasks = command.MaxConcurrentTasks.HasValue
                ? Math.Max(1, command.MaxConcurrentTasks.Value)
                : current.MaxConcurrentTasks,
        };

    private static SystemSettingsDto Deserialize(string json)
    {
        try
        {
            var partial = JsonSerializer.Deserialize<UpdateSystemSettingsCommand>(json, JsonOptions);
            return partial is null ? Defaults : Merge(Defaults, partial);
        }
        catch
        {
            return Defaults;
        }
    }
}
