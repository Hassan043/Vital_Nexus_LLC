-- Post-deployment script for VitalNexus.LabMarkersData.Database
PRINT 'LabMarkersData post-deployment script executed.';

IF NOT EXISTS (SELECT * FROM sys.database_principals WHERE name = N'sec_ReadOnly')
    EXEC('CREATE ROLE [sec_ReadOnly]');
GO

IF NOT EXISTS (SELECT 1 FROM dbo.MarkerDefinition WHERE Id = 1)
BEGIN
    INSERT INTO dbo.MarkerDefinition (Id, Code, DisplayName, Unit, Category, DefaultReferenceRangeLow, DefaultReferenceRangeHigh, OptimalRangeLow, OptimalRangeHigh, IsActive)
    VALUES (1, 'WBC', 'White Blood Cell Count', '10^9/L', 'CBC', 4.0, 11.0, 4.5, 10.0, 1);
END
GO
