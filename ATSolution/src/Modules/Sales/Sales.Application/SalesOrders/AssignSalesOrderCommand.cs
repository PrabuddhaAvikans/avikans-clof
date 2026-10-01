using System.Text.Json;
using ATSolution.SharedKernel.Models;

namespace Sales.Application.SalesOrders;

public sealed record AssignSalesOrderCommand(Guid Id, Guid UserId);
