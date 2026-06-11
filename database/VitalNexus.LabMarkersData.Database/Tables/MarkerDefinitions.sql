-- Marker definitions (master/reference data)
CREATE TABLE [dbo].[MarkerDefinition]
(
    [Id] INT NOT NULL PRIMARY KEY,
    [Code] NVARCHAR(100) NOT NULL,
    [DisplayName] NVARCHAR(200) NOT NULL,
    [Unit] NVARCHAR(50) NULL,
    [Category] NVARCHAR(100) NULL,
    [DefaultReferenceRangeLow] FLOAT NULL,
    [DefaultReferenceRangeHigh] FLOAT NULL,
    [OptimalRangeLow] FLOAT NULL,
    [OptimalRangeHigh] FLOAT NULL,
    [IsActive] BIT NOT NULL DEFAULT 1
);
