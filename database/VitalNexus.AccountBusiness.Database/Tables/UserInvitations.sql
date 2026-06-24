-- Staff user invitations (Entra-only acceptance; no password storage).
CREATE TABLE [dbo].[UserInvitations]
(
    [Id] UNIQUEIDENTIFIER NOT NULL,
    [CustomerId] UNIQUEIDENTIFIER NOT NULL,
    [Email] NVARCHAR(256) NOT NULL,
    [RoleName] NVARCHAR(64) NOT NULL,
    [InvitedByUserId] UNIQUEIDENTIFIER NOT NULL,
    [CreatedAt] DATETIME2 NOT NULL CONSTRAINT [DF_UserInvitations_CreatedAt] DEFAULT SYSUTCDATETIME(),
    [AcceptedAt] DATETIME2 NULL,
    CONSTRAINT [PK_UserInvitations] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_UserInvitations_Customers] FOREIGN KEY ([CustomerId]) REFERENCES [dbo].[Customers] ([Id]),
    CONSTRAINT [FK_UserInvitations_Users] FOREIGN KEY ([InvitedByUserId]) REFERENCES [dbo].[Users] ([Id])
);
GO

CREATE UNIQUE INDEX [UQ_UserInvitations_PendingEmail]
    ON [dbo].[UserInvitations] ([Email])
    WHERE [AcceptedAt] IS NULL;
GO
