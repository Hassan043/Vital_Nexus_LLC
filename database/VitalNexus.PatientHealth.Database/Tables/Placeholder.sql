-- Demo placeholder table proving per-customer Patients database provisioning.
CREATE TABLE [dbo].[Placeholder]
(
    [Id] INT NOT NULL IDENTITY(1, 1),
    [Note] NVARCHAR(200) NOT NULL,
    [CreatedAt] DATETIME2 NOT NULL CONSTRAINT [DF_Placeholder_CreatedAt] DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [PK_Placeholder] PRIMARY KEY ([Id])
);
GO
