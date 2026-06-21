-- Clinic membership for Accounts users.
CREATE TABLE [dbo].[ClinicMemberships]
(
    [UserId] UNIQUEIDENTIFIER NOT NULL,
    [ClinicId] UNIQUEIDENTIFIER NOT NULL,
    [JoinedAt] DATETIME2 NOT NULL CONSTRAINT [DF_ClinicMemberships_JoinedAt] DEFAULT SYSUTCDATETIME(),
    [IsActive] BIT NOT NULL CONSTRAINT [DF_ClinicMemberships_IsActive] DEFAULT 1,
    CONSTRAINT [PK_ClinicMemberships] PRIMARY KEY ([UserId], [ClinicId]),
    CONSTRAINT [FK_ClinicMemberships_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([Id]),
    CONSTRAINT [FK_ClinicMemberships_Clinics] FOREIGN KEY ([ClinicId]) REFERENCES [dbo].[Clinics] ([Id])
);

CREATE NONCLUSTERED INDEX [IX_ClinicMemberships_ClinicId]
    ON [dbo].[ClinicMemberships] ([ClinicId]);
