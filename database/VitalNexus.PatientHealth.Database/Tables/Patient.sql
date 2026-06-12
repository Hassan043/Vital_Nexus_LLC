-- Core patient record for a single clinic tenant database.
-- PHI for this tenant is stored only in this Patients database instance.
CREATE TABLE [dbo].[Patient]
(
    [Id] UNIQUEIDENTIFIER NOT NULL,
    [AnonymousPatientIdentifier] NVARCHAR(64) NOT NULL,
    [CreatedAt] DATETIME2 NOT NULL CONSTRAINT [DF_Patient_CreatedAt] DEFAULT SYSUTCDATETIME(),
    [UpdatedAt] DATETIME2 NULL,
    [IsActive] BIT NOT NULL CONSTRAINT [DF_Patient_IsActive] DEFAULT 1,
    CONSTRAINT [PK_Patient] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [UQ_Patient_AnonymousPatientIdentifier] UNIQUE ([AnonymousPatientIdentifier])
);
