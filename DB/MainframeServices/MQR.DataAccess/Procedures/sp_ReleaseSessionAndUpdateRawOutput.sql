-- Stored procedure to release a session lock and update raw mainframe output
-- This operation is performed in a single transaction to ensure consistency

CREATE OR ALTER PROCEDURE [dbo].[sp_ReleaseSessionAndUpdateRawOutput]
    @TopsAlternateName VARCHAR(10),
    @RawMainframeOutput VARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @RequestId UNIQUEIDENTIFIER;
    DECLARE @PreviousLockTakenUtc DATETIME;
    DECLARE @RowsAffected INT = 0;

    BEGIN TRANSACTION;

    BEGIN TRY
        -- Get the RequestId and previous lock time from ActiveSession
        SELECT
            @RequestId = RequestId,
            @PreviousLockTakenUtc = LockTakenUtc
        FROM ActiveSession WITH (UPDLOCK)
        WHERE TopsAlternateName = @TopsAlternateName;

        -- Check if the session exists
        IF @RequestId IS NULL
        BEGIN
            ROLLBACK TRANSACTION;
            -- Return result indicating session not found
            SELECT
                CONVERT(BIT, 0) AS Success,
                'Session not found' AS Message,
                @TopsAlternateName AS TopsAlternateName,
                NULL AS RequestId,
                NULL AS PreviousLockTakenUtc,
                0 AS RowsAffected;
            RETURN;
        END

        -- Release the session lock
        UPDATE ActiveSession
        SET LockTakenUtc = NULL,
            RequestId = NULL
        WHERE TopsAlternateName = @TopsAlternateName;

        -- Update or insert the raw output
        IF EXISTS (SELECT 1 FROM RawOutput WHERE RequestId = @RequestId)
        BEGIN
            UPDATE RawOutput
            SET RawMainframeOutput = @RawMainframeOutput
            WHERE RequestId = @RequestId;

            SET @RowsAffected = @@ROWCOUNT;
        END
        ELSE
        BEGIN
            INSERT INTO RawOutput (RequestId, RawMainframeOutput)
            VALUES (@RequestId, @RawMainframeOutput);

            SET @RowsAffected = @@ROWCOUNT;
        END
        
        -- Set RequestSession status to ParsingMainframeResponse        
        UPDATE RequestSession
        SET Status = 'ParsingMainframeResponse'
        WHERE RequestId = @RequestId;

        COMMIT TRANSACTION;

        -- Return success result
        SELECT
            CONVERT(BIT, 1) AS Success,
            'Session released and output updated successfully' AS Message,
            @TopsAlternateName AS TopsAlternateName,
            @RequestId AS RequestId,
            @PreviousLockTakenUtc AS PreviousLockTakenUtc,
            @RowsAffected AS RowsAffected;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        -- Return error result
        SELECT
            CONVERT(BIT, 0) AS Success,
            ERROR_MESSAGE() AS Message,
            @TopsAlternateName AS TopsAlternateName,
            @RequestId AS RequestId,
            @PreviousLockTakenUtc AS PreviousLockTakenUtc,
            0 AS RowsAffected;

        THROW;
    END CATCH
END
GO
