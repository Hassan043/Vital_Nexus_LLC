-- Example view for marker lookup
CREATE VIEW [dbo].[v_MarkerLookup]
AS
SELECT Id, Code, DisplayName, Unit, Category
FROM dbo.MarkerDefinition
WHERE IsActive = 1;
