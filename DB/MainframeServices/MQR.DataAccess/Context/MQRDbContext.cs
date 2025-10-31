using MQR.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using System.Data;

namespace MQR.DataAccess.Context;

public class MQRDbContext : DbContext
{
    public MQRDbContext()
    {
    }

    public MQRDbContext(DbContextOptions<MQRDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<RequestSession> RequestSessions { get; set; }
    public virtual DbSet<ActiveSession> ActiveSessions { get; set; }
    public virtual DbSet<RawOutput> RawOutputs { get; set; }
    public virtual DbSet<ParsedOutput> ParsedOutputs { get; set; }
    public virtual DbSet<TransactionData> TransactionData { get; set; }

    public virtual DbSet<LogonCredential> LogonCredentials { get; set; }

    /// <summary>
    /// Acquires a session lock by setting LockTakenUtc and associating a RequestId.
    /// Fails if the session is already locked.
    /// </summary>
    /// <param name="topsAlternateName">The session name to lock</param>
    /// <param name="requestId">The RequestId to associate with this lock</param>
    /// <param name="lockTakenUtc">Optional timestamp for the lock (defaults to current UTC time)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating success/failure and details of the lock operation</returns>
    public async Task<AcquireSessionLockResult> AcquireSessionLockAsync(
        string instanceName,
        string poolId,
        Guid requestId,
        DateTimeOffset? lockTakenUtc = null,
        CancellationToken cancellationToken = default)
    {
        var instanceNameParam = new SqlParameter("@InstanceName", SqlDbType.VarChar, 50)
        {
            Value = instanceName
        };
        
        var poolIdParam = new SqlParameter("@PoolId", SqlDbType.VarChar, 10)
        {
            Value = poolId
        };

        var requestIdParam = new SqlParameter("@RequestId", SqlDbType.UniqueIdentifier)
        {
            Value = requestId
        };

        var lockTakenUtcParam = new SqlParameter("@LockTakenUtc", SqlDbType.DateTime)
        {
            Value = lockTakenUtc ?? DateTimeOffset.UtcNow
        };

        var results = await Database
            .SqlQueryRaw<AcquireSessionLockResult>(
                "EXEC sp_AcquireSessionLock @InstanceName, @PoolId, @RequestId, @LockTakenUtc",
                instanceNameParam,
                poolIdParam,
                requestIdParam,
                lockTakenUtcParam)
            .ToListAsync(cancellationToken);

        return results.FirstOrDefault() ?? new AcquireSessionLockResult
        {
            Success = false,
            Message = "No result returned from stored procedure",
            RequestId = requestId
        };
    }

    /// <summary>
    /// Releases a session lock and updates the raw mainframe output in a single transaction.
    /// Sets LockTakenUtc to NULL on ActiveSession and updates/inserts the RawMainframeOutput.
    /// </summary>
    /// <param name="topsAlternateName">The session name to release</param>
    /// <param name="rawMainframeOutput">The raw mainframe output text to store</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating success/failure and details of the operation</returns>
    public async Task<ReleaseSessionResult> ReleaseSessionAndUpdateRawOutputAsync(
        string topsAlternateName,
        string rawMainframeOutput,
        CancellationToken cancellationToken = default)
    {
        var topsAlternateNameParam = new SqlParameter("@TopsAlternateName", SqlDbType.VarChar, 10)
        {
            Value = topsAlternateName
        };

        var rawMainframeOutputParam = new SqlParameter("@RawMainframeOutput", SqlDbType.VarChar, -1) // -1 for MAX
        {
            Value = rawMainframeOutput
        };
        
        var results = await Database
            .SqlQueryRaw<ReleaseSessionResult>(
                "EXEC sp_ReleaseSessionAndUpdateRawOutput @TopsAlternateName, @RawMainframeOutput",
                topsAlternateNameParam,
                rawMainframeOutputParam)
            .ToListAsync(cancellationToken);

        return results.FirstOrDefault() ?? new ReleaseSessionResult
        {
            Success = false,
            Message = "No result returned from stored procedure",
            TopsAlternateName = topsAlternateName
        };
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LogonCredential>(entity =>
        {
            entity.ToTable("LogonCredentials");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .ValueGeneratedNever();
            
            entity.Property(e => e.Pool)
                .HasMaxLength(100);

            entity.Property(e => e.UserName)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.Password)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.PasswordEncrypted)
                .IsRequired();

            entity.Property(e => e.LockedOut)
                .IsRequired();

            entity.Property(e => e.LockedOutDateUtc);

            entity.Property(e => e.PasswordChangedDateUtc);

            entity.Property(e => e.LockLastTakenUtc);
            
            entity.HasIndex(e => e.Pool);
        });

