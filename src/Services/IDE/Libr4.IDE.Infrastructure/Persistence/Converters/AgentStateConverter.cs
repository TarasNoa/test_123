using Libr4.IDE.Domain.FSharp;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Libr4.IDE.Infrastructure.Persistence.Converters;

/// <summary>
/// EF Core value converter for F# AgentState discriminated union
/// Converts between F# state and string for database storage
/// </summary>
public class AgentStateConverter : ValueConverter<AgentState, string>
{
    public AgentStateConverter()
        : base(
            v => StatePersistence.serializeState(v),
            v => StatePersistence.deserializeState(v))
    {
    }
}
