-- Post-deployment script for VitalNexus.PatientHealth.Database (Patients database).
PRINT 'Patients database post-deployment script executed.';

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'sec_ClinicalRead')
    EXEC('CREATE ROLE [sec_ClinicalRead]');
GO

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'sec_ClinicalWrite')
    EXEC('CREATE ROLE [sec_ClinicalWrite]');
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Placeholder)
BEGIN
    INSERT INTO dbo.Placeholder (Note)
    VALUES ('Provisioned Patients database — clinical schema will expand in later phases.');
END
GO
