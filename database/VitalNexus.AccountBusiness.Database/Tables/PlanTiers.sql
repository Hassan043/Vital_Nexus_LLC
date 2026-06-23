-- Subscription plan tiers (demo seed data; full Stripe mapping deferred).
CREATE TABLE [dbo].[PlanTiers]
(
    [Id] INT NOT NULL,
    [Name] NVARCHAR(100) NOT NULL,
    [Description] NVARCHAR(500) NULL,
    [IsActive] BIT NOT NULL CONSTRAINT [DF_PlanTiers_IsActive] DEFAULT 1,
    CONSTRAINT [PK_PlanTiers] PRIMARY KEY ([Id]),
    CONSTRAINT [UQ_PlanTiers_Name] UNIQUE ([Name])
);
GO
