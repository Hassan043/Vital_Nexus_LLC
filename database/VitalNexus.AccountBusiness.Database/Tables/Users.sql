-- Schema: Users table for AccountBusiness
CREATE TABLE [dbo].[Users]
(
    [Id] UNIQUEIDENTIFIER NOT NULL,
    [EntraObjectId] UNIQUEIDENTIFIER NOT NULL,
    [Email] NVARCHAR(256) NOT NULL,
    [DisplayName] NVARCHAR(200) NULL,
    [CreatedAt] DATETIME2 NOT NULL CONSTRAINT [DF_Users_CreatedAt] DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [PK_Users] PRIMARY KEY ([Id]),
    CONSTRAINT [UQ_Users_EntraObjectId] UNIQUE ([EntraObjectId]),
    CONSTRAINT [UQ_Users_Email] UNIQUE ([Email])
);
