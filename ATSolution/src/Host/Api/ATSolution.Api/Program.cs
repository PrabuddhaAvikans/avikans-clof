using ATSolution.Api.Exceptions;
using ATSolution.Api.Extensions;
using ATSolution.Application;
using ATSolution.Infrastructure;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddExceptionHandler<GlobalExceptionHandler>()
    .AddProblemDetails();

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddHostModules(builder.Configuration);

builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
