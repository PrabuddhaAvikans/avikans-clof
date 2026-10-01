using System.Text.Json;
using ATSolution.SharedKernel.Models;

namespace Sales.Application.SalesOrders;

public sealed record CancelSalesOrderCommand(Guid Id, string? Reason);
