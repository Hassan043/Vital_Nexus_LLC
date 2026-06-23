-- Application-level roles for VitalNexus authorization (distinct from SQL security roles).
CREATE TABLE [dbo].[ApplicationRoles]
(
    [Id] INT NOT NULL,
    [Name] NVARCHAR(50) NOT NULL,
    CONSTRAINT [PK_ApplicationRoles] PRIMARY KEY ([Id]),
    CONSTRAINT [UQ_ApplicationRoles_Name] UNIQUE ([Name])
);
