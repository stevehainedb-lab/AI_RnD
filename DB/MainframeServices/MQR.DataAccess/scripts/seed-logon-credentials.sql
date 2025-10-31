-- Seed LogonCredentials table with data from JSON files
-- This script inserts all credentials from the various environment JSON files

SET NOCOUNT ON;

-- DEV Environment
INSERT INTO LogonCredential (Id, Identifier, Pool, UserName, Password, PasswordEncrypted, LockedOut, LockedOutDateUtc, PasswordChangedDateUtc, LockLastTakenUtc)
VALUES
    (NEWID(), 'WATMU30', '', '#WATMU30', '1ABB8C9B24806707', 1, 0, NULL, '2011-04-01T00:00:00', '2011-04-15T12:37:51'),
    (NEWID(), 'WATMU31', '', '#WATMU31', '1ABB8C9B24806707', 1, 0, NULL, '2011-04-01T00:00:00', '2011-04-15T12:27:53'),
    (NEWID(), 'WATMU32', '', '#WATMU32', '1ABB8C9B24806707', 1, 0, NULL, '2011-04-01T00:00:00', '2011-04-15T12:43:05'),
    (NEWID(), 'WATMU33', '', '#WATMU33', '1ABB8C9B24806707', 1, 0, NULL, '2011-04-01T00:00:00', NULL),
    (NEWID(), 'WATMU34', '', '#WATMU34', '1ABB8C9B24806707', 1, 0, NULL, '2011-04-01T00:00:00', '2011-04-15T12:36:41');

-- LIVE Environment
INSERT INTO LogonCredential (Id, Identifier, Pool, UserName, Password, PasswordEncrypted, LockedOut, LockedOutDateUtc, PasswordChangedDateUtc, LockLastTakenUtc)
VALUES
    (NEWID(), 'WATMU01', NULL, '#WATMU01', 'testing', 0, 0, NULL, '2009-03-09T16:58:47', '2009-03-29T04:50:26'),
    (NEWID(), 'WATMU02', NULL, '#WATMU02', 'testing', 0, 0, NULL, '2009-03-09T16:58:47', '2009-03-29T04:50:26'),
    (NEWID(), 'WATMU03', NULL, '#WATMU03', 'testing', 0, 0, NULL, '2009-03-09T16:58:47', '2009-03-29T04:50:24'),
    (NEWID(), 'WATMU04', NULL, '#WATMU04', 'testing', 0, 0, NULL, '2009-03-09T16:58:47', '2009-03-29T04:50:24'),
    (NEWID(), 'WATMU05', NULL, '#WATMU05', 'testing', 0, 0, NULL, '2009-03-09T16:58:47', '2009-03-29T04:50:25');

-- UAT Environment
INSERT INTO LogonCredential (Id, Identifier, Pool, UserName, Password, PasswordEncrypted, LockedOut, LockedOutDateUtc, PasswordChangedDateUtc, LockLastTakenUtc)
VALUES
    (NEWID(), 'WATMA36', NULL, '#WATMA36', '6FCBADFD667DD868', 1, 0, NULL, '2007-06-13T14:20:53', '2007-06-13T14:20:54'),
    (NEWID(), 'WATMA37', NULL, '#WATMA37', '0E2E20E57FB1A984', 1, 0, NULL, '2007-05-08T14:23:25', '2007-06-07T12:22:43'),
    (NEWID(), 'WATMA38', NULL, '#WATMA38', 'F25489B85C2AE746', 1, 0, NULL, '2007-05-08T14:23:25', '2007-06-07T13:15:51'),
    (NEWID(), 'WATMA39', NULL, '#WATMA39', 'C9015FAA3B650233', 1, 0, NULL, '2007-06-13T14:21:59', '2007-06-13T14:21:59'),
    (NEWID(), 'WATMA40', NULL, '#WATMA40', '2EA1150B0A50E469', 1, 0, NULL, '2007-06-13T14:24:10', '2007-06-13T14:24:11');

-- DR Environment
INSERT INTO LogonCredential (Id, Identifier, Pool, UserName, Password, PasswordEncrypted, LockedOut, LockedOutDateUtc, PasswordChangedDateUtc, LockLastTakenUtc)
VALUES
    (NEWID(), 'WATMU06', NULL, '#WATMU06', 'testing', 0, 0, NULL, '2009-03-09T16:58:47', '2009-03-29T04:50:26'),
    (NEWID(), 'WATMU07', NULL, '#WATMU07', 'testing', 0, 0, NULL, '2009-03-09T16:58:47', '2009-03-29T04:50:26'),
    (NEWID(), 'WATMU08', NULL, '#WATMU08', 'testing', 0, 0, NULL, '2009-03-09T16:58:47', '2009-03-29T04:50:24'),
    (NEWID(), 'WATMU09', NULL, '#WATMU09', 'testing', 0, 0, NULL, '2009-03-09T16:58:47', '2009-03-29T04:50:24'),
    (NEWID(), 'WATMU10', NULL, '#WATMU10', 'testing', 0, 0, NULL, '2009-03-09T16:58:47', '2009-03-29T04:50:25');

