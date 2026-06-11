-- Initial reference data for MarkerDefinition
INSERT INTO dbo.MarkerDefinition (Id, Code, DisplayName, Unit, Category, DefaultReferenceRangeLow, DefaultReferenceRangeHigh, OptimalRangeLow, OptimalRangeHigh, IsActive)
VALUES (1, 'WBC', 'White Blood Cell Count', '10^9/L', 'CBC', 4.0, 11.0, 4.5, 10.0, 1);
