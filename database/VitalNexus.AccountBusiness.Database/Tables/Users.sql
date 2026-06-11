-- Schema: Users table for AccountBusiness
CREATE TABLE [dbo].[Users]
(
    [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    [Email] NVARCHAR(256) NOT NULL,
    [DisplayName] NVARCHAR(200) NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
