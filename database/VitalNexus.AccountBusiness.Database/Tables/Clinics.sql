-- Clinic tenants registered in the Accounts database. Each clinic belongs to one customer.
CREATE TABLE [dbo].[Clinics]
(
    [Id] UNIQUEIDENTIFIER NOT NULL,
    [CustomerId] UNIQUEIDENTIFIER NOT NULL,
    [Name] NVARCHAR(200) NOT NULL,
    [CreatedAt] DATETIME2 NOT NULL CONSTRAINT [DF_Clinics_CreatedAt] DEFAULT SYSUTCDATETIME(),
    [IsActive] BIT NOT NULL CONSTRAINT [DF_Clinics_IsActive] DEFAULT 1,
    CONSTRAINT [PK_Clinics] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Clinics_Customers] FOREIGN KEY ([CustomerId]) REFERENCES [dbo].[Customers] ([Id])
);
GO
