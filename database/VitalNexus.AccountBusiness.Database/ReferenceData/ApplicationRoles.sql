-- Seed application roles referenced by UserRoles.
IF NOT EXISTS (SELECT 1 FROM dbo.ApplicationRoles WHERE Id = 1)
BEGIN
    INSERT INTO dbo.ApplicationRoles (Id, Name)
    VALUES (1, 'Clinician');
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.ApplicationRoles WHERE Id = 2)
BEGIN
    INSERT INTO dbo.ApplicationRoles (Id, Name)
    VALUES (2, 'ClinicAdmin');
END
GO
