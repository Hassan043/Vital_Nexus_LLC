-- Optional clinic scope for a staff invitation.
CREATE TABLE [dbo].[UserInvitationClinics]
(
    [InvitationId] UNIQUEIDENTIFIER NOT NULL,
    [ClinicId] UNIQUEIDENTIFIER NOT NULL,
    CONSTRAINT [PK_UserInvitationClinics] PRIMARY KEY ([InvitationId], [ClinicId]),
    CONSTRAINT [FK_UserInvitationClinics_UserInvitations] FOREIGN KEY ([InvitationId]) REFERENCES [dbo].[UserInvitations] ([Id]),
    CONSTRAINT [FK_UserInvitationClinics_Clinics] FOREIGN KEY ([ClinicId]) REFERENCES [dbo].[Clinics] ([Id])
);
GO
