using System.Diagnostics.CodeAnalysis;

namespace MQR.Services.MainframeAction.Sessions.Models;

public sealed record ScreenSnapshot(
    string? SafeText,
    DateTime CapturedUtc,
    string? ScreenId = null)
{
    [MemberNotNullWhen(true, nameof(SafeText))]
    public bool HasText => !string.IsNullOrEmpty(SafeText);
}