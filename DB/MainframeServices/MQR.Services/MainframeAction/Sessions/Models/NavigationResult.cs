using MQR.DataAccess.Entities;

namespace MQR.Services.MainframeAction.Sessions.Models;

public sealed record NavigationResult(
    bool Success,
    TransactionData? Transaction,
    ScreenSnapshot? LastSnapshot,
    string? FailureReason = null);