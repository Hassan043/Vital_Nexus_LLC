-- Tenant routing metadata: one dedicated Patients database per customer (shared by all clinics).
CREATE TABLE [dbo].[CustomerPatientsDatabases]
(
    [CustomerId] UNIQUEIDENTIFIER NOT NULL,
    [DatabaseName] NVARCHAR(128) NOT NULL,
    [ServerName] NVARCHAR(256) NULL,
    [IsActive] BIT NOT NULL CONSTRAINT [DF_CustomerPatientsDatabases_IsActive] DEFAULT 1,
    [ProvisionedAt] DATETIME2 NOT NULL CONSTRAINT [DF_CustomerPatientsDatabases_ProvisionedAt] DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [PK_CustomerPatientsDatabases] PRIMARY KEY ([CustomerId]),
    CONSTRAINT [FK_CustomerPatientsDatabases_Customers] FOREIGN KEY ([CustomerId]) REFERENCES [dbo].[Customers] ([Id])
);
GO
