using MQR.Services.Instructions.Models.Shared;

namespace MQR.Services.Instructions.Models.Queries;

/// <summary>
/// Represents instructions for querying mainframe systems.
/// </summary>
public class QueryInstructionSet
{
    /// <summary>Unique identifier for this query instruction set.</summary>
    public required string Identifier { get; init; }
    
    /// <summary>List of process actions to execute during the query.</summary>
    public List<ProcessAction> ProcessActions { get; init; } = [];
    
    public override string ToString() => Identifier;
}


