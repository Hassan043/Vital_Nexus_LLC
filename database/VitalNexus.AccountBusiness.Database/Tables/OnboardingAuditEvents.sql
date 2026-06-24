-- Customer onboarding audit trail (demo scope; full AuditEvents table deferred to F4.T1.5).
CREATE TABLE [dbo].[OnboardingAuditEvents]
(
    [Id] UNIQUEIDENTIFIER NOT NULL,
    [CustomerId] UNIQUEIDENTIFIER NOT NULL,
    [ActorUserId] UNIQUEIDENTIFIER NULL,
    [EventType] NVARCHAR(64) NOT NULL,
    [Detail] NVARCHAR(500) NULL,
    [OccurredAt] DATETIME2 NOT NULL CONSTRAINT [DF_OnboardingAuditEvents_OccurredAt] DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [PK_OnboardingAuditEvents] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_OnboardingAuditEvents_Customers] FOREIGN KEY ([CustomerId]) REFERENCES [dbo].[Customers] ([Id])
);
GO
