using Libr4.Payments.Api.Endpoints;
using Libr4.Payments.Application;
using Libr4.Payments.Application.Tax;
using Libr4.Payments.Infrastructure;
using Libr4.Shared.Infrastructure.Observability;
using Libr4.Shared.Web.Auth;
using Libr4.Shared.Web.CurrentUser;
using Libr4.Shared.Web.HealthChecks;
using Libr4.Shared.Web.Logging;
using Libr4.Shared.Web.Middleware;
using Libr4.Shared.Web.Swagger;

var builder = WebApplication.CreateBuilder(args);

builder.AddLibr4Serilog("payments");
builder.Services.AddLibr4JwtAuth(builder.Configuration);
builder.Services.AddLibr4CurrentUser();
builder.Services.AddLibr4Telemetry("payments");
builder.Services.AddLibr4Swagger("Libr4 Payments API", includeSecurity: true);

// Application & Infrastructure
builder.Services.AddPaymentsApplication();
builder.Services.AddPaymentsInfrastructure(builder.Configuration);

// Tax calculation service with F# Units of Measure
builder.Services.AddScoped<ITaxCalculationService, TaxCalculationService>();

var app = builder.Build();

// Middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();
app.UseExceptionHandler("/error");
app.UseMiddleware<CorrelationIdMiddleware>();

app.MapLibr4HealthChecks();
app.MapLibr4Metrics();

// Map endpoints
app.MapPaymentEndpoints();
app.MapEscrowEndpoints();
app.MapWalletEndpoints();

app.Run();
