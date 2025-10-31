namespace MQR.DataAccess.Entities;

public class LogonCredential
{
    public Guid Id { get; init; }
    
    public string? Pool { get; set; }

    public required string UserName { get; init; }

    public required string Password { get; set; }

    /// <summary>
    /// Specifies if the password is encrypted at rest.
    /// </summary>
    public bool PasswordEncrypted { get; set; }

    /// <summary>
    /// Represents if a credential has been revoked and can no longer be used to access the mainframe.
    /// </summary>
    public bool LockedOut { get; set; }

    /// <summary>
    /// Marks when this credential was revoked.
    /// </summary>
    public DateTime? LockedOutDateUtc { get; set; }

    /// <summary>
    /// Marks when this credential's password was last changed.
    /// </summary>
    public DateTime PasswordChangedDateUtc { get; set; }

    /// <summary>
    /// Marks when this credential was last used to initiate a mainframe session.
    /// </summary>
    public DateTime? LockLastTakenUtc { get; set; }
    
    public override string ToString() => UserName;
}