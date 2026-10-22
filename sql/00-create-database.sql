-- Idempotent: creates TeeTimeDb only if it does not exist yet.
IF DB_ID(N'TeeTimeDb') IS NULL
    CREATE DATABASE TeeTimeDb;
