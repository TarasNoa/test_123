using Libr4.Matching.Application;
using Libr4.Matching.Infrastructure;
using Libr4.Matching.Api.Endpoints;
using Libr4.Shared.Web.Logging;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.AddLibr4Serilog();

builder.Services.AddMatchingApplication();
builder.Services.AddMatchingInfrastructure(builder.Configuration);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHealthChecks();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapMatchingEndpoints();
app.MapHealthChecks("/health");

app.Run();
