-- Many-to-many assignment of application roles to Accounts users.
CREATE TABLE [dbo].[UserRoles]
(
    [UserId] UNIQUEIDENTIFIER NOT NULL,
    [RoleId] INT NOT NULL,
    [AssignedAt] DATETIME2 NOT NULL CONSTRAINT [DF_UserRoles_AssignedAt] DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [PK_UserRoles] PRIMARY KEY ([UserId], [RoleId]),
    CONSTRAINT [FK_UserRoles_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([Id]),
    CONSTRAINT [FK_UserRoles_ApplicationRoles] FOREIGN KEY ([RoleId]) REFERENCES [dbo].[ApplicationRoles] ([Id])
);

CREATE NONCLUSTERED INDEX [IX_UserRoles_RoleId]
    ON [dbo].[UserRoles] ([RoleId]);
