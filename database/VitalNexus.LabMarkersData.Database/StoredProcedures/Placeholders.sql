-- Placeholder stored procedures for LabMarkersData
CREATE PROCEDURE dbo.usp_GetActiveMarkers
AS
BEGIN
    SELECT Id, Code, DisplayName, Unit, Category FROM dbo.MarkerDefinition WHERE IsActive = 1;
END
