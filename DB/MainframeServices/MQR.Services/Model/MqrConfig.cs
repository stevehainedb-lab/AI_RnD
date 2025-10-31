namespace MQR.Services.Model;

public sealed class MqrConfig
{
    public const string SectionName = "Mqr";

    /// <summary>
    /// When to timeout a dead session.
    /// </summary>
    public int SessionStuckTimeoutMinutes { get; init; }
    
    /// <summary>
    /// The encryption key used to encrypt/decrypt passwords.
    /// </summary>
    public required string EncryptionKey { get; init; }
    
    /// <summary>
    /// How often do passwords need to be changed for the mainframe?
    /// </summary>
    public TimeSpan PasswordChangeInterval { get; init; }

    /// <summary>
    /// the default timeout for the TN3270 emulator.
    /// </summary>
    public TimeSpan Tn3270EmulatorDefaultTimeout { get; init; } = TimeSpan.FromSeconds(5);
    
    /// <summary>
    /// The interval to check sessions.
    /// </summary>
    public TimeSpan SessionScalerInterval { get; set; }
    
    /// <summary>
    /// Max number of days to store result cache for.
    /// </summary>
    public TimeSpan CacheExpirationDays { get; init; }

    /// <summary>
    /// the interval to allow the host to settle.
    /// </summary>
    public TimeSpan ScreenCheckInterval { get; set; } = TimeSpan.FromMilliseconds(200);
    
    /// <summary>
    /// the timeout interval to allow the host to settle.
    /// </summary>
    public TimeSpan HostSettleTimeout { get; set; }= TimeSpan.FromSeconds(5);
    
    /// <summary>
    /// the tolerance for the server mainframe time.
    /// </summary>
    public TimeSpan ServerMainframeTimeTolerance { get; set; } = TimeSpan.FromSeconds(30);
    
    /// <summary>
    /// the period to send keep alive messages to the mainframe.
    /// </summary>
    public TimeSpan Tn3270EmulatorKeepAlivePeriod { get; set; } = TimeSpan.FromMinutes(5);
}