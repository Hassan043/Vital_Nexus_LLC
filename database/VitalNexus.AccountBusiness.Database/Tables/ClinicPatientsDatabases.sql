-- Tenant routing metadata: maps each clinic to its dedicated Patients database.
CREATE TABLE [dbo].[ClinicPatientsDatabases]
(
    [ClinicId] UNIQUEIDENTIFIER NOT NULL,
    [DatabaseName] NVARCHAR(128) NOT NULL,
    [ServerName] NVARCHAR(256) NULL,
    [IsActive] BIT NOT NULL CONSTRAINT [DF_ClinicPatientsDatabases_IsActive] DEFAULT 1,
    [CreatedAt] DATETIME2 NOT NULL CONSTRAINT [DF_ClinicPatientsDatabases_CreatedAt] DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [PK_ClinicPatientsDatabases] PRIMARY KEY ([ClinicId]),
    CONSTRAINT [FK_ClinicPatientsDatabases_Clinics] FOREIGN KEY ([ClinicId]) REFERENCES [dbo].[Clinics] ([Id])
);
