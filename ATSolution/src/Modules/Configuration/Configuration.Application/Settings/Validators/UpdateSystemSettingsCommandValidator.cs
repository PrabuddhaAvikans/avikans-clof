using Configuration.Application.Settings;
using FluentValidation;

namespace Configuration.Application.Settings.Validators;

public sealed class UpdateSystemSettingsCommandValidator : AbstractValidator<UpdateSystemSettingsCommand>
{
    public UpdateSystemSettingsCommandValidator()
    {
        RuleFor(x => x.CompanyName).MaximumLength(200).When(x => x.CompanyName is not null);
        RuleFor(x => x.Tagline).MaximumLength(300).When(x => x.Tagline is not null);
        RuleFor(x => x.Email).EmailAddress().MaximumLength(256).When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Phone).MaximumLength(50).When(x => x.Phone is not null);
        RuleFor(x => x.Website).MaximumLength(300).When(x => x.Website is not null);
        RuleFor(x => x.Address).MaximumLength(500).When(x => x.Address is not null);
        RuleFor(x => x.TaxRegistration).MaximumLength(100).When(x => x.TaxRegistration is not null);
        RuleFor(x => x.LogoUrl).MaximumLength(1000).When(x => x.LogoUrl is not null);
        RuleFor(x => x.AppSubtitle).MaximumLength(300).When(x => x.AppSubtitle is not null);
        RuleFor(x => x.Country).MaximumLength(10).When(x => x.Country is not null);
        RuleFor(x => x.TaxRate).InclusiveBetween(0, 100).When(x => x.TaxRate.HasValue);
        RuleFor(x => x.QuotationValidityDays).GreaterThan(0).When(x => x.QuotationValidityDays.HasValue);
        RuleFor(x => x.PaymentTermsDays).GreaterThanOrEqualTo(0).When(x => x.PaymentTermsDays.HasValue);
        RuleFor(x => x.PaymentTerms).MaximumLength(500).When(x => x.PaymentTerms is not null);
        RuleFor(x => x.QuotationPrefix).MaximumLength(20).When(x => x.QuotationPrefix is not null);
        RuleFor(x => x.SalesOrderPrefix).MaximumLength(20).When(x => x.SalesOrderPrefix is not null);
        RuleFor(x => x.JobPrefix).MaximumLength(20).When(x => x.JobPrefix is not null);
        RuleFor(x => x.DeliveryPrefix).MaximumLength(20).When(x => x.DeliveryPrefix is not null);
        RuleFor(x => x.MaxConcurrentTasks).GreaterThanOrEqualTo(1).When(x => x.MaxConcurrentTasks.HasValue);
    }
}
