namespace MQR.Services.Instructions.Models.Shared;

/// <summary>
/// An action to process during query execution, potentially with nested actions.
/// </summary>
public class ProcessAction
{
    /// <summary>Identifier for this process action.</summary>
    public required string Identifier { get; init; }

    /// <summary>Whether this is a core action in the query flow.</summary>
    public bool IsCoreAction { get; init; }

    /// <summary>Whether no data is expected from this action.</summary>
    public bool NoPrintDataExpected { get; init; }
    
    /// <summary>Whether to skip this action if it is not enabled.</summary>
    public string? EnabledWhen { get; init; }
    
    /// <summary>Data points to capture from the screen.</summary>
    public List<ScreenCaptureDataPoint> ScreenCaptureDataPoints { get; init; } = [];

    /// <summary>Marks that identify a failure screen state.</summary>
    public List<ScreenIdentificationMark> ErrorScreenIdentificationMarks { get; init; } = [];
    
    /// <summary>Marks that identify the expected screen state.</summary>
    public List<ScreenIdentificationMark> ScreenIdentificationMarks { get; init; } = [];

    /// <summary>Inputs to provide to the screen.</summary>
    public List<ScreenInput> ScreenInputs { get; init; } = [];

    /// <summary>Optional navigation action to perform before running other actions.</summary>
    public NavigationAction? NavigationAction { get; init; }

    /// <summary>Marks that identify the expected screen state post Navigation Action.</summary>
    public List<ScreenIdentificationMark> SuccessScreenIdentificationMarks { get; init; } = [];
    
    /// <summary>Nested process actions to execute.</summary>
    public List<ProcessAction> ProcessActions { get; init; } = [];
    
    /// <summary>Condition that defines successful query execution.</summary>
    public SuccessCondition? SuccessCondition { get; init; }

    public override string ToString() => Identifier;
}
