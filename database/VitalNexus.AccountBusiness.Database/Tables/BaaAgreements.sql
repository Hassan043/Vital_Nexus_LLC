-- Business Associate Agreement acceptance per customer (demo: checkbox status).
CREATE TABLE [dbo].[BaaAgreements]
(
    [CustomerId] UNIQUEIDENTIFIER NOT NULL,
    [SignedByUserId] UNIQUEIDENTIFIER NOT NULL,
    [SignedAt] DATETIME2 NOT NULL CONSTRAINT [DF_BaaAgreements_SignedAt] DEFAULT SYSUTCDATETIME(),
    [AgreementVersion] NVARCHAR(32) NOT NULL CONSTRAINT [DF_BaaAgreements_AgreementVersion] DEFAULT N'2026.1',
    CONSTRAINT [PK_BaaAgreements] PRIMARY KEY ([CustomerId]),
    CONSTRAINT [FK_BaaAgreements_Customers] FOREIGN KEY ([CustomerId]) REFERENCES [dbo].[Customers] ([Id]),
    CONSTRAINT [FK_BaaAgreements_Users] FOREIGN KEY ([SignedByUserId]) REFERENCES [dbo].[Users] ([Id])
);
GO
