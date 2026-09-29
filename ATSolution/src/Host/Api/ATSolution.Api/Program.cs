using ATSolution.Api.Exceptions;
using ATSolution.Api.Extensions;
using ATSolution.Api.Seeding;
using ATSolution.Application;
using ATSolution.Infrastructure;
using ATSolution.SharedKernel.Constants;
using Identity.Api.DTOs.Requests;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddExceptionHandler<GlobalExceptionHandler>()
    .AddProblemDetails();

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddHostModules(builder.Configuration);

// After identity modules so IdentityDataSeeder is registered first.
builder.Services.AddScoped<ICommercialDataSeeder, CommercialMockDataSeeder>();
builder.Services.AddHostedService<CommercialMockDataHostedService>();

builder.Services.AddOpenApi(options =>
{
    options.AddSchemaTransformer((schema, context, cancellationToken) =>
    {
        if (context.JsonTypeInfo.Type == typeof(LoginRequestDto))
        {
            schema.Example = System.Text.Json.Nodes.JsonNode.Parse(
                $$"""{"email":"{{IdentityMessages.AdminEmail}}","password":"{{IdentityMessages.AdminPassword}}"}""");
        }

        return Task.CompletedTask;
    });
});

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("Avikans Lighting API")
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
            .AddPreferredSecuritySchemes("Bearer");
    });
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
