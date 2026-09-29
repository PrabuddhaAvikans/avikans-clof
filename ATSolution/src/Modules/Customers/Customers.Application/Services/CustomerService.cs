using System.Text.Json;
using System.Text.Json.Serialization;
using ATSolution.Application;
using ATSolution.Application.Abstractions.Persistence;
using ATSolution.Application.Abstractions.Validation;
using ATSolution.Application.Exceptions;
using ATSolution.SharedKernel.Constants;
using ATSolution.SharedKernel.Models;
using Customers.Application.Abstractions;
using Customers.Application.Customers;
using Customers.Domain.Customers;
using Microsoft.EntityFrameworkCore;

namespace Customers.Application.Services;

public sealed class CustomerService : ICustomerService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly IRepository<Customer, Guid> _customers;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IApplicationValidator _validator;

    public CustomerService(
        IRepository<Customer, Guid> customers,
        IUnitOfWork unitOfWork,
        IApplicationValidator validator)
    {
        _customers = customers;
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    public async Task<PaginatedResponse<CustomerDto>> ListAsync(
        CustomerListQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Max(1, query.PageSize);
        var customers = _customers.Query().AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Type))
            customers = customers.Where(c => c.Type == query.Type);
        if (!string.IsNullOrWhiteSpace(query.Status))
            customers = customers.Where(c => c.Status == query.Status);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            customers = customers.Where(c =>
                c.Name.Contains(search)
                || c.Code.Contains(search)
                || c.Email.Contains(search)
                || c.Phone.Contains(search));
        }

        customers = customers.OrderBy(c => c.Name);
        var totalCount = await customers.CountAsync(cancellationToken);
        var items = await customers.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return PaginatedResponse<CustomerDto>.Create(items.Select(Map).ToList(), totalCount, page, pageSize);
    }

    public async Task<CustomerDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var customer = await _customers.Query().AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        return customer is null ? null : Map(customer);
    }

    public async Task<CustomerDto> CreateAsync(
        CreateCustomerCommand command,
        CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAsync(command, cancellationToken);

        var code = string.IsNullOrWhiteSpace(command.Code)
            ? await GenerateCustomerCodeAsync(cancellationToken)
            : command.Code.Trim();

        var codeExists = await _customers.Query().AnyAsync(c => c.Code == code, cancellationToken);
        if (codeExists)
        {
            throw new ApplicationValidationException(
            [
                new ValidationError(nameof(command.Code), "Customer code already exists.", ValidationErrorCodes.Conflict),
            ]);
        }

        var contacts = NormalizeContacts(command.ContactPersons);
        var customer = Customer.Create(
            code,
            command.Name,
            command.Type,
            command.Email,
            command.Phone,
            JsonSerializer.Serialize(command.BillingAddresses, JsonOptions),
            command.ActiveBillingAddressIndex,
            command.DeliverySameAsBilling,
            command.ShippingAddresses is null ? null : JsonSerializer.Serialize(command.ShippingAddresses, JsonOptions),
            command.ActiveShippingAddressIndex,
            JsonSerializer.Serialize(contacts, JsonOptions),
            command.TaxId,
            command.CreditLimit,
            command.PaymentTermsDays,
            command.Notes,
            command.Status);

        await _customers.AddAsync(customer, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(customer);
    }

    public async Task<CustomerDto> UpdateAsync(
        UpdateCustomerCommand command,
        CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAsync(command, cancellationToken);

        var customer = await _customers.Query()
            .FirstOrDefaultAsync(c => c.Id == command.Id, cancellationToken)
            ?? throw new NotFoundException($"Customer '{command.Id}' was not found.");

        if (command.Code is not null)
        {
            var codeExists = await _customers.Query()
                .AnyAsync(c => c.Code == command.Code && c.Id != command.Id, cancellationToken);
            if (codeExists)
            {
                throw new ApplicationValidationException(
                [
                    new ValidationError(nameof(command.Code), "Customer code already exists.", ValidationErrorCodes.Conflict),
                ]);
            }
        }

        customer.Update(
            command.Code,
            command.Name,
            command.Type,
            command.Email,
            command.Phone,
            command.BillingAddresses is null ? null : JsonSerializer.Serialize(command.BillingAddresses, JsonOptions),
            command.ActiveBillingAddressIndex,
            command.DeliverySameAsBilling,
            command.ShippingAddresses is null ? null : JsonSerializer.Serialize(command.ShippingAddresses, JsonOptions),
            command.ActiveShippingAddressIndex,
            command.ContactPersons is null ? null : JsonSerializer.Serialize(NormalizeContacts(command.ContactPersons), JsonOptions),
            command.TaxId,
            command.CreditLimit,
            command.PaymentTermsDays,
            command.Notes,
            command.Status);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(customer);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var customer = await _customers.Query()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Customer '{id}' was not found.");

        customer.Deactivate();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static IReadOnlyList<ContactPersonDto> NormalizeContacts(IReadOnlyList<ContactPersonInputDto> contacts) =>
        contacts.Select(c => new ContactPersonDto(
            c.Id ?? Guid.NewGuid(),
            c.Name,
            c.Title,
            c.Email,
            c.Phone,
            c.IsPrimary)).ToList();

    private async Task<string> GenerateCustomerCodeAsync(CancellationToken cancellationToken)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"CUS-{year}-";
        var existing = await _customers.Query()
            .AsNoTracking()
            .Where(c => c.Code.StartsWith(prefix))
            .Select(c => c.Code)
            .ToListAsync(cancellationToken);

        var next = existing
            .Select(code => int.TryParse(code.AsSpan(prefix.Length), out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max() + 1;

        return $"{prefix}{next:D4}";
    }

    private static CustomerDto Map(Customer customer)
    {
        var billing = Deserialize(customer.BillingAddressesJson, Array.Empty<AddressDto>());
        var shipping = string.IsNullOrWhiteSpace(customer.ShippingAddressesJson)
            ? null
            : Deserialize(customer.ShippingAddressesJson, Array.Empty<AddressDto>());
        var contacts = Deserialize(customer.ContactPersonsJson, Array.Empty<ContactPersonDto>());

        return new CustomerDto(
            customer.Id,
            customer.Code,
            customer.Name,
            customer.Type,
            customer.Email,
            customer.Phone,
            billing,
            customer.ActiveBillingAddressIndex,
            customer.DeliverySameAsBilling,
            shipping,
            customer.ActiveShippingAddressIndex,
            contacts,
            customer.TaxId,
            customer.CreditLimit,
            customer.PaymentTermsDays,
            customer.Notes,
            customer.Status,
            customer.TotalOrders,
            customer.TotalRevenue,
            customer.CreatedOnUtc,
            customer.ModifiedOnUtc);
    }

    private static T Deserialize<T>(string? json, T fallback)
    {
        if (string.IsNullOrWhiteSpace(json)) return fallback;
        try
        {
            return JsonSerializer.Deserialize<T>(json, JsonOptions) ?? fallback;
        }
        catch
        {
            return fallback;
        }
    }
}
