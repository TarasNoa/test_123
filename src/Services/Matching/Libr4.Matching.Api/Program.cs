using Libr4.Matching.Application;
using Libr4.Matching.Infrastructure;
using Libr4.Matching.Api.Endpoints;

AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMatchingApplication();
builder.Services.AddMatchingInfrastructure(builder.Configuration);

builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapMatchingEndpoints();
app.MapHealthChecks("/health");

app.Run();
