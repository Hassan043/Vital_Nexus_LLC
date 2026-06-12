-- Tenant-scoped database roles for the Patients database.
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'sec_ClinicalRead')
    EXEC('CREATE ROLE [sec_ClinicalRead]');

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'sec_ClinicalWrite')
    EXEC('CREATE ROLE [sec_ClinicalWrite]');
