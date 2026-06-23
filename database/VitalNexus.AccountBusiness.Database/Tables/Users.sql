-- Users: Entra External ID identity + VitalNexus authorization profile only.
-- Microsoft Entra handles authentication and MFA; no passwords stored here.
CREATE TABLE [dbo].[Users]
(
    [Id] UNIQUEIDENTIFIER NOT NULL,
    [EntraObjectId] UNIQUEIDENTIFIER NULL,
    [CustomerId] UNIQUEIDENTIFIER NOT NULL,
    [Email] NVARCHAR(256) NOT NULL,
    [DisplayName] NVARCHAR(200) NULL,
    [AccountStatus] NVARCHAR(32) NOT NULL CONSTRAINT [DF_Users_AccountStatus] DEFAULT 'Active',
    [CreatedAt] DATETIME2 NOT NULL CONSTRAINT [DF_Users_CreatedAt] DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [PK_Users] PRIMARY KEY ([Id]),
    CONSTRAINT [UQ_Users_EntraObjectId] UNIQUE ([EntraObjectId]),
    CONSTRAINT [UQ_Users_Email] UNIQUE ([Email]),
    CONSTRAINT [FK_Users_Customers] FOREIGN KEY ([CustomerId]) REFERENCES [dbo].[Customers] ([Id])
);
GO
