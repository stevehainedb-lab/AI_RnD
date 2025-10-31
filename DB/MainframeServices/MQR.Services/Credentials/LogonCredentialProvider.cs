using System.Text;
using Microsoft.EntityFrameworkCore;
using MQR.DataAccess.Context;
using MQR.DataAccess.Entities;

namespace MQR.Services.Credentials;

public interface ILogonCredentialProvider
{
    /// <summary>
    /// Returns an arbitrary valid logon credential, or null if none can be found.
    /// </summary>
    Task<LogonCredential?> GetValidCredentialAsync(string? pool, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Generates a new password suitable for storing in a login credential.
    /// </summary>
    Task<string> GenerateNewPasswordAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the password in an existential logon credential and persists the change.
    /// </summary>
    Task UpdateCredentialPasswordAsync(
        LogonCredential credential,
        string newPassword,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Mark a credential as no longer being valid for use.
    /// </summary>
    Task RevokeCredentialAsync(
        LogonCredential credential,
        CancellationToken cancellationToken = default);

    Task UnlockCredentialAsync(LogonCredential credential, CancellationToken cancellationToken = default);
}

public class LogonCredentialProvider(
    TimeProvider clock,
    IDbContextFactory<MQRDbContext> dbContextFactory,
    TripleDesEncryptor encryptor) : ILogonCredentialProvider
{
    public async Task<LogonCredential?> GetValidCredentialAsync(string? pool, CancellationToken cancellationToken = default)
    {
        var stale = clock.GetUtcNow().UtcDateTime.Subtract(TimeSpan.FromMinutes(10));
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        // We want to get whatever valid credential was used furthest in the past.
        // The point is to cycle through all our credentials.
        // If we use the same one all the time, it'll get locked out due to rate limits.
        var valid = await db.LogonCredentials
            .Where(s => !s.LockedOut)
            .Where(s => s.Pool == pool)
            .Where(s => s.LockLastTakenUtc == null || s.LockLastTakenUtc < stale)
            .OrderByDescending(s => s.LockLastTakenUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (valid is not null)
        {
            valid.LockLastTakenUtc = clock.GetUtcNow().UtcDateTime;
            await db.SaveChangesAsync(cancellationToken);
        }

        return valid;
    }

    public async Task UnlockCredentialAsync(LogonCredential credential, CancellationToken cancellationToken = default)
    {
        credential.LockLastTakenUtc = null;
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task<string> GenerateNewPasswordAsync(CancellationToken cancellationToken = default)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const int passwordLength = 6;

        var result = new StringBuilder(passwordLength);
        var random = Random.Shared;

        for (var i = 0; i < passwordLength; i++)
        {
            var index = random.Next(alphabet.Length);
            result.Append(alphabet[index]);
        }

        return Task.FromResult(result.ToString());
    }

    public async Task UpdateCredentialPasswordAsync(
        LogonCredential credential,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        if (credential.PasswordEncrypted)
        {
            newPassword = await encryptor.Encrypt(newPassword);
        }

        await ModifyCredentialAsync(credential, c =>
        {
            c.Password = newPassword;
            c.PasswordChangedDateUtc = clock.GetUtcNow().UtcDateTime;
        }, cancellationToken);
    }

    public async Task RevokeCredentialAsync(
        LogonCredential credential,
        CancellationToken cancellationToken = default)
    {
        await ModifyCredentialAsync(credential, c =>
        {
            c.LockedOut = true;
            c.LockedOutDateUtc = clock.GetUtcNow().UtcDateTime;
        }, cancellationToken);
    }

    private async Task ModifyCredentialAsync(LogonCredential credential,
        Action<LogonCredential> modification,
        CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var record = await db.LogonCredentials.FindAsync([credential.Id], cancellationToken);

        if (record is null)
        {
            throw new InvalidOperationException("Could not find credential with ID " + credential.Id);
        }
        
        modification(record);
        
        await db.SaveChangesAsync(cancellationToken);
    }
}