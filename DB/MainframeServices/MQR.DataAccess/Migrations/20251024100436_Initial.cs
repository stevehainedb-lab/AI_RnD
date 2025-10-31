using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MQR.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LogonCredentials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Identifier = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Pool = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Password = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PasswordEncrypted = table.Column<bool>(type: "bit", nullable: false),
                    LockedOut = table.Column<bool>(type: "bit", nullable: false),
                    LockedOutDateUtc = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: true),
                    PasswordChangedDateUtc = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: false),
                    LockLastTakenUtc = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogonCredentials", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RequestSession",
                columns: table => new
                {
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: true),
                    ClientTrackingId = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    ClientRequest = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime", unicode: false, nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestSession", x => x.RequestId);
                });

            migrationBuilder.CreateTable(
                name: "ActiveSession",
                columns: table => new
                {
                    TopsAlternateName = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    LockTakenUtc = table.Column<DateTime>(type: "datetime", unicode: false, nullable: true),
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RequestSessionRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActiveSession", x => x.TopsAlternateName);
                    table.ForeignKey(
                        name: "FK_ActiveSession_RequestSession_RequestId",
                        column: x => x.RequestId,
                        principalTable: "RequestSession",
                        principalColumn: "RequestId");
                    table.ForeignKey(
                        name: "FK_ActiveSession_RequestSession_RequestSessionRequestId",
                        column: x => x.RequestSessionRequestId,
                        principalTable: "RequestSession",
                        principalColumn: "RequestId");
                });

            migrationBuilder.CreateTable(
                name: "ParsedOutput",
                columns: table => new
                {
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParsedMainframeJson = table.Column<string>(type: "varchar(max)", unicode: false, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParsedOutput", x => x.RequestId);
                    table.ForeignKey(
                        name: "FK_ParsedOutput_RequestSession_RequestId",
                        column: x => x.RequestId,
                        principalTable: "RequestSession",
                        principalColumn: "RequestId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RawOutput",
                columns: table => new
                {
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RawMainframeOutput = table.Column<string>(type: "varchar(max)", unicode: false, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RawOutput", x => x.RequestId);
                    table.ForeignKey(
                        name: "FK_RawOutput_RequestSession_RequestId",
                        column: x => x.RequestId,
                        principalTable: "RequestSession",
                        principalColumn: "RequestId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActiveSession_RequestId",
                table: "ActiveSession",
                column: "RequestId",
                unique: true,
                filter: "[RequestId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ActiveSession_RequestSessionRequestId",
                table: "ActiveSession",
                column: "RequestSessionRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_LogonCredentials_Identifier",
                table: "LogonCredentials",
                column: "Identifier",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LogonCredentials_Pool",
                table: "LogonCredentials",
                column: "Pool");

            migrationBuilder.CreateIndex(
                name: "IX_RequestSession_SessionId_InProgress",
                table: "RequestSession",
                column: "SessionId",
                filter: "[Status] = 'InProgress'")
                .Annotation("SqlServer:Include", new[] { "RequestId" });

            // Create stored procedures
            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE [dbo].[sp_AcquireSessionLock]
    @TopsAlternateName VARCHAR(10),
    @RequestId UNIQUEIDENTIFIER,
    @LockTakenUtc DATETIME = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ActualLockTakenUtc DATETIME;
    DECLARE @PreviousLockTakenUtc DATETIME;
    DECLARE @PreviousRequestId UNIQUEIDENTIFIER;
    DECLARE @SessionExists BIT = 0;

    SET @ActualLockTakenUtc = COALESCE(@LockTakenUtc, SYSUTCDATETIME());

    BEGIN TRANSACTION;

    BEGIN TRY
        SELECT
            @PreviousLockTakenUtc = LockTakenUtc,
            @PreviousRequestId = RequestId,
            @SessionExists = 1
        FROM ActiveSession WITH (UPDLOCK)
        WHERE TopsAlternateName = @TopsAlternateName;

        IF @SessionExists = 0
        BEGIN
            ROLLBACK TRANSACTION;
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

        IF @PreviousLockTakenUtc IS NOT NULL
        BEGIN
            ROLLBACK TRANSACTION;
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

        UPDATE ActiveSession
        SET LockTakenUtc = @ActualLockTakenUtc,
            RequestId = @RequestId
        WHERE TopsAlternateName = @TopsAlternateName;

        COMMIT TRANSACTION;

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
END");

            migrationBuilder.Sql(@"
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
        SELECT
            @RequestId = RequestId,
            @PreviousLockTakenUtc = LockTakenUtc
        FROM ActiveSession WITH (UPDLOCK)
        WHERE TopsAlternateName = @TopsAlternateName;

        IF @RequestId IS NULL
        BEGIN
            ROLLBACK TRANSACTION;
            SELECT
                CONVERT(BIT, 0) AS Success,
                'Session not found' AS Message,
                @TopsAlternateName AS TopsAlternateName,
                NULL AS RequestId,
                NULL AS PreviousLockTakenUtc,
                0 AS RowsAffected;
            RETURN;
        END

        UPDATE ActiveSession
        SET LockTakenUtc = NULL,
            RequestId = NULL
        WHERE TopsAlternateName = @TopsAlternateName;

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

        COMMIT TRANSACTION;

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

        SELECT
            CONVERT(BIT, 0) AS Success,
            ERROR_MESSAGE() AS Message,
            @TopsAlternateName AS TopsAlternateName,
            @RequestId AS RequestId,
            @PreviousLockTakenUtc AS PreviousLockTakenUtc,
            0 AS RowsAffected;

        THROW;
    END CATCH
END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop stored procedures
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS [dbo].[sp_AcquireSessionLock]");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS [dbo].[sp_ReleaseSessionAndUpdateRawOutput]");

            migrationBuilder.DropTable(
                name: "ActiveSession");

            migrationBuilder.DropTable(
                name: "LogonCredentials");

            migrationBuilder.DropTable(
                name: "ParsedOutput");

            migrationBuilder.DropTable(
                name: "RawOutput");

            migrationBuilder.DropTable(
                name: "RequestSession");
        }
    }
}
