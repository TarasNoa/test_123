using Libr4.IDE.Application.AutonomousAppGeneration.Api;

namespace Libr4.IDE.AutonomousAppGeneration.Host.Endpoints;

/// <summary>Compatibility shim — endpoints live in <see cref="AutonomousAppGenerationEndpoints"/>.</summary>
public static class AutonomousAppGenerationHostEndpoints
{
    public static void MapAutonomousAppGenerationEndpoints(this IEndpointRouteBuilder app)
    {
        AutonomousAppGenerationEndpoints.MapAutonomousAppGenerationEndpoints(app, "/api/ide/app-generation");
        SessionSearchEndpoints.MapSessionSearchEndpoints(app, "/api/ide/memory");
    }
}
