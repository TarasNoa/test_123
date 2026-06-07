using Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;
using Libr4.IDE.Application.AutonomousAppGeneration.Memory.Hermes;
using Libr4.IDE.Application.AutonomousAppGeneration.Memory.Qdrant;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Memory.Cognitive;

public static class CognitiveMemoryServiceCollectionExtensions
{
    public static IServiceCollection AddCognitiveMemoryBridge(this IServiceCollection services)
    {
        services.AddSingleton<ICognitiveMemoryBridge, HermesCognitiveMemoryBridge>();
        services.AddHostedService<CognitiveMemoryBackfillHostedService>();

        services.RemoveAll<IHermesMemoryStore>();
        services.AddSingleton<IHermesMemoryStore>(sp => WireHermesMemoryStore(sp));

        services.RemoveAll<IMemoryStore>();
        services.AddSingleton<IMemoryStore>(sp =>
            new HermesBackedMemoryStore(sp.GetRequiredService<IHermesMemoryStore>()));

        return services;
    }

    internal static IHermesMemoryStore WireHermesMemoryStore(IServiceProvider sp)
    {
        IHermesMemoryStore store = sp.GetRequiredService<SqliteHermesMemoryStore>();

        var qdrantOptions = sp.GetService<IOptions<QdrantSyncOptions>>();
        if (qdrantOptions?.Value.UseQdrantSync == true)
        {
            store = new QdrantSyncHermesMemoryStore(
                store,
                sp.GetRequiredService<IHermesVectorSyncService>());
        }

        return new CognitiveSyncHermesMemoryStore(
            store,
            sp.GetRequiredService<ICognitiveMemoryBridge>());
    }
}
