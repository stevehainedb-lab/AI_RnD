using MQR.Services.Instructions.Models.Shared;
using MQR.Services.MainframeAction.Sessions.Abstractions;
using MQR.Services.Observability;
using Open3270.Interfaces;

namespace MQR.Services.MainframeAction.Sessions.Services;

internal sealed class ScreenInputWriter : IScreenInputWriter
{
    public ITnEmulator Emulator { get; }
    private readonly IScreenDataExtractor _dataExtractor;

    public ScreenInputWriter(ITnEmulator emulator, IScreenDataExtractor dataExtractor)
    {
        Emulator = emulator;
        _dataExtractor = dataExtractor;
    }

    public async Task WriteInputsAsync(List<ScreenInput> inputs, Func<string, string>? getValueMethod, IMainframeIoLogger? mainframeIoLogger = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (inputs is not { Count: > 0 }) return;

        var dataToLog = new List<string>();
        foreach (var input in inputs)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var valueToWrite = input.GetInputValue(getValueMethod);
            if (string.IsNullOrWhiteSpace(valueToWrite)) continue;
            dataToLog.Add($"Writing {input.Identifier} to screen: '{valueToWrite}'");
            await SetFieldAsync(input.Position, valueToWrite, mainframeIoLogger, cancellationToken).ConfigureAwait(false);
        }

        if (dataToLog.Count == 0)
            throw new InvalidOperationException("No Inputs to Write");

        mainframeIoLogger?.LogWrapped(dataToLog);
        await _dataExtractor.GetVisibleScreenAsync(mainframeIoLogger, cancellationToken).ConfigureAwait(false);
    }

    public Task SetFieldAsync(ScreenPosition input, string valueToWrite, IMainframeIoLogger? mainframeIoLogger = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (input.FieldNumber.HasValue)
        {
            Emulator.SetField((int)input.FieldNumber.Value, valueToWrite);
            mainframeIoLogger?.LogImportantLine($"SetField {input} to {valueToWrite}");
            return Task.CompletedTask;
        }

        if (input is { StartRow: not null, StartColumn: not null })
        {
            Emulator.SetCursor((int)input.StartColumn, (int)input.StartRow);
            Emulator.SendText(valueToWrite);
            mainframeIoLogger?.LogImportantLine($"SentText ({input.StartRow}, {input.StartColumn}) to {valueToWrite}");
            return Task.CompletedTask;
        }

        throw new InvalidOperationException($"Invalid ScreenPosition {input}");
    }
}