-- Placeholder security objects (roles, permissions). Fill in as needed.
-- Example: create role for read-only consumers
IF NOT EXISTS (SELECT * FROM sys.database_principals WHERE name = N'sec_ReadOnly')
    EXEC('CREATE ROLE [sec_ReadOnly]');
