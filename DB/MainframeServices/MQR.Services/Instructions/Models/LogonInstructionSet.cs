using MQR.Services.Instructions.Models.Shared;

namespace MQR.Services;

/// <summary>
/// Represents instructions for initiating mainframe sessions.
/// </summary>
public class LogonInstructionSet
{
    /// <summary>Identifies this instruction set.</summary>
    public required string Identifier { get; init; }

    /// <summary>Pool name the credentials are drawn from.</summary>
    public string? CredentialPool { get; init; }
    
    /// <summary>
    /// The connection params to the mainframe.
    /// </summary>
    public required LogonConnection LogonConnection { get; init; }
    
    /// <summary>
    /// The logon instructions.
    /// </summary>
    public required ProcessAction LogonInstruction { get; init; }
    
    /// <summary>
    /// How many active sessions this instruction set will try to maintain.
    /// Why do we put this on the instruction set, and not (say) in appsettings.json?
    /// Because different instruction sets may have different requirements.
    /// One for TOPS will see different levels of traffic to one for TRUST.
    /// </summary>
    public int InstanceSessionCount { get; init; }

    public override string ToString() => Identifier;
}

public class LogonConnection
{
    /// <summary>Overall connection timeout.</summary>
    public TimeSpan? ConnectionTimeOut { get; init; }

    public required string HostAddress { get; init; }

    /// <summary>TCP port.</summary>
    public required int HostPort { get; init; }

    /// <summary>Terminal device type (domain-specific string, kept as-is).</summary>
    public TerminalDeviceType TerminalDeviceType { get; init; }
}

public enum TerminalDeviceType
{
    IBM32782E,
    IBM32782
}
