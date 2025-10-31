namespace MQR.Services.Instructions.Models.Shared;

public class SuccessCondition
{
    public bool LockCredentialOnFailure { get; init; }
    public bool ResetSessionOnFailure { get; init; }
    public List<ScreenIdentificationMark> ScreenIdentificationMarks { get; init; } = [];
}