        modelBuilder.Entity<RawOutput>(entity =>
        {
            entity.HasKey(e => e.RequestId);

            entity.ToTable("RawOutput");

            entity.Property(e => e.RawMainframeOutput).IsUnicode(false);
        });

        modelBuilder.Entity<ParsedOutput>(entity =>
        {
            entity.HasKey(e => e.RequestId);

            entity.ToTable("ParsedOutput");

            entity.Property(e => e.ParsedMainframeJson).IsUnicode(false);
        });

        modelBuilder.Entity<ActiveSession>(entity =>
        {
            entity.HasKey(e => e.TopsAlternateName);

            entity.ToTable("ActiveSession");
            entity.Property(e => e.SessionId).IsUnicode(false).HasMaxLength(10);
            entity.Property(e => e.TopsAlternateName).IsUnicode(false).HasMaxLength(10);
            entity.Property(e => e.LockTakenUtc).IsUnicode(false).HasColumnType("datetime");

            entity.HasOne<RequestSession>()
                .WithOne(r => r.ActiveSession)
                .HasForeignKey<ActiveSession>(e => e.RequestId)
                .HasPrincipalKey<RequestSession>(r => r.RequestId);
        });

        modelBuilder.Entity<RequestSession>(entity =>
        {
            entity.HasKey(e => e.RequestId);

            entity.ToTable("RequestSession");

            // The PrintConsumer wants to find items which:
            // - have a given SessionId
            // - are InProgress
            // We include the properties we want in the query to make the index 'covering'.
            // That should hopefully mean we skip any table scans at all.
            entity.HasIndex(e => e.SessionId)
                .HasDatabaseName("IX_RequestSession_SessionId_InProgress")
                .HasFilter("[Status] = 'InProgress'")
                .IncludeProperties(e => new
                {
                    e.RequestId
                });

            entity.Property(e => e.RequestId).ValueGeneratedNever();
            entity.Property(e => e.SessionId).IsUnicode(false).HasMaxLength(10);
            entity.Property(e => e.ClientTrackingId).IsUnicode(false).HasMaxLength(255);
            entity.Property(e => e.ClientRequest).IsUnicode(false).HasMaxLength(500);
            entity.Property(e => e.Status).HasConversion<string>().IsUnicode(false).HasMaxLength(30);
            entity.Property(e => e.CreatedOnUtc)
                .IsUnicode(false)
                .HasColumnType("datetime")
                .HasDefaultValueSql("GETUTCDATE()");
            
            entity.HasOne(r => r.RawMainframeOutput)
                .WithOne()
                .HasForeignKey<RawOutput>(ro => ro.RequestId)
                .HasPrincipalKey<RequestSession>(rs => rs.RequestId);

            entity.HasOne(r => r.ParsedMainframeOutput)
                .WithOne()
                .HasForeignKey<ParsedOutput>(po => po.RequestId)
                .HasPrincipalKey<RequestSession>(rs => rs.RequestId);
        });
        
        modelBuilder.Entity<RequestCache>(entity =>
        {
            entity.ToTable("RequestCache");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.Value)
                .IsRequired();

            entity.Property(e => e.ExpiresAtTime)
                .IsRequired();

            entity.Property(e => e.SlidingExpirationInSeconds);

            entity.Property(e => e.AbsoluteExpiration);
        });

        modelBuilder.Entity<TransactionData>(entity =>
            {
                entity.ToTable("TransactionData");
                entity.Property(e => e.Action)
                    .IsRequired();
                entity.Property(e => e.ActionContext);
                entity.Property(e => e.Credential)
                    .IsRequired();
                entity.Property(e => e.SessionId)
                    .IsRequired();
                entity.Property(e => e.CallingApplication);
                entity.Property(e => e.Tags);
            }
        );
    }
}