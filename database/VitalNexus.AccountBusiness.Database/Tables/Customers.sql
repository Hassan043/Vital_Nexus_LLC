-- Schema: Customers table for AccountBusiness
CREATE TABLE [dbo].[Customers]
(
    [Id] UNIQUEIDENTIFIER NOT NULL,
    [Name] NVARCHAR(200) NOT NULL,
    [CreatedAt] DATETIME2 NOT NULL CONSTRAINT [DF_Customers_CreatedAt] DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [PK_Customers] PRIMARY KEY ([Id])
);
GO
