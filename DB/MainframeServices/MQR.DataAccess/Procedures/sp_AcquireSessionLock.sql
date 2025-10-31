-- Stored procedure to acquire a session lock
-- This operation sets the LockTakenUtc timestamp and associates a RequestId with the session
-- Designed to be expanded in the future with additional lock acquisition logic

CREATE OR ALTER PROCEDURE [dbo].[sp_AcquireSessionLock]
    @TopsAlternateName VARCHAR(10),
    @RequestId UNIQUEIDENTIFIER,
    @LockTakenUtc DATETIME = NULL -- If not provided, uses current UTC time
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ActualLockTakenUtc DATETIME;
    DECLARE @PreviousLockTakenUtc DATETIME;
    DECLARE @PreviousRequestId UNIQUEIDENTIFIER;
    DECLARE @SessionExists BIT = 0;

    -- Use current UTC time if not provided
    SET @ActualLockTakenUtc = COALESCE(@LockTakenUtc, SYSUTCDATETIME());

    BEGIN TRANSACTION;

    BEGIN TRY
        -- Get current lock state with an update lock to prevent race conditions
        SELECT
            @PreviousLockTakenUtc = LockTakenUtc,
            @PreviousRequestId = RequestId,
            @SessionExists = 1
        FROM ActiveSession WITH (UPDLOCK)
        WHERE TopsAlternateName = @TopsAlternateName;

        -- Check if the session exists
        IF @SessionExists = 0
        BEGIN
            ROLLBACK TRANSACTION;
            -- Return result indicating session not found
            SELECT
                0 AS Success,
                'Session not found' AS Message,
                @TopsAlternateName AS TopsAlternateName,
                @RequestId AS RequestId,
                NULL AS LockTakenUtc,
                NULL AS PreviousLockTakenUtc,
                NULL AS PreviousRequestId,
                0 AS WasAlreadyLocked;
            RETURN;
        END

        -- Check if session is already locked
        IF @PreviousLockTakenUtc IS NOT NULL
        BEGIN
            ROLLBACK TRANSACTION;
            -- Return result indicating session is already locked
            SELECT
                0 AS Success,
                'Session is already locked' AS Message,
                @TopsAlternateName AS TopsAlternateName,
                @RequestId AS RequestId,
                @ActualLockTakenUtc AS LockTakenUtc,
                @PreviousLockTakenUtc AS PreviousLockTakenUtc,
                @PreviousRequestId AS PreviousRequestId,
                1 AS WasAlreadyLocked;
            RETURN;
        END

        -- Acquire the lock
        UPDATE ActiveSession
        SET LockTakenUtc = @ActualLockTakenUtc,
            RequestId = @RequestId
        WHERE TopsAlternateName = @TopsAlternateName;

        COMMIT TRANSACTION;

        -- Return success result
        SELECT
            1 AS Success,
            'Session lock acquired successfully' AS Message,
            @TopsAlternateName AS TopsAlternateName,
            @RequestId AS RequestId,
            @ActualLockTakenUtc AS LockTakenUtc,
            @PreviousLockTakenUtc AS PreviousLockTakenUtc,
            @PreviousRequestId AS PreviousRequestId,
            0 AS WasAlreadyLocked;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        -- Return error result
        SELECT
            0 AS Success,
            ERROR_MESSAGE() AS Message,
            @TopsAlternateName AS TopsAlternateName,
            @RequestId AS RequestId,
            @ActualLockTakenUtc AS LockTakenUtc,
            @PreviousLockTakenUtc AS PreviousLockTakenUtc,
            @PreviousRequestId AS PreviousRequestId,
            CASE WHEN @PreviousLockTakenUtc IS NOT NULL THEN 1 ELSE 0 END AS WasAlreadyLocked;

        THROW;
    END CATCH
END
GO
