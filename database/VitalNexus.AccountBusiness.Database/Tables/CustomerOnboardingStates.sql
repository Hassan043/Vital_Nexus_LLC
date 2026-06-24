-- Persisted onboarding progress per customer.
CREATE TABLE [dbo].[CustomerOnboardingStates]
(
    [CustomerId] UNIQUEIDENTIFIER NOT NULL,
    [SelectedPlanTierId] INT NULL,
    [ClinicProfileComplete] BIT NOT NULL CONSTRAINT [DF_CustomerOnboardingStates_ClinicProfileComplete] DEFAULT 0,
    [ProvisioningCompletedAt] DATETIME2 NULL,
    [UpdatedAt] DATETIME2 NOT NULL CONSTRAINT [DF_CustomerOnboardingStates_UpdatedAt] DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [PK_CustomerOnboardingStates] PRIMARY KEY ([CustomerId]),
    CONSTRAINT [FK_CustomerOnboardingStates_Customers] FOREIGN KEY ([CustomerId]) REFERENCES [dbo].[Customers] ([Id]),
    CONSTRAINT [FK_CustomerOnboardingStates_PlanTiers] FOREIGN KEY ([SelectedPlanTierId]) REFERENCES [dbo].[PlanTiers] ([Id])
);
GO
