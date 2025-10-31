namespace MQR.Services.Instructions.Models.Shared;

/// <summary>
/// Defines input to be sent to a specific screen position or field.
/// </summary>
public class ScreenInput
{
    /// <summary>
    /// Identifier for this screen input.
    /// </summary>
    public required string Identifier { get; init; }

    /// <summary>
    /// The value to input (may contain parameter placeholders like [#ParamName#]).
    /// </summary>
    public required string Value { get; init; }

    /// <summary>
    /// teh position to input the value.
    /// </summary>
    public required ScreenPosition Position { get; init; }

    public override string ToString() => $"{Identifier} = {Value}";
}