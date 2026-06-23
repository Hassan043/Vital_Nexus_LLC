-- Optional profile details for a clinic (address, contact, timezone).
CREATE TABLE [dbo].[ClinicProfiles]
(
    [ClinicId] UNIQUEIDENTIFIER NOT NULL,
    [DisplayName] NVARCHAR(200) NULL,
    [ContactEmail] NVARCHAR(256) NULL,
    [Phone] NVARCHAR(32) NULL,
    [TimeZoneId] NVARCHAR(64) NULL,
    [CreatedAt] DATETIME2 NOT NULL CONSTRAINT [DF_ClinicProfiles_CreatedAt] DEFAULT SYSUTCDATETIME(),
    [UpdatedAt] DATETIME2 NOT NULL CONSTRAINT [DF_ClinicProfiles_UpdatedAt] DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [PK_ClinicProfiles] PRIMARY KEY ([ClinicId]),
    CONSTRAINT [FK_ClinicProfiles_Clinics] FOREIGN KEY ([ClinicId]) REFERENCES [dbo].[Clinics] ([Id])
);
GO