-- SYS Environment
INSERT INTO LogonCredential (Id, Identifier, Pool, UserName, Password, PasswordEncrypted, LockedOut, LockedOutDateUtc, PasswordChangedDateUtc, LockLastTakenUtc)
VALUES
    (NEWID(), 'WATMU19', NULL, '#WATMU19', 'testing', 0, 0, NULL, '2009-03-09T16:58:47', '2009-03-29T04:50:24'),
    (NEWID(), 'WATMU20', NULL, '#WATMU20', 'testing', 0, 0, NULL, '2009-03-09T16:58:47', '2009-03-29T04:50:26'),
    (NEWID(), 'WATMU21', NULL, '#WATMU21', 'testing', 0, 0, NULL, '2009-03-09T16:58:47', '2009-03-29T04:50:26'),
    (NEWID(), 'WATMU22', NULL, '#WATMU22', 'testing', 0, 0, NULL, '2009-03-09T16:58:47', '2009-03-29T04:50:25'),
    (NEWID(), 'WATMU23', NULL, '#WATMU23', 'testing', 0, 0, NULL, '2009-03-09T16:58:47', '2009-03-29T04:50:24');

-- SOAK Environment
INSERT INTO LogonCredential (Id, Identifier, Pool, UserName, Password, PasswordEncrypted, LockedOut, LockedOutDateUtc, PasswordChangedDateUtc, LockLastTakenUtc)
VALUES
    (NEWID(), 'WATMA41', NULL, '#WATMA41', 'B0F3C08BB33C8FDA', 1, 0, NULL, '2007-09-24T14:48:29', '2007-09-24T15:34:51'),
    (NEWID(), 'WATMA42', NULL, '#WATMA42', '9A46A65423F3DA1B', 1, 0, NULL, '2007-09-24T14:48:29', '2007-09-24T15:34:51'),
    (NEWID(), 'WATMA43', NULL, '#WATMA43', '3623A33D08C422E8', 1, 0, NULL, '2007-09-24T14:48:28', '2007-09-24T15:34:52'),
    (NEWID(), 'WATMA44', NULL, '#WATMA44', '8E3C4705EDBEE63B', 1, 0, NULL, '2007-09-24T14:48:29', '2007-09-24T15:34:52'),
    (NEWID(), 'WATMA45', NULL, '#WATMA45', '9ECA7003CC6BF8D0', 1, 0, NULL, '2007-09-24T14:48:29', '2007-09-24T15:34:52');

-- PREPROD Environment
INSERT INTO LogonCredential (Id, Identifier, Pool, UserName, Password, PasswordEncrypted, LockedOut, LockedOutDateUtc, PasswordChangedDateUtc, LockLastTakenUtc)
VALUES
    (NEWID(), 'WATMU14', NULL, '#WATMU14', 'SBLSGE', 0, 0, NULL, '2009-03-09T16:58:47', '2009-03-29T04:50:24'),
    (NEWID(), 'WATMU15', NULL, '#WATMU15', 'WSVSUY', 0, 0, NULL, '2009-03-09T16:58:47', '2009-03-29T04:50:25'),
    (NEWID(), 'WATMU16', NULL, '#WATMU16', 'NWQQKK', 0, 0, NULL, '2009-03-09T16:58:47', '2009-03-29T04:50:26'),
    (NEWID(), 'WATMU17', NULL, '#WATMU17', 'NHDEHJ', 0, 0, NULL, '2009-03-09T16:58:47', '2009-03-29T04:50:26'),
    (NEWID(), 'WATMU18', NULL, '#WATMU18', 'QWGSWJ', 0, 0, NULL, '2009-03-09T16:58:47', '2009-03-29T04:50:24');

-- SYSdevx Environment
INSERT INTO LogonCredential (Id, Identifier, Pool, UserName, Password, PasswordEncrypted, LockedOut, LockedOutDateUtc, PasswordChangedDateUtc, LockLastTakenUtc)
VALUES
    (NEWID(), 'WABCKCW', NULL, '#WABCKCW', 'B10BFA0D77C5B36A', 1, 0, NULL, '2008-08-14T16:13:42', '2008-10-20T16:09:58');

PRINT 'Successfully inserted ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' credentials';
