using System.Globalization;
using MQR.Services.Instructions.Models.Shared;
using MQR.Services.MainframeAction.Sessions.Abstractions;
using MQR.Services.Observability;
using Open3270.Interfaces;

namespace MQR.Services.MainframeAction.Sessions.Services;

internal sealed class ScreenAvailabilityChecker : IScreenAvailabilityChecker
{
    public ITnEmulator Emulator { get; }
    private readonly IScreenWaiter _screenWaiter;
    private readonly IScreenDataExtractor _dataExtractor;

    public ScreenAvailabilityChecker(ITnEmulator emulator, IScreenWaiter screenWaiter, IScreenDataExtractor dataExtractor)
    {
        Emulator = emulator;
        _screenWaiter = screenWaiter;
        _dataExtractor = dataExtractor;
    }

    public async Task<(bool Ok, string? Reason, List<(string Key, string Value)> Captures)> CheckAsync(ProcessAction logonInstruction, TimeSpan serverMainframeTimeTolerance, IMainframeIoLogger? mainframeIoLogger, CancellationToken cancellationToken = default)
    {
        // Refresh and snapshot
        await _dataExtractor.GetVisibleScreenAsync(mainframeIoLogger, cancellationToken).ConfigureAwait(false);

        var (okTime, reasonTime, captures) = await CalculateServerToMfTimeShiftAsync(logonInstruction, serverMainframeTimeTolerance, mainframeIoLogger, cancellationToken).ConfigureAwait(false);

        // Check for error identification markers
        foreach (var identificationMarker in logonInstruction.ErrorScreenIdentificationMarks)
        {
            if (await _screenWaiter.WaitForScreenAsync(identificationMarker, mainframeIoLogger, cancellationToken).ConfigureAwait(false))
            {
                var reason = (reasonTime ?? string.Empty) + $"Communications Unavailable Identification Marker has been found:{identificationMarker.Identifier}.";
                return (false, reason, captures);
            }
        }

        return (okTime, reasonTime, captures);
    }

    private async Task<(bool Ok, string? Reason, List<(string Key, string Value)> Captures)> CalculateServerToMfTimeShiftAsync(ProcessAction logonInstruction, TimeSpan serverMainframeTimeTolerance, IMainframeIoLogger? mainframeIoLogger, CancellationToken cancellationToken)
    {
        if (logonInstruction.ScreenCaptureDataPoints.Count != 2)
        {
            var reason = $"Incorrect number of captures found. Expected 2, found {logonInstruction.ScreenCaptureDataPoints.Count}.";
            return (false, reason, []);
        }

        var serverTime = DateTime.Now;
        serverTime = new DateTime(serverTime.Ticks - serverTime.Ticks % TimeSpan.TicksPerSecond);

        var captures = await _dataExtractor.ExtractCapturePointsAsync(logonInstruction.ScreenCaptureDataPoints, mainframeIoLogger, cancellationToken).ConfigureAwait(false);
        var mfDate = captures.SingleOrDefault(i => i.Key == "MFDate").Value?.Trim();
        var mfTime = captures.SingleOrDefault(i => i.Key == "MFTime").Value?.Trim();

        var reasonAgg = string.Empty;
        if (mfDate == null) reasonAgg += "Could not extract MFDate";
        if (mfTime == null) reasonAgg += (reasonAgg.Length > 0 ? ", " : string.Empty) + "Could not extract MFDate";
        if (!string.IsNullOrEmpty(reasonAgg)) return (false, reasonAgg, captures);

        try
        {
            var mainframeTime = DateTime.ParseExact($"{mfDate} {mfTime}", "dd/MM/yy HH:mm:ss", DateTimeFormatInfo.InvariantInfo);

            var diff = mainframeTime - serverTime;
            captures.Add(("ServerToMfTimeShift", diff.ToString()));
            if (diff > serverMainframeTimeTolerance)
            {
                var reason = $"Time Shift Calculation Failed. Server Time: {serverTime} MF Time: {mainframeTime} Time Shift: {diff}";
                return (false, reason, captures);
            }
            else
            {
                mainframeIoLogger?.LogImportantLine($"Server Time: {serverTime:T} MF Time: {mainframeTime:T} Time Shift: {diff}");
                return (true, null, captures);
            }
        }
        catch (Exception ex)
        {
            var reason = $"could not calculate TimeSpan between MFTime: {mfDate} {mfTime} ServerTime: {serverTime} {Environment.NewLine}{ex}";
            return (false, reason, captures);
        }
    }
}