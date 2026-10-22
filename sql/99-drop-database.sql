-- Used by `scripts/db.ps1 -Reset`. Drops the practice database (local Docker only!).
IF DB_ID(N'TeeTimeDb') IS NOT NULL
BEGIN
    ALTER DATABASE TeeTimeDb SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE TeeTimeDb;
END
