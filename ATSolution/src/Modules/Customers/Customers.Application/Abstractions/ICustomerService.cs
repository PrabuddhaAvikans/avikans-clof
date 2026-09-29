using ATSolution.SharedKernel.Models;
using Customers.Application.Customers;

namespace Customers.Application.Abstractions;

public interface ICustomerService
{
    Task<PaginatedResponse<CustomerDto>> ListAsync(CustomerListQuery query, CancellationToken cancellationToken = default);
    Task<CustomerDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CustomerDto> CreateAsync(CreateCustomerCommand command, CancellationToken cancellationToken = default);
    Task<CustomerDto> UpdateAsync(UpdateCustomerCommand command, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